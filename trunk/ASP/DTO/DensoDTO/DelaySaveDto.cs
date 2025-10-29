using ASP.Models.Front;
namespace ASP.DTO.DensoDTO
{
    public class DelaySaveDto
    {
        public string OrderId { get; set; }
        public short  DelayType { get; set; }
        public string Reason { get; set; }
        public string StartTime { get; set; }
        public string ChangeTime { get; set; }
        public double DelayTime { get; set; }
    } 
}