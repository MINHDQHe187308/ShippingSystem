using System;
using System.Collections.Generic;
using ASP.Models.Admin;
namespace ASP.Models.Front.ThemeOption
{
    public partial class ThemeOption : BaseEntity   
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Value { get; set; }
        public string? TypeData { get; set; }
        public string? Language { get; set; }
   
    }
}
