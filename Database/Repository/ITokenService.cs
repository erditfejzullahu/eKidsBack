using Database.Models;

namespace Database.Repository
{
    public interface ITokenService
    {
        Task<AuthResponse> GenerateTokens(string userID, CancellationToken cancToken);
        Task<string?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancToken);
        Task InvalidateRefreshToken(string refreshToken, CancellationToken cancellationToken);
    }
}