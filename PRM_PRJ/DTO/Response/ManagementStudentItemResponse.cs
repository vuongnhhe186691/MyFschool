namespace PRM_PRJ.DTO.Response
{
    public class ManagementStudentItemResponse
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string? UserId { get; set; }

        public List<string> ParentIds { get; set; } = new();

        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; }
    }
}