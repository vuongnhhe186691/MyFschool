using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class Semester
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public int SchoolYearId { get; set; }
        public SchoolYear SchoolYear { get; set; } = null!;

        public ICollection<Score> Scores { get; set; } = new List<Score>();
    }
}