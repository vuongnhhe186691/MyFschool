namespace PRM_PRJ.DTO.Response
{
    public class AdminUserItemResponse
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}