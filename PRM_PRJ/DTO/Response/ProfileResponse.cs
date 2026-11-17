namespace PRM_PRJ.DTO.Response
{
    public class ProfileResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsTwoFactorEnabled { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}