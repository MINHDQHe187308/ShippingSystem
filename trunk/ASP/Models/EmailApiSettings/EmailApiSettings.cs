namespace ASP.Models.Front
{
    public class EmailApiSettings
    {
        public string TokenEndpoint { get; set; } = string.Empty;
        public string SendEndpoint { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
    }
}