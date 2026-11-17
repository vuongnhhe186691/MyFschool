namespace PRM_PRJ.DTO.Request
{
    public class ResetPasswordRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}