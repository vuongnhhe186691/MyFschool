using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.DTO.Request
{
    public class CreateAdminNotificationRequest
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;
    }
}