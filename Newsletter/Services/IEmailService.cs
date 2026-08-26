namespace Newsletter.Services
{
    public interface IEmailService
    {
        //  Task SendAsync(string to, string subject, string htmlBody, string? textBody = null);
        Task SendEmailConfirmationAsync(string toEmail, string confirmationLink);
    }
}
