namespace PRM_PRJ.DTO.Request
{
    public class ImportStudentRowRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public int ClassId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string? UserId { get; set; }

        // ví dụ: "guid1;guid2"
        public string? ParentIds { get; set; }

        public string? AvatarUrl { get; set; }
        public bool? IsActive { get; set; }
    }
}