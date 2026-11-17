namespace PRM_PRJ.DTO.Response
{
    public class ManagementTimetableItemResponse
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string DayOfWeek { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string WeekStartDate { get; set; } = string.Empty;
    }
}