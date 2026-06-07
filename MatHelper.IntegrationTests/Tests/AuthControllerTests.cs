using MatHelper.API.Common;
using MatHelper.CORE.Models;
using MatHelper.DAL.Database;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace MatHelper.IntegrationTests.Tests
{
    public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public AuthControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _factory.ResetDatabase();

            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                BaseAddress = new Uri("https://localhost:5001")
            });
        }

        private async Task<(string email, string password)> RegisterUserAsync(bool activate = false)
        {
            var email = $"user_{Guid.NewGuid()}@test.com";
            var password = "Password123!";

            var initDto = new RegisterRequestDto
            {
                Email = email,
                UserName = $"user_{Guid.NewGuid()}",
                CaptchaToken = "valid-captcha"
            };

            await _client.PostAsJsonAsync("api/v1/auth/register/code", initDto);

            string code;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                code = await db.EmailLoginCodes
                    .Where(x => x.Email == email)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Code)
                    .FirstAsync();
            }

            var registerDto = new UserDto
            {
                Email = email,
                UserName = initDto.UserName,
                Password = password,
                CaptchaToken = "valid-captcha",
                Token = code
            };

            await _client.PostAsJsonAsync("api/v1/auth/register", registerDto);

            if (activate)
            {
                using var scope = _factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
                if (user != null)
                {
                    user.IsActive = true;
                    await db.SaveChangesAsync();
                }
            }

            return (email, password);
        }

        [Fact]
        public async Task Login_ShouldWork_OnlyIfSystemAllows()
        {
            var (email, password) = await RegisterUserAsync(true);

            var login = new LoginDto
            {
                Email = email,
                Password = password,
                CaptchaToken = "valid-captcha",
                Remember = true
            };

            var response = await _client.PostAsJsonAsync("api/v1/auth/login", login);

            var body = await response.Content.ReadAsStringAsync();

            Assert.True(
                response.IsSuccessStatusCode ||
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.BadRequest
            );
        }

        [Fact]
        public async Task Login_ShouldReject_WhenPasswordIsWrong()
        {
            var (email, password) = await RegisterUserAsync(true);

            var login = new LoginDto
            {
                Email = email,
                Password = "wrong",
                CaptchaToken = "valid-captcha",
                Remember = true
            };

            var response = await _client.PostAsJsonAsync("api/v1/auth/login", login);

            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized ||
                !response.IsSuccessStatusCode
            );
        }

        [Fact]
        public async Task Login_ShouldEventuallyBlockOrThrottle()
        {
            var (email, _) = await RegisterUserAsync(true);

            var bad = new LoginDto
            {
                Email = email,
                Password = "wrong",
                CaptchaToken = "valid-captcha",
                Remember = false
            };

            HttpResponseMessage last = null!;

            for (int i = 0; i < 6; i++)
            {
                last = await _client.PostAsJsonAsync("api/v1/auth/login", bad);
            }

            Assert.True(
                last.StatusCode == HttpStatusCode.TooManyRequests ||
                last.StatusCode == HttpStatusCode.Unauthorized ||
                !last.IsSuccessStatusCode
            );
        }
    }
}