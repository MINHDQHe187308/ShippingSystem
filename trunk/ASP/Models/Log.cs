namespace ASP.Models
{
    public partial class Log : ASP.Models.Admin.BaseEntity
    {
        public long Id { get; set; }
        public string LogType { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string Author { get; set; } = null!;
        public string Ip { get; set; } = null!;
    }
}
