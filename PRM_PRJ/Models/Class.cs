using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class Class
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string Name { get; set; } = string.Empty;

        public Guid? HomeroomTeacherId { get; set; }
        public Teacher? HomeroomTeacher { get; set; }

        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}