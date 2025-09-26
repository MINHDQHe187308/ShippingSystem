namespace ASP.DTO.DensoDTO
{
    public class ApiResponse<T>
    {
        public int Status { get; set; }
        public string Message { get; set; }
        public T[] data { get; set; }
    }
}