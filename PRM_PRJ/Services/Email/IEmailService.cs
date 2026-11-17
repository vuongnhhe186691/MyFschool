namespace PRM_PRJ.Services.Email
{
    public interface IEmailService
    {
        Task SendOtpAsync(string toEmail, string otpCode);
        Task SendLoginOtpAsync(string toEmail, string otpCode);
    }
}