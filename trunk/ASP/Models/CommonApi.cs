using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASP.Models
{
    public class CommonApi<T>
    {
        public int? status_code { get; set; }
        public string messages { get; set; }
        public int total_record { get; set; }
        public List<T> data { get; set; }
    }
}