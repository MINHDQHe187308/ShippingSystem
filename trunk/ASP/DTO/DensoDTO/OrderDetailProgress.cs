// File: ASP.DTO.DensoDTO/OrderDetailProgress.cs (Cập nhật để sử dụng BookContStatus và PalletStatus mapping)
namespace ASP.DTO.DensoDTO
{
    public class OrderDetailProgress
    {
        public Guid UId { get; set; }
        public string PartNo { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int TotalPallet { get; set; }
        public string Warehouse { get; set; } = string.Empty;
        public int ContNo { get; set; }
        public short BookContStatus { get; set; }
        public double CollectPercent { get; set; }
        public double PreparePercent { get; set; }
        public double LoadingPercent { get; set; }
        public string CurrentStage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}