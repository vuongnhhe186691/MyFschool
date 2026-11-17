using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class Meal
    {
        [Key]
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public string Menu { get; set; }

        public ICollection<StudentMeal> StudentMeals { get; set; }
    }
}
