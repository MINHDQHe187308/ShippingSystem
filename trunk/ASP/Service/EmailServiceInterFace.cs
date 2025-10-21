namespace ASP.Services  
{
    public interface EmailServiceInterface
    {
        Task SendEmailAsync(string to, string cc, string subject, string body, bool isHtml = true);
       // Task SendEmailSMTP(string to, string cc, string subject, string body, bool isHtml = true);
    }
}