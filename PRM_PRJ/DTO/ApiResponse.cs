namespace PRM_PRJ.DTO
{
    public class ApiResponse<T>
    {
        public string Status { get; set; }

        public int Code { get; set; }

        public T Data { get; set; }
    }
}
