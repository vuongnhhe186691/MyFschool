namespace PRM_PRJ.Models
{
    public class TeacherSubject
    {
        public Guid TeacherId { get; set; }
        public Teacher Teacher { get; set; }

        public int SubjectId { get; set; }
        public Subject Subject { get; set; }
    }
}
