using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ASP.Models.Admin;
namespace ASP.Models.Front;

public partial class Customer : BaseEntity
{
    [Key]
    public string CustomerCode { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string Descriptions { get; set; } = null!;
    public string CreateBy { get; set; } = null!;
    public string? UpdateBy { get; set; }
   
}
