using System.Text.Json.Serialization;

namespace ASP.DTO.DensoDTO
{
    public class DeliveryResponse<T>
    {
        [JsonPropertyName("Status")]
        public int Status { get; set; }
        [JsonPropertyName("Message")]
        public string Message { get; set; }
        [JsonPropertyName("Data")]
        public List<T> Data { get; set; } = new List<T>();
    }
}