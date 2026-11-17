namespace PRM_PRJ.DTO.Response
{
    public class GradeItemResponse
    {
        public int GradeId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string AssessmentType { get; set; } = string.Empty;
        public double Score { get; set; }
        public string? Grade { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}