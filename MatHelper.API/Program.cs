using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Options;
using MatHelper.DAL.Database;
using MatHelper.DAL.Repositories;
using Microsoft.IdentityModel.Tokens;
using MatHelper.BLL.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<RequestLogRepository>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<RequestLoggingFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowAll",
        policy =>
        {
            policy.AllowAnyHeader();
            policy.AllowAnyMethod();
            policy.AllowAnyOrigin();
        });
});
builder.Services.Configure<DbOptions>(
    builder.Configuration.GetSection("DbOptions"));

builder.Services.AddDbContext<AppDbContext>((provider, ctx) =>
{
    var options = provider.GetRequiredService<IOptions<DbOptions>>().Value;
    ctx.UseSqlServer(options.ConnectionString);
});

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();

if (jwtOptions == null)
{
    throw new InvalidOperationException("JWT options are not configured properly.");
}

builder.Services.AddSingleton(jwtOptions);

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IRequestLogService, RequestLogService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<RequestLogRepository>();
builder.Services.AddScoped<CaptchaValidationService>();

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
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.SecretKey))
        };
    });

var app = builder.Build();

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Handling request: {Method} {Path}", context.Request.Method, context.Request.Path);
    await next();
    logger.LogInformation("Response status: {StatusCode}", context.Response.StatusCode);
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
