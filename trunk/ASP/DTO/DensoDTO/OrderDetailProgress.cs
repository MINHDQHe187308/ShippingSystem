namespace ASP.DTO.DensoDTO
{
    public class OrderDetailProgress
    {
        public Guid UId { get; set; }  // ID của OrderDetail
        public string PartNo { get; set; } = null!;
        public int Quantity { get; set; }
        public int TotalPallet { get; set; }
        public string Warehouse { get; set; } = null!;
        public int ContNo { get; set; }
        public short BookContStatus { get; set; }
        public double CollectPercent { get; set; }
        public double PreparePercent { get; set; }
        public double LoadingPercent { get; set; }
        public string CurrentStage { get; set; } = null!;
        public string Status { get; set; } = null!;  // Text từ BookContStatus (e.g., "Chưa xuất")
    }
}