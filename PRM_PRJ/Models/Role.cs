using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public ICollection<UserRole> UserRoles { get; set; }
    }
}