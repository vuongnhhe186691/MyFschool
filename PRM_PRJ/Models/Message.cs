using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; }

        public Guid SenderId { get; set; }
        public User Sender { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
