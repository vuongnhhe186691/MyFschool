using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class SchoolYear
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public ICollection<Semester> Semesters { get; set; } = new List<Semester>();
    }
}