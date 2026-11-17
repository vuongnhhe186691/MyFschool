using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class Timetable
    {
        [Key]
        public int Id { get; set; }

        public int ClassId { get; set; }
        public Class Class { get; set; } = null!;

        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;

        public Guid TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;

        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public DateTime WeekStartDate { get; set; }

        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}