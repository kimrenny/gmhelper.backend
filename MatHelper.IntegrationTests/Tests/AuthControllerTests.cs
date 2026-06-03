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
            _factory.ResetDatabase();

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
            var userDto = new UserDto
            {
                Email = $"testuser_{Guid.NewGuid()}@example.com",
                UserName = $"testuser_{Guid.NewGuid()}",
                Password = "Password123!",
                CaptchaToken = "valid-captcha"
            };

            var registerResponse = await _client.PostAsJsonAsync("api/v1/auth/register", userDto);
            registerResponse.EnsureSuccessStatusCode();

            using (var scope = _factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
                Assert.NotNull(user);
                user.IsActive = true;
                await db.SaveChangesAsync();
            }

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

            var refreshResponse = await _client.PostAsync("api/v1/auth/token/refresh", null);
            refreshResponse.EnsureSuccessStatusCode();

            var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(refreshResult?.AccessToken);
        }

        [Fact]
        public async Task Login_ShouldFail_WhenUserIsNotActive()
        {
            var userDto = new UserDto
            {
                Email = $"inactiveuser_{Guid.NewGuid()}@example.com",
                UserName = $"inactiveuser_{Guid.NewGuid()}",
                Password = "Password123!",
                CaptchaToken = "valid-captcha"
            };

            var registerResponse = await _client.PostAsJsonAsync("api/v1/auth/register", userDto);
            registerResponse.EnsureSuccessStatusCode();

            var loginDto = new LoginDto
            {
                Email = userDto.Email,
                Password = userDto.Password,
                CaptchaToken = "valid-captcha",
                Remember = true
            };

            var loginResponse = await _client.PostAsJsonAsync("api/v1/auth/login", loginDto);

            Assert.False(loginResponse.IsSuccessStatusCode);
        }


        [Fact]
        public async Task Login_ShouldFail_WhenPasswordIsIncorrect()
        {
            var userDto = new UserDto
            {
                Email = $"wrongpassuser_{Guid.NewGuid()}@example.com",
                UserName = $"wrongpassuser_{Guid.NewGuid()}",
                Password = "CorrectPassword123!",
                CaptchaToken = "valid-captcha"
            };

            var registerResponse = await _client.PostAsJsonAsync("api/v1/auth/register", userDto);
            registerResponse.EnsureSuccessStatusCode();

            using (var scope = _factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
                Assert.NotNull(user);
                user.IsActive = true;
                await db.SaveChangesAsync();
            }

            var loginDto = new LoginDto
            {
                Email = userDto.Email,
                Password = "WrongPassword!",
                CaptchaToken = "valid-captcha",
                Remember = true
            };

            var loginResponse = await _client.PostAsJsonAsync("api/v1/auth/login", loginDto);

            Assert.False(loginResponse.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Register_Login_Logout_ShouldInvalidateSession()
        {
            var userDto = new UserDto
            {
                Email = $"logoutuser_{Guid.NewGuid()}@example.com",
                UserName = $"logoutuser_{Guid.NewGuid()}",
                Password = "Password123!",
                CaptchaToken = "valid-captcha"
            };
            var regResp = await _client.PostAsJsonAsync("api/v1/auth/register", userDto);
            regResp.EnsureSuccessStatusCode();

            using (var scope = _factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
                user!.IsActive = true;
                await db.SaveChangesAsync();
            }

            var loginDto = new LoginDto
            {
                Email = userDto.Email,
                Password = userDto.Password,
                CaptchaToken = "valid-captcha",
                Remember = true
            };
            var loginResp = await _client.PostAsJsonAsync("api/v1/auth/login", loginDto);
            loginResp.EnsureSuccessStatusCode();

            var logoutResp = await _client.PostAsync("api/v1/auth/logout", null);
            logoutResp.EnsureSuccessStatusCode();

            var refreshResp = await _client.PostAsync("api/v1/auth/token/refresh", null);
            Assert.Equal(HttpStatusCode.BadRequest, refreshResp.StatusCode);
        }

        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenRequiredFieldsAreEmpty()
        {
            var invalidUserDto = new UserDto
            {
                Email = "",
                UserName = "   ",
                Password = "Password123!",
                CaptchaToken = "valid-captcha"
            };

            var response = await _client.PostAsJsonAsync("api/v1/auth/register", invalidUserDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_ShouldBlockRequests_WhenTooManyFailedAttempts()
        {
            var userDto = new UserDto
            {
                Email = $"blockedip_{Guid.NewGuid()}@example.com",
                UserName = $"blockedip_{Guid.NewGuid()}",
                Password = "CorrectPassword123!",
                CaptchaToken = "valid-captcha"
            };
            var regResp = await _client.PostAsJsonAsync("api/v1/auth/register", userDto);
            regResp.EnsureSuccessStatusCode();

            var wrongLoginDto = new LoginDto
            {
                Email = userDto.Email,
                Password = "TotallyWrongPassword!",
                CaptchaToken = "valid-captcha",
                Remember = false
            };

            HttpResponseMessage lastResponse = null!;
            for (int i = 0; i < 6; i++)
            {
                lastResponse = await _client.PostAsJsonAsync("api/v1/auth/login", wrongLoginDto);
            }

            var correctLoginDto = new LoginDto
            {
                Email = userDto.Email,
                Password = userDto.Password,
                CaptchaToken = "valid-captcha",
                Remember = false
            };
            var responseAfterBlock = await _client.PostAsJsonAsync("api/v1/auth/login", correctLoginDto);

            Assert.False(responseAfterBlock.IsSuccessStatusCode);
        }
    }
}
