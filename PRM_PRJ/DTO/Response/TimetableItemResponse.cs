namespace PRM_PRJ.DTO.Response
{
    public class TimetableItemResponse
    {
        public int Id { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string DayOfWeek { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string WeekStartDate { get; set; } = string.Empty;
        public string LessonDate { get; set; } = string.Empty;
    }
}
