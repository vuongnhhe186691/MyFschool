namespace PRM_PRJ.DTO.Request
{
    public class ImportAssignStudentRowRequest
    {
        public string? StudentCode { get; set; }
        public string? StudentId { get; set; }
        public int TargetClassId { get; set; }
    }
}