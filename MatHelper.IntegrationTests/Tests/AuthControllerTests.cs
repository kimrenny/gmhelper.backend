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
        private readonly CookieContainer _cookieContainer;

        public AuthControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;

            _cookieContainer = new CookieContainer();

            var clientOptions = new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                BaseAddress = new Uri("https://localhost:5001")
            };

            _client = factory.CreateClient(clientOptions);
        }


        [Fact]
        public async Task Register_Login_RefreshToken_WorkFlow()
        {
            var clientOptions = new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
            };
            var client = _factory.CreateClient(clientOptions);

            // 1. Registration
            var userDto = new UserDto
            {
                Email = $"testuser_{Guid.NewGuid()}@example.com",
                UserName = $"testuser_{Guid.NewGuid()}",
                Password = "Password123!",
                CaptchaToken = "valid-captcha"
            };

            var registerResponse = await _client.PostAsJsonAsync("api/v1/auth/register", userDto);
            registerResponse.EnsureSuccessStatusCode();

            // ==== ACTIVATION USER ====
            using (var scope = _factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
                Assert.NotNull(user);
                user.IsActive = true;
                await db.SaveChangesAsync();
            }

            // 2. Login
            var loginDto = new LoginDto
            {
                Email = userDto.Email,
                Password = userDto.Password,
                CaptchaToken = "valid-captcha",
                Remember = true
            };

            var loginResponse = await _client.PostAsJsonAsync("api/v1/auth/login", loginDto);
            loginResponse.EnsureSuccessStatusCode();

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
            Assert.NotNull(loginResult?.Data);
            Assert.NotNull(loginResult.Data.AccessToken);

            // 3. Refresh token
            var refreshResponse = await _client.PostAsync("api/v1/auth/token/refresh", null);
            refreshResponse.EnsureSuccessStatusCode();

            var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(refreshResult?.AccessToken);
        }

        [Fact]
        public async Task Login_ShouldFail_WhenUserIsNotActive()
        {
            // 1. Registration
            var userDto = new UserDto
            {
                Email = $"inactiveuser_{Guid.NewGuid()}@example.com",
                UserName = $"inactiveuser_{Guid.NewGuid()}",
                Password = "Password123!",
                CaptchaToken = "valid-captcha"
            };

            var registerResponse = await _client.PostAsJsonAsync("api/v1/auth/register", userDto);
            registerResponse.EnsureSuccessStatusCode();

            // 2. Attempt login without activating user
            var loginDto = new LoginDto
            {
                Email = userDto.Email,
                Password = userDto.Password,
                CaptchaToken = "valid-captcha",
                Remember = true
            };

            var loginResponse = await _client.PostAsJsonAsync("api/v1/auth/login", loginDto);

            // Expect failure (e.g., 400 BadRequest or 401 Unauthorized)
            Assert.False(loginResponse.IsSuccessStatusCode);
        }


        [Fact]
        public async Task Login_ShouldFail_WhenPasswordIsIncorrect()
        {
            // 1. Registration
            var userDto = new UserDto
            {
                Email = $"wrongpassuser_{Guid.NewGuid()}@example.com",
                UserName = $"wrongpassuser_{Guid.NewGuid()}",
                Password = "CorrectPassword123!",
                CaptchaToken = "valid-captcha"
            };

            var registerResponse = await _client.PostAsJsonAsync("api/v1/auth/register", userDto);
            registerResponse.EnsureSuccessStatusCode();

            // ==== ACTIVATION USER ====
            using (var scope = _factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
                Assert.NotNull(user);
                user.IsActive = true;
                await db.SaveChangesAsync();
            }

            // 2. Attempt login with wrong password
            var loginDto = new LoginDto
            {
                Email = userDto.Email,
                Password = "WrongPassword!",
                CaptchaToken = "valid-captcha",
                Remember = true
            };

            var loginResponse = await _client.PostAsJsonAsync("api/v1/auth/login", loginDto);

            // Expect failure
            Assert.False(loginResponse.IsSuccessStatusCode);
        }

    }
}
