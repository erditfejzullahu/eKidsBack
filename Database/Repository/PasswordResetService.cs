using BCrypt.Net;
using Database.Context;
using Database.DTOs;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly ILogger<PasswordResetService> _logger;
        private readonly ApplicationDbContext _context;
        public PasswordResetService(ILogger<PasswordResetService> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<string> GeneratePasswordResetTokenAsync(string email)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(c => c.Email == email);
                if (user == null) return null;

                var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

                var expiresAt = DateTime.UtcNow.AddHours(2);

                var resetToken = new PasswordResetTokens
                {
                    UserId = user.ID,
                    Token = token,
                    ExpiresAt = expiresAt,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.PasswordResetTokens.AddAsync(resetToken);

                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating password reset");
                throw new ApplicationException(ex.Message);
            }
        }

        public async Task<bool> ValidatePasswordResetTokenAsync(string email, string token)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(c => c.Email == email);
                if(user == null ) return false; 

                var resetToken = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => 
                        t.UserId == user.ID &&
                        t.Token == token &&
                        t.ExpiresAt > DateTime.UtcNow &&
                        !t.IsUsed);

                return resetToken != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating passwrod reset");
                throw new ApplicationException(ex.Message); 
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken CancToken)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(c => c.Email == email, CancToken);
                if (user == null) return false;

                var resetToken = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t =>
                        t.UserId == user.ID &&
                        t.Token == token &&
                        t.ExpiresAt > DateTime.UtcNow &&
                        !t.IsUsed, CancToken);

                if (resetToken == null) return false;

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.Password = hashedPassword;
                resetToken.IsUsed = true;

                _context.Users.Update(user);

                var userTokens = await _context.PasswordResetTokens
                    .Where(t => t.UserId == user.ID)
                    .ToListAsync(CancToken);

                _context.PasswordResetTokens.RemoveRange(userTokens);
                await _context.SaveChangesAsync(CancToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reseting password");
                throw new ApplicationException(ex.Message);
            }
        }
    }
}
