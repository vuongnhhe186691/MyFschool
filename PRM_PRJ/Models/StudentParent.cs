namespace PRM_PRJ.Models
{
    public class StudentParent
    {
        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public Guid ParentUserId { get; set; }
        public User ParentUser { get; set; } = null!;
    }
}