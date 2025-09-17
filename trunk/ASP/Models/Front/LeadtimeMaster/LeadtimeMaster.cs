using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ASP.Models.Admin; 
namespace ASP.Models.Front;

public partial class LeadtimeMaster : BaseEntity
{
    [Key]
    public string CustomerCode { get; set; } = null!;
    
    public string TransCd { get; set; } = null!;
    
    public double CollectTimePerPallet { get; set; }
    
    public double PrepareTimePerPallet { get; set; }
    
    public double LoadingTimePerColumn { get; set; }
    
    public string CreateBy { get; set; } = null!;
   
    public string UpdateBy { get; set; } = null!;

}

