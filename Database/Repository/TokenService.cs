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

        public AuthResponse GenerateTokens(string userID)
        {

            var accessToken = GenerateAccessToken(userID);
            var refreshToken = GenerateRefreshToken();

            SaveRefreshToken(userID, refreshToken);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
        private string GenerateAccessToken(string userID)
        {

            var user = GetUserFromDatabase(userID) ?? throw new Exception("User Not Found");

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userID),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.Role)
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

        private Users GetUserFromDatabase(string userID)
        {
            // Replace this with your actual data retrieval logic

            return _userRepository.GetAll().FirstOrDefault(u => u.ID == int.Parse(userID));
        }

        private string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        private void SaveRefreshToken(string userID, string refreshToken)
        {
            var token = new RefreshToken
            {
                UserID = userID,
                Token = refreshToken,
                ExpiryDate = DateTime.Now.AddDays(_jwtSettings.RefreshTokenExpiryDays)
            };

            _context.RefreshToken.Add(token);
            _context.SaveChanges();
        }

        public string ValidateRefreshToken(string refreshToken)
        {
            var token = _context.RefreshToken.SingleOrDefault(t => t.Token == refreshToken && t.ExpiryDate > DateTime.Now);
            return token?.UserID;
        }

        public async Task InvalidateRefreshToken(string refreshToken, CancellationToken cancellationToken)
        {

            var token = await _refreshTokenRepository.GetToken(refreshToken, cancellationToken);

            if(token != null)
            {
                await _refreshTokenRepository.RemoveToken(refreshToken, cancellationToken);
            }
            
                /*var token = _context.RefreshToken.SingleOrDefault(t => t.Token == refreshToken);
                if (token != null)
                {
                
                    _context.RefreshToken.Remove(token);
                    _context.SaveChangesAsync();
                }*/
            
        }

    }
}
