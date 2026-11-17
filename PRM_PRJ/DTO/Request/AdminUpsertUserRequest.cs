using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.DTO.Request
{
    public class AdminUpsertUserRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? Password { get; set; }

        public bool IsActive { get; set; } = true;

        public List<string> Roles { get; set; } = new();
    }
}