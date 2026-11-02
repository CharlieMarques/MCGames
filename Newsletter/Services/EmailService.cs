using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Utils;

namespace Newsletter.Services
{
    public class EmailService : IEmailService
    {
        private const int SmtpTimeoutMs = 30000;
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task SendEmailConfirmationAsync(string toEmail, string confirmationLink) =>
            SendAsync(s => BuildConfirmationMessage(s.SenderName, s.SenderEmail, toEmail, confirmationLink));

        public Task SendPasswordResetAsync(string toEmail, string resetLink) =>
            SendAsync(s => BuildPasswordResetMessage(s.SenderName, s.SenderEmail, toEmail, resetLink));

        public Task SendPasswordChangedNoticeAsync(string toEmail) =>
            SendAsync(s => BuildPasswordChangedMessage(s.SenderName, s.SenderEmail, toEmail));

        // ---------- Armado de cada email (público y estático para poder probarlo sin servidor SMTP) ----------

        public static MimeMessage BuildConfirmationMessage(string senderName, string senderEmail, string toEmail, string confirmationLink)
        {
            ValidateLink(confirmationLink, nameof(confirmationLink));
            var safeLink = WebUtility.HtmlEncode(confirmationLink);

            var html = $@"
    <h2 style='color:#0b57d0; text-align:center;'>¡Bienvenido a MCGames!</h2>
    <p style='color:#c4c7c5; font-size:1rem;'>Gracias por registrarte. Para activar tu cuenta y acceder a todas las funciones de la plataforma, confirmá tu correo electrónico haciendo clic en el siguiente botón:</p>
    {Button(safeLink, "Confirmar mi correo")}
    {FallbackLink(safeLink)}
    {SmallText("Si no creaste esta cuenta en MCGames, simplemente ignorá este mensaje.")}";

            var text = $"¡Bienvenido a MCGames! Gracias por registrarte. Para activar tu cuenta, ingresá a este enlace: {confirmationLink}\n\n" +
                       "Si no creaste esta cuenta, simplemente ignorá este mensaje.";

            return CreateMessage(senderName, senderEmail, toEmail, "Confirmá tu cuenta en MCGames", html, text);
        }

        public static MimeMessage BuildPasswordResetMessage(string senderName, string senderEmail, string toEmail, string resetLink)
        {
            ValidateLink(resetLink, nameof(resetLink));
            var safeLink = WebUtility.HtmlEncode(resetLink);

            var html = $@"
    <h2 style='color:#0b57d0; text-align:center;'>Restablecer tu contraseña</h2>
    <p style='color:#c4c7c5; font-size:1rem;'>Recibimos un pedido para restablecer la contraseña de tu cuenta de MCGames. Hacé clic en el botón para elegir una nueva. El enlace vence en 2 horas y sirve una sola vez.</p>
    {Button(safeLink, "Elegir nueva contraseña")}
    {FallbackLink(safeLink)}
    {SmallText("Si no pediste este cambio, ignorá este mensaje: tu contraseña actual sigue funcionando.")}";

            var text = $"Recibimos un pedido para restablecer la contraseña de tu cuenta de MCGames. Para elegir una nueva, ingresá a este enlace (vence en 2 horas y sirve una sola vez): {resetLink}\n\n" +
                       "Si no pediste este cambio, ignorá este mensaje: tu contraseña actual sigue funcionando.";

            return CreateMessage(senderName, senderEmail, toEmail, "Restablecé tu contraseña de MCGames", html, text);
        }

        public static MimeMessage BuildPasswordChangedMessage(string senderName, string senderEmail, string toEmail)
        {
            var html = $@"
    <h2 style='color:#0b57d0; text-align:center;'>Tu contraseña fue cambiada</h2>
    <p style='color:#c4c7c5; font-size:1rem;'>Te avisamos que la contraseña de tu cuenta de MCGames se cambió recientemente.</p>
    <p style='color:#c4c7c5; font-size:1rem;'>Si fuiste vos, no tenés que hacer nada.</p>
    <p style='color:#c4c7c5; font-size:1rem;'>Si no fuiste vos, recuperá el acceso con la opción ""¿La olvidaste?"" en la pantalla de inicio de sesión.</p>
    {SmallText("Este es un aviso automático de seguridad.")}";

            var text = "Te avisamos que la contraseña de tu cuenta de MCGames se cambió recientemente.\n\n" +
                       "Si fuiste vos, no tenés que hacer nada. Si no fuiste vos, recuperá el acceso con la opción \"¿La olvidaste?\" en la pantalla de inicio de sesión.";

            return CreateMessage(senderName, senderEmail, toEmail, "Tu contraseña de MCGames fue cambiada", html, text);
        }

        // ---------- Partes comunes ----------

        private static MimeMessage CreateMessage(string senderName, string senderEmail, string toEmail, string subject, string htmlContent, string textBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(string.Empty, toEmail));
            message.Subject = subject;
            message.MessageId = MimeUtils.GenerateMessageId("mcgames.com.ar");

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
<div style='background-color:#101218; color:#ffffff; padding:30px; font-family:Arial, sans-serif; border-radius:12px; max-width:600px; margin:auto; border:1px solid #2d2f38;'>{htmlContent}
</div>",
                TextBody = textBody
            };
            message.Body = bodyBuilder.ToMessageBody();
            return message;
        }

        private static string Button(string safeLink, string text) => $@"
    <div style='text-align:center; margin:35px 0;'>
        <a href='{safeLink}' style='background-color:#0b57d0; color:#ffffff; padding:14px 28px; text-decoration:none; border-radius:8px; font-weight:bold; font-size:1rem; display:inline-block;'>{text}</a>
    </div>";

        private static string FallbackLink(string safeLink) =>
            $"<p style='font-size:0.85rem; color:#80858c; text-align:center;'>Si el botón no funciona, copiá y pegá este enlace en tu navegador:<br>{safeLink}</p>";

        private static string SmallText(string text) =>
            $"<p style='font-size:0.85rem; color:#80858c; text-align:center;'>{text}</p>";

        private static void ValidateLink(string link, string paramName)
        {
            if (!Uri.TryCreate(link, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("El link tiene que ser una URL absoluta http o https.", paramName);
            }
        }

        // ---------- Envío ----------

        private sealed record EmailSettings(string Server, int Port, string SenderEmail, string SenderName, string Password);

        private async Task SendAsync(Func<EmailSettings, MimeMessage> buildMessage)
        {
            var settings = ReadSettings();
            var message = buildMessage(settings);

            using var client = new SmtpClient { Timeout = SmtpTimeoutMs };
            var security = settings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await client.ConnectAsync(settings.Server, settings.Port, security);
            await client.AuthenticateAsync(settings.SenderEmail, settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private EmailSettings ReadSettings()
        {
            var server = _configuration["EmailSettings:Server"];
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var password = _configuration["EmailSettings:Password"];
            var senderName = _configuration["EmailSettings:SenderName"];
            var portText = _configuration["EmailSettings:Port"];

            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(server)) missing.Add("Server");
            if (string.IsNullOrWhiteSpace(senderEmail)) missing.Add("SenderEmail");
            if (string.IsNullOrWhiteSpace(password)) missing.Add("Password");

            int port = 465;
            if (!string.IsNullOrWhiteSpace(portText) && !int.TryParse(portText, out port))
            {
                missing.Add("Port (no es un número)");
            }

            if (missing.Count > 0)
            {
                // Solo nombres de claves, nunca valores: el mensaje puede terminar en un log.
                throw new InvalidOperationException("Falta configuración de EmailSettings: " + string.Join(", ", missing));
            }

            return new EmailSettings(server!, port, senderEmail!, string.IsNullOrWhiteSpace(senderName) ? "MCGames" : senderName, password!);
        }
    }
}
