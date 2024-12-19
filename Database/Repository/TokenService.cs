using Database.Context;
using Database.Models;
using eKids.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ApplicationDbContext _context;
        private readonly IRepository<RefreshToken> _refreshTokenRepository;
        private readonly IRepository<Users> _userRepository;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IOptions<JwtSettings> jwtSettings, IRepository<RefreshToken> refreshTokenRepository, ApplicationDbContext context, IRepository<Users> userRepository, ILogger<TokenService> logger)
        {
            _jwtSettings = jwtSettings.Value;
            _refreshTokenRepository = refreshTokenRepository;
            _context = context;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<AuthResponse> GenerateTokens(string userID, CancellationToken cancToken)
        {

            var accessToken = await GenerateAccessTokenAsync(userID, cancToken);
            var refreshToken = GenerateRefreshToken();

            await SaveRefreshTokenAsync(userID, refreshToken, cancToken);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
        private async Task<string> GenerateAccessTokenAsync(string userID, CancellationToken cancToken)
        {

            var user = await GetUserFromDatabaseAsync(userID, cancToken) ?? throw new Exception("User Not Found");

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userID),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,    
                claims: claims,
                signingCredentials: creds,
                expires: DateTime.Now.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes)
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<Users?> GetUserFromDatabaseAsync(string userID, CancellationToken cancToken)
        {
            // Replace this with your actual data retrieval logic

            return await _userRepository.GetAll().FirstOrDefaultAsync(u => u.ID == int.Parse(userID), cancToken);
        }

        private string GenerateRefreshToken()
        {
            //return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var randomBytes = new byte[64];
            RandomNumberGenerator.Fill(randomBytes);
            return Convert.ToHexString(randomBytes);
        }

        private async Task SaveRefreshTokenAsync(string userID, string refreshToken, CancellationToken cancToken)
        {
            try
            {
                var token = new RefreshToken
                {
                    UserID = userID,
                    Token = refreshToken,
                    ExpiryDate = DateTime.Now.AddDays(_jwtSettings.RefreshTokenExpiryDays)
                };

                await _context.RefreshToken.AddAsync(token, cancToken);
                await _context.SaveChangesAsync(cancToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in adding refreshtoken in db");
                throw;
            }
        }

        public async Task<string?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancToken)
        {
            try
            {
                var token = await _context.RefreshToken.AsNoTracking().SingleOrDefaultAsync(t => t.Token == refreshToken && t.ExpiryDate > DateTime.Now, cancToken);
                if(token == null)
                {
                    return null;
                }
                return token.UserID;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating refresh token");
                throw;
            }
        }

        public async Task InvalidateRefreshToken(string refreshToken, CancellationToken cancellationToken)
        {
            try
            {
                var token = await _refreshTokenRepository.GetToken(refreshToken, cancellationToken);

                if(token != null)
                {
                    await _refreshTokenRepository.RemoveToken(refreshToken, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in invalidating refresh token");
                throw;
            }
        }

    }
}
