using ASP.Models.Front;
namespace ASP.DTO.DensoDTO
{
    public class LeadtimeMasterDTO
    {
        public string CustomerCode { get; set; } = null!;
        public string TransCd { get; set; } = null!;
        public double CollectTimePerPallet { get; set; }
        public double PrepareTimePerPallet { get; set; }
        public double LoadingTimePerColumn { get; set; }
        public string CreateBy { get; set; } = null!;
        public string UpdateBy { get; set; } = null!;
    }
}
