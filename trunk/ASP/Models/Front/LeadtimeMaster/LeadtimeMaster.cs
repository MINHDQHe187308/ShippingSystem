using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;  // Thêm using này
using ASP.Models.Admin;

namespace ASP.Models.Front
{
    public partial class LeadtimeMaster : BaseEntity
    {
        [Key, Column(Order = 0)]  
        [MaxLength(5)]
        public string CustomerCode { get; set; } = null!;

        [Key, Column(Order = 1)] 
        [MaxLength(5)] 
        public string TransCd { get; set; } = null!;

        public double CollectTimePerPallet { get; set; }
        public double PrepareTimePerPallet { get; set; }
        public double LoadingTimePerColumn { get; set; }
        public string CreateBy { get; set; } = null!;
        public string? UpdateBy { get; set; } = null!;  // Làm nullable cho nhất quán

        // Navigation: FK rõ ràng
        [ForeignKey("CustomerCode")]
        public virtual Customer Customer { get; set; } = null!;
    }
}