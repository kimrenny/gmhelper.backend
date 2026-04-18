using DotNetEnv;
using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Mappers;
using MatHelper.BLL.Services;
using MatHelper.BLL.Filters;
using MatHelper.DAL.Database;
using MatHelper.DAL.Interfaces;
using MatHelper.DAL.Repositories;
using MatHelper.IntegrationTests.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");

        builder.UseSetting("https_port", "5001");

        var projectDir = System.IO.Path.GetFullPath("../../../../");
        Env.Load(System.IO.Path.Combine(projectDir, ".env.test"));

        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<ISecurityService, SecurityService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ITokenGeneratorService, TokenGeneratorService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IDeviceManagementService, DeviceManagementService>();
            services.AddScoped<IRequestLogService, RequestLogService>();
            services.AddScoped<IAdminSettingsService, AdminSettingsService>();
            services.AddScoped<IMailService, MockMailService>();
            services.AddScoped<IGeoTaskProcessingService, GeoTaskProcessingService>();
            services.AddScoped<IMathTaskProcessingService, MathTaskProcessingService>();
            services.AddScoped<IClientInfoService, MockClientInfoService>();
            services.AddScoped<ITwoFactorService, TwoFactorService>();
            services.AddScoped<IUserMapper, UserMapper>();
            services.AddScoped<ICaptchaValidationService, MockCaptchaValidationService>();
            services.AddScoped<IErrorLogRepository, ErrorLogRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IEmailConfirmationRepository, EmailConfirmationRepository>();
            services.AddScoped<IEmailLoginCodeRepository, EmailLoginCodeRepository>();
            services.AddScoped<ILoginTokenRepository, LoginTokenRepository>();
            services.AddScoped<IPasswordRecoveryRepository, PasswordRecoveryRepository>();
            services.AddScoped<IRequestLogRepository, MockRequestLogRepository>();
            services.AddScoped<IAuthLogRepository, AuthLogRepository>();
            services.AddScoped<IAdminSettingsRepository, AdminSettingsRepository>();
            services.AddScoped<ITaskRequestRepository, TaskRequestRepository>();
            services.AddScoped<ITaskRatingRepository, TaskRatingRepository>();
            services.AddScoped<ITwoFactorRepository, TwoFactorRepository>();
            services.AddScoped<IAppTwoFactorSessionRepository, AppTwoFactorSessionRepository>();

            services.RemoveAll<RequestLoggingFilter>();
        });
    }

    public void ResetDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }
}
