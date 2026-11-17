namespace PRM_PRJ.DTO.Request
{
    public class UpsertTimetableRequest
    {
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public Guid TeacherId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string WeekStartDate { get; set; } = string.Empty;
    }
}