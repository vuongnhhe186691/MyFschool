using Microsoft.EntityFrameworkCore;
using PRM_PRJ.Models;
using PRM_PRJ.Services.inf;
using System.Security.Cryptography;

namespace PRM_PRJ.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly AppDbContext _db;

        public RefreshTokenService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<string> GenerateAsync(User user)
        {
            var token = GenerateTokenValue();
            var expiry = DateTime.UtcNow.AddDays(7);

            _db.RefreshTokens.Add(new RefreshToken
            {
                Token = token,
                UserId = user.Id,
                ExpiryDate = expiry,
                IsRevoked = false
            });

            await _db.SaveChangesAsync();
            return token;
        }

        public async Task<RefreshToken?> ValidateAsync(string token)
        {
            return await _db.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Token == token &&
                    !x.IsRevoked &&
                    x.ExpiryDate > DateTime.UtcNow);
        }

        public async Task<string?> RotateAsync(string refreshToken)
        {
            var entity = await _db.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == refreshToken);

            if (entity == null || entity.IsRevoked || entity.ExpiryDate <= DateTime.UtcNow)
            {
                return null;
            }

            entity.IsRevoked = true;

            var newToken = GenerateTokenValue();
            _db.RefreshTokens.Add(new RefreshToken
            {
                Token = newToken,
                UserId = entity.UserId,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            });

            await _db.SaveChangesAsync();
            return newToken;
        }

        public async Task RevokeAsync(string token)
        {
            var entity = await _db.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == token);

            if (entity == null)
            {
                return;
            }

            entity.IsRevoked = true;
            await _db.SaveChangesAsync();
        }

        private static string GenerateTokenValue()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}