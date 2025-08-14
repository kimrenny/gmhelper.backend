using DotNetEnv;
using MatHelper.BLL.Filters;
using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Mappers;
using MatHelper.BLL.Middlewares;
using MatHelper.BLL.Services;
using MatHelper.CORE.Options;
using MatHelper.DAL.Database;
using MatHelper.DAL.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(7057);
});

Env.Load("../.env");

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<RequestLoggingFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS")?
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins ?? Array.Empty<string>())
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.Configure<DbOptions>(
    builder.Configuration.GetSection("DbOptions"));

builder.Services.AddDbContext<AppDbContext>((provider, ctx) =>
{
    var options = provider.GetRequiredService<IOptions<DbOptions>>().Value;
    string? connectionString;

    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

    if(env == "Development")
    {
        connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DevConnection");
    }
    else
    {
        connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
    }

    ctx.UseNpgsql(connectionString);
});

var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? builder.Configuration["Jwt:SecretKey"];
var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"];
var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
{
    throw new InvalidOperationException("JWT options are not configured properly.");
}

var jwtOptions = new JwtOptions
{
    SecretKey = secretKey!,
    Issuer = issuer!,
    Audience = audience!
};

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITokenGeneratorService,  TokenGeneratorService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IDeviceManagementService, DeviceManagementService>();
builder.Services.AddScoped<IRequestLogService, RequestLogService>();
builder.Services.AddScoped<IProcessRequestService, ProcessRequestService>();
builder.Services.AddScoped<IAdminSettingsService, AdminSettingsService>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IGeoTaskProcessingService, GeoTaskProcessingService>();
builder.Services.AddScoped<IMathTaskProcessingService, MathTaskProcessingService>();
builder.Services.AddScoped<IClientInfoService, ClientInfoService>();
builder.Services.AddScoped<IUserMapper, UserMapper>();
builder.Services.AddScoped<ErrorLogRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<EmailConfirmationRepository>();
builder.Services.AddScoped<LoginTokenRepository>();
builder.Services.AddScoped<PasswordRecoveryRepository>();
builder.Services.AddScoped<RequestLogRepository>();
builder.Services.AddScoped<AuthLogRepository>();
builder.Services.AddScoped<AdminSettingsRepository>();
builder.Services.AddScoped<CaptchaValidationService>();
builder.Services.AddScoped<TaskRequestRepository>();
builder.Services.AddScoped<TaskRatingRepository>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.SecretKey))
        };
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors("AllowFrontend");

app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
        return;
    }
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHsts();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseMiddleware<ErrorLoggingMiddleware>();

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    var remoteIp = context.Connection.RemoteIpAddress?.ToString();

    if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
    {
        remoteIp = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ?? remoteIp;
    }

    var headers = context.Request.Headers.Select(h => $"{h.Key}: {h.Value}").ToList();

    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' https://www.google.com https://www.gstatic.com; style-src 'self'; img-src 'self'; font-src 'self'; object-src 'none'; frame-ancestors 'self' https://www.google.com https://www.gstatic.com; base-uri 'self'; form-action 'self'";
    
    var startTime = DateTime.UtcNow;
    logger.LogInformation($"[{DateTime.UtcNow}] Handling request: {context.Request.Method} {context.Request.Path}");
    
    await next();

    var elapsedTime = DateTime.UtcNow - startTime;
    logger.LogInformation($"[{DateTime.UtcNow}] Response status: {context.Response.StatusCode}. Time taken: {elapsedTime}. [{context.Request.Method} {context.Request.Path}]");
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
