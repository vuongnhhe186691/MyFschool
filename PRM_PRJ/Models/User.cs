using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsTwoFactorEnabled { get; set; } = false;

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        // user làm phụ huynh của nhiều học sinh
        public ICollection<StudentParent> ParentStudents { get; set; } = new List<StudentParent>();
    }
}