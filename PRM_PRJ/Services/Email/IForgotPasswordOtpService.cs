namespace PRM_PRJ.Services.Email
{
    public interface IForgotPasswordOtpService
    {
        string GenerateAndStore(string phoneNumber);
        bool Verify(string phoneNumber, string otpCode);
        void Remove(string phoneNumber);
    }
}