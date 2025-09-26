using System;
using System.Collections.Generic;

namespace ASP.DTO.DensoDTO
{
    public class OrderDTO
    {
        public Guid OrderId { get; set; }
        public DateTime CreateDate { get; set; }
        public string CustomerCode { get; set; }
        public DateTime ShippingDate { get; set; }
        public string PartNo { get; set; }
        public bool IsAllocatePallet { get; set; }
        public int Quantity { get; set; }
        public int PalletSize { get; set; }
        public int TotalPallet { get; set; }
        public string TransCode { get; set; }
        public string Warehouse { get; set; }
        public string Pic { get; set; }
        public int BookContId { get; set; }
        public int ContId { get; set; }
        public List<PalletDTO> Pallets { get; set; }
    }

    public class PalletDTO
    {
        public string PartNo { get; set; }
        public int PalletNo { get; set; }
        public string Location { get; set; }
        public string Area { get; set; }
        public int Status { get; set; }
    }
}