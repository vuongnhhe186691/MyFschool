using Microsoft.Extensions.Caching.Memory;

namespace PRM_PRJ.Services.Email
{
    public class LoginOtpService : ILoginOtpService
    {
        private readonly IMemoryCache _memoryCache;
        private const int ExpireMinutes = 5;

        public LoginOtpService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public void StoreVerifiedPassword(string phoneNumber, string password)
        {
            _memoryCache.Set(
                GetPasswordKey(phoneNumber),
                password,
                TimeSpan.FromMinutes(ExpireMinutes));
        }

        public bool HasValidVerifiedPassword(string phoneNumber, out string password)
        {
            if (_memoryCache.TryGetValue(GetPasswordKey(phoneNumber), out string? storedPassword)
                && !string.IsNullOrWhiteSpace(storedPassword))
            {
                password = storedPassword;
                return true;
            }

            password = string.Empty;
            return false;
        }

        public string GenerateAndStoreOtp(string phoneNumber)
        {
            var otp = Random.Shared.Next(100000, 999999).ToString();
            _memoryCache.Set(GetOtpKey(phoneNumber), otp, TimeSpan.FromMinutes(ExpireMinutes));
            return otp;
        }

        public bool VerifyOtp(string phoneNumber, string otpCode)
        {
            if (!_memoryCache.TryGetValue(GetOtpKey(phoneNumber), out string? storedOtp))
                return false;

            return string.Equals(storedOtp, otpCode, StringComparison.Ordinal);
        }

        public void Clear(string phoneNumber)
        {
            _memoryCache.Remove(GetOtpKey(phoneNumber));
            _memoryCache.Remove(GetPasswordKey(phoneNumber));
        }

        private static string GetOtpKey(string phoneNumber)
            => $"login-otp:{phoneNumber.Trim()}";

        private static string GetPasswordKey(string phoneNumber)
            => $"login-password:{phoneNumber.Trim()}";
    }
}