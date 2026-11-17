using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PRM_PRJ.Models
{
    public class Application
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public Guid SenderUserId { get; set; }

        [ForeignKey(nameof(SenderUserId))]
        public User SenderUser { get; set; } = null!;
    }
}