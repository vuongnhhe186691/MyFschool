using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class Conversation
    {
        [Key]
        public int Id { get; set; }

        public Guid StudentId { get; set; }
        public Student Student { get; set; }

        public ICollection<Message> Messages { get; set; }
    }
}
