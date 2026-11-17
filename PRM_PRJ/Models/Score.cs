using PRM_PRJ.Enums;
using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class Score
    {
        [Key]
        public int Id { get; set; }

        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;

        public int SemesterId { get; set; }
        public Semester Semester { get; set; } = null!;

        public ScoreType Type { get; set; }

        public double? Value { get; set; }

        [MaxLength(20)]
        public string? Grade { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}