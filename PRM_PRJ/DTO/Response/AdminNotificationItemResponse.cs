namespace PRM_PRJ.DTO.Response
{
    public class AdminNotificationItemResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsRead { get; set; }
    }
}