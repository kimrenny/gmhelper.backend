using Azure.Core;
using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using MatHelper.CORE.Options;
using MatHelper.CORE.Exceptions;
using MatHelper.DAL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace MatHelper.BLL.Services
{
    public class OwnerService : IOwnerService
    {
        private readonly ISecurityService _securityService;
        private readonly ITokenService _tokenService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IUserRepository _userRepository;
        private readonly ILoginTokenRepository _loginTokenRepository;
        private readonly IUserMapper _userMapper;
        private readonly ILogger _logger;

        public OwnerService(ISecurityService securityService, ITokenService tokenService, ITwoFactorService twoFactorService, IUserRepository userRepository, ILoginTokenRepository loginTokenRepository, IUserMapper userMapper, ILogger<AdminService> logger)
        {
            _securityService = securityService;
            _tokenService = tokenService;
            _twoFactorService = twoFactorService;
            _userRepository = userRepository;
            _loginTokenRepository = loginTokenRepository;
            _userMapper = userMapper;
            _logger = logger;
        }

        public async Task ChangeUserRoleAsync(Guid requesterUserId, Guid targetUserId, string newRole)
        {
            var requester = await _userRepository.GetUserByIdAsync(requesterUserId);
            if(requester == null)
                throw new Exception("Requester not found");

            var user = await _userRepository.GetUserByIdAsync(targetUserId);
            if (user == null)
                throw new Exception("User not found");

            if (user.Role == UserRole.Owner.ToString())
                throw new Exception("Cannot change Owner role");

            if (!Enum.TryParse<UserRole>(newRole, out var parsedRole))
                throw new Exception("Invalid role");

            bool isTwoFAEnabled = await _twoFactorService.IsTwoFactorEnabledAsync(requester.Id, "totp");
            if (!isTwoFAEnabled)
                throw new TwoFactorRequiredException();

            if (parsedRole == UserRole.Owner)
                throw new Exception("Cannot assign Owner role");

            user.Role = parsedRole.ToString();

            await _userRepository.UpdateUserAsync(user);
            await _tokenService.DeactivateAllUserTokensAsync(user.Id);

            _logger.LogInformation(
                "User role changed. UserId: {UserId}, NewRole: {Role}",
                user.Id,
                parsedRole
            );
        }
    }
}