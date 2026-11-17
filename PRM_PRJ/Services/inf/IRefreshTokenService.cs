using PRM_PRJ.Models;

namespace PRM_PRJ.Services.inf
{
    public interface IRefreshTokenService
    {
        Task<string> GenerateAsync(User user);
        Task<RefreshToken?> ValidateAsync(string token);
        Task<string?> RotateAsync(string refreshToken);
        Task RevokeAsync(string token);
    }
}