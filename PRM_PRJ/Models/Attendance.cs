using PRM_PRJ.Enums;
using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        public Guid StudentId { get; set; }
        public Student Student { get; set; }

        public int TimetableId { get; set; }
        public Timetable Timetable { get; set; }

        public DateTime Date { get; set; }

        public AttendanceStatus Status { get; set; }

        public string? Note { get; set; }
    }
}
