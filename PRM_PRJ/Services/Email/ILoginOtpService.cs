namespace PRM_PRJ.Services.Email
{
    public interface ILoginOtpService
    {
        void StoreVerifiedPassword(string phoneNumber, string password);
        bool HasValidVerifiedPassword(string phoneNumber, out string password);
        string GenerateAndStoreOtp(string phoneNumber);
        bool VerifyOtp(string phoneNumber, string otpCode);
        void Clear(string phoneNumber);
    }
}