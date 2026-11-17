namespace PRM_PRJ.DTO.Response
{
    public class EventItemResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EventDate { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }
}