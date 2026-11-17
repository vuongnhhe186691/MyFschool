using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class Pickup
    {
        [Key]
        public int Id { get; set; }

        public Guid StudentId { get; set; }
        public Student Student { get; set; }

        public DateTime Date { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } // Picked / NotPicked

        public string? Note { get; set; }
    }
}
