using System;
using System.Collections.Generic;

namespace ASP.Models.Front.Customer
{
    public partial class Customer
    {
        public string CustomerCode { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public string Descriptions { get; set; } = null!;
        public string CreateBy { get; set; } = null!;
        public string? UpdateBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
