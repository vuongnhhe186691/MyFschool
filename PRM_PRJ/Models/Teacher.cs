using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class Teacher
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        public string? AvatarUrl { get; set; }

        public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
    }
}