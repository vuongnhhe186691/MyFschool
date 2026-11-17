namespace PRM_PRJ.DTO.Response
{
    public class AcademicLookupResponse
    {
        public List<LookupOptionResponse> Students { get; set; } = new();
        public List<LookupOptionResponse> Subjects { get; set; } = new();
        public List<LookupOptionResponse> Semesters { get; set; } = new();
        public List<LookupOptionResponse> Classes { get; set; } = new();
        public List<LookupOptionResponse> Teachers { get; set; } = new();
    }
}