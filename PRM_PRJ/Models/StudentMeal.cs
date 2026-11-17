using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class StudentMeal
    {
        [Key]
        public int Id { get; set; }

        public Guid StudentId { get; set; }
        public Student Student { get; set; }

        public int MealId { get; set; }
        public Meal Meal { get; set; }

        public bool IsRegistered { get; set; }
    }
}
