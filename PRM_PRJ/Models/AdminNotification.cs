using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class AdminNotification
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public Guid CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserNotificationRead> UserReads { get; set; } = new List<UserNotificationRead>();
    }
}