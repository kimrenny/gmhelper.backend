using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Models;
using MatHelper.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace MatHelper.BLL.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly ITokenGeneratorService _tokenGeneratorService;
        private readonly IUserRepository _userRepository;
        private readonly IEmailConfirmationRepository _emailConfirmationRepository;

        public RegistrationService(
            ITokenGeneratorService tokenGeneratorService,
            IUserRepository userRepository,
            IEmailConfirmationRepository emailConfirmationRepository)
        {
            _tokenGeneratorService = tokenGeneratorService;
            _userRepository = userRepository;
            _emailConfirmationRepository = emailConfirmationRepository;
        }

        public async Task EnsureEmailAndUsernameUniqueAsync(string email, string username)
        {
            var existingUserByEmail = await _userRepository.GetUserByEmailAsync(email);

            if (existingUserByEmail != null)
            {
                if (!existingUserByEmail.IsActive)
                {
                    var existingToken = await _emailConfirmationRepository
                        .GetTokenByUserIdAsync(existingUserByEmail.Id);

                    if (existingToken != null && existingToken.ExpirationDate > DateTime.UtcNow)
                    {
                        throw new InvalidOperationException("The account awaits confirmation. Follow the link in the email.");
                    }
                    else
                    {
                        await _userRepository.DeleteUserAsync(existingUserByEmail);
                        _userRepository.Detach(existingUserByEmail);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Email is already used by another user.");
                }
            }

            var existingUserByUsername = await _userRepository.GetUserByUsernameAsync(username);

            if (existingUserByUsername != null)
            {
                throw new InvalidOperationException("Username is already used by another user.");
            }
        }

        public Task<User> BuildNewUserAsync(UserDto dto, string passwordHash)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = dto.UserName,
                Email = dto.Email,
                RegistrationDate = DateTime.UtcNow,
                PasswordHash = passwordHash,
                Avatar = null,
                Role = "User",
                IsActive = false,
            };

            return Task.FromResult(user);
        }

        public async Task CreateInactiveInitialSessionAsync(User user, DeviceInfo device, string ip)
        {
            var accessToken = _tokenGeneratorService.GenerateJwtToken(user, device);
            var refreshToken = _tokenGeneratorService.GenerateRefreshToken();

            var loginToken = new LoginToken
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow,
                RefreshTokenExpiration = DateTime.UtcNow,
                UserId = user.Id,
                DeviceInfo = device,
                IpAddress = ip,
                IsActive = false
            };

            user.LoginTokens = new List<LoginToken> { loginToken };

            await Task.CompletedTask;
        }
    }
}