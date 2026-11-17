namespace PRM_PRJ.DTO.Request
{
    public class UpsertGradeRequest
    {
        public Guid StudentId { get; set; }
        public int SubjectId { get; set; }
        public int SemesterId { get; set; }
        public string AssessmentType { get; set; } = string.Empty;
        public double Score { get; set; }
        public string? Grade { get; set; }
    }
}