namespace Newsletter.Services
{
    public interface IEmailService
    {
        //  Task SendAsync(string to, string subject, string htmlBody, string? textBody = null);
        Task SendEmailConfirmationAsync(string toEmail, string confirmationLink);
        Task SendPasswordResetAsync(string toEmail, string resetLink);
        Task SendPasswordChangedNoticeAsync(string toEmail);
    }
}
