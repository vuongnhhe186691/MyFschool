using Microsoft.Extensions.Caching.Memory;

namespace PRM_PRJ.Services.Email
{
    public class ForgotPasswordOtpService : IForgotPasswordOtpService
    {
        private readonly IMemoryCache _memoryCache;
        private const int ExpireMinutes = 5;

        public ForgotPasswordOtpService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public string GenerateAndStore(string phoneNumber)
        {
            var otp = Random.Shared.Next(100000, 999999).ToString();
            _memoryCache.Set(GetKey(phoneNumber), otp, TimeSpan.FromMinutes(ExpireMinutes));
            return otp;
        }

        public bool Verify(string phoneNumber, string otpCode)
        {
            if (!_memoryCache.TryGetValue(GetKey(phoneNumber), out string? storedOtp))
                return false;

            return string.Equals(storedOtp, otpCode, StringComparison.Ordinal);
        }

        public void Remove(string phoneNumber)
        {
            _memoryCache.Remove(GetKey(phoneNumber));
        }

        private static string GetKey(string phoneNumber)
            => $"forgot-password-otp:{phoneNumber.Trim()}";
    }
}