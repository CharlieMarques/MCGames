using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Newsletter.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task SendEmailConfirmationAsync(string toEmail, string confirmationLink)
        {
            var smtpServer = _configuration["EmailSettings:Server"];
            var smtpPort = int.Parse(_configuration["EmailSettings:Port"] ?? "465");
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderName = _configuration["EmailSettings:SenderName"];
            var password = _configuration["EmailSettings:Password"];

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Confirma tu cuenta en MCGames";
            //{confirmationLink}
            var bodyBuilder = new BodyBuilder
            {
                TextBody = "Un texto de prueba"
               /* HtmlBody = $@"
                    <div style='background-color: #101218; color:#ffffff; padding: 30px; font-family: Arial, sans-serif; border-radius: 12px; max-width: 600px; margin: auto; border: 1px solid #2d2f38;'> >
                        <h2 style='color: #0b57d0; text-align: center;'>¡Bienvenido a MCGames!</h2>
                        <p style ='Color: #c4c7c5; font-size 1rem;'>Gracias por registrarte. Para activar tu cuenta y acceder a todas las funciones de la plataforma, por favor confirmá tu correo elentrónico haciendo clic en el siguiente botón:</p>
                        <div style = 'text-align: center; margin: 35px 0;'>
                        <a href='hola' style='background-color: #0b57d0; color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 1rem; display: inline-block;'>
                        Confirmar mi correo
                        </a>
                        </div>
                        <p style='font-size: 0.85rem; color: #80858c; text-align: center;'> Si no creaste esta cuenta en MCGames, simplemente ignorá este mensaje.</p>
                    </div>",
                TextBody = $"¡Bienvenido a MCGames! Gracias por registrarte. Para activar tu cuenta, por favor ingresa a este enlace: {confirmationLink} . Si no creaste esta cuenta, simplemente ignora este mensaje."
               */

            };

            message.Body = bodyBuilder.ToMessageBody();
            
            using var client = new SmtpClient();
            await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(senderEmail, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

        }
    }
}
