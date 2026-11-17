namespace PRM_PRJ.Models
{
    public class UserNotificationRead
    {
        public int NotificationId { get; set; }
        public AdminNotification Notification { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    }
}