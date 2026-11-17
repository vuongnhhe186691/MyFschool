using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class Student
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        [Required]
        public int ClassId { get; set; }
        public Class Class { get; set; } = null!;

        public Guid? UserId { get; set; }
        public User? User { get; set; }

        [Required, MaxLength(20)]
        public string StudentCode { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<Score> Scores { get; set; } = new List<Score>();

        // 1 học sinh có thể có nhiều phụ huynh
        public ICollection<StudentParent> StudentParents { get; set; } = new List<StudentParent>();
    }
}