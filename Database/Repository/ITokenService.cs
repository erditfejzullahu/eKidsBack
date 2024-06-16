using Database.Models;

namespace Database.Repository
{
    public interface ITokenService
    {
        AuthResponse GenerateTokens(string userID);
        string ValidateRefreshToken(string refreshToken);
        Task InvalidateRefreshToken(string refreshToken, CancellationToken cancellationToken);
    }
}