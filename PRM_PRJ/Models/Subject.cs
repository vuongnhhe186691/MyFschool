using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class Subject
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
    }
}