using Microsoft.Extensions.Configuration;
using Newsletter.Services;

namespace Newsletter.UnitTests.Services
{
    public class EmailServiceTests
    {
        private const string Link = "https://www.mcgames.com.ar/pages/ConfirmarEmail.html?userId=abc&token=xyz";

        private static EmailService CreateService(Dictionary<string, string?> settings) =>
            new(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

        // ---------- BuildConfirmationMessage ----------

        [Fact]
        public void BuildConfirmationMessage_IncluyeElLinkEnElHtmlYEnElTextoPlano()
        {
            var message = EmailService.BuildConfirmationMessage("MCGames", "no-reply@mcgames.com.ar", "user@test.com", Link);

            // En el HTML el & va escapado como &amp;, que es lo correcto dentro de un atributo.
            Assert.Contains("href='https://www.mcgames.com.ar/pages/ConfirmarEmail.html?userId=abc&amp;token=xyz'", message.HtmlBody);
            Assert.Contains(Link, message.TextBody);
            Assert.DoesNotContain("href='hola'", message.HtmlBody);
        }

        [Fact]
        public void BuildConfirmationMessage_ArmaRemitenteDestinatarioYAsunto()
        {
            var message = EmailService.BuildConfirmationMessage("MCGames", "no-reply@mcgames.com.ar", "user@test.com", Link);

            var from = Assert.Single(message.From.Mailboxes);
            Assert.Equal("MCGames", from.Name);
            Assert.Equal("no-reply@mcgames.com.ar", from.Address);
            Assert.Equal("user@test.com", Assert.Single(message.To.Mailboxes).Address);
            Assert.Equal("Confirmá tu cuenta en MCGames", message.Subject);
        }

        [Theory]
        [InlineData("?userId=abc&token=xyz")]
        [InlineData("/pages/ConfirmarEmail.html?userId=abc")]
        [InlineData("javascript:alert(1)")]
        [InlineData("")]
        public void BuildConfirmationMessage_ConLinkInvalido_LanzaArgumentException(string link)
        {
            Assert.Throws<ArgumentException>(() =>
                EmailService.BuildConfirmationMessage("MCGames", "no-reply@mcgames.com.ar", "user@test.com", link));
        }

        // ---------- Configuración ----------

        [Fact]
        public async Task SendEmailConfirmationAsync_SinConfiguracion_LanzaInvalidOperationExceptionConLasClavesFaltantes()
        {
            var service = CreateService(new Dictionary<string, string?>());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SendEmailConfirmationAsync("user@test.com", Link));

            Assert.Contains("Server", ex.Message);
            Assert.Contains("SenderEmail", ex.Message);
            Assert.Contains("Password", ex.Message);
        }

        [Fact]
        public async Task SendEmailConfirmationAsync_ConPuertoInvalido_LanzaInvalidOperationExceptionSinMostrarLaContrasena()
        {
            var service = CreateService(new Dictionary<string, string?>
            {
                ["EmailSettings:Server"] = "smtp.test.com",
                ["EmailSettings:SenderEmail"] = "no-reply@mcgames.com.ar",
                ["EmailSettings:Password"] = "secreto-123",
                ["EmailSettings:Port"] = "abc"
            });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SendEmailConfirmationAsync("user@test.com", Link));

            Assert.Contains("Port", ex.Message);
            Assert.DoesNotContain("secreto-123", ex.Message);
        }

        // ---------- Recuperación de contraseña ----------

        private const string ResetLink = "https://www.mcgames.com.ar/pages/RecuperarContrasena.html?userId=abc&token=xyz";

        [Fact]
        public void BuildPasswordResetMessage_IncluyeElLinkEnElHtmlYEnElTextoPlano()
        {
            var message = EmailService.BuildPasswordResetMessage("MCGames", "no-reply@mcgames.com.ar", "user@test.com", ResetLink);

            Assert.Contains("href='https://www.mcgames.com.ar/pages/RecuperarContrasena.html?userId=abc&amp;token=xyz'", message.HtmlBody);
            Assert.Contains(ResetLink, message.TextBody);
        }

        [Fact]
        public void BuildPasswordResetMessage_TieneElAsuntoYAvisaQueVenceEnDosHoras()
        {
            var message = EmailService.BuildPasswordResetMessage("MCGames", "no-reply@mcgames.com.ar", "user@test.com", ResetLink);

            Assert.Equal("Restablecé tu contraseña de MCGames", message.Subject);
            Assert.Contains("2 horas", message.HtmlBody);
            Assert.Contains("2 horas", message.TextBody);
        }

        [Fact]
        public void BuildPasswordResetMessage_ConLinkRelativo_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                EmailService.BuildPasswordResetMessage("MCGames", "no-reply@mcgames.com.ar", "user@test.com", "/pages/RecuperarContrasena.html"));
        }

        // ---------- Aviso de contraseña cambiada ----------

        [Fact]
        public void BuildPasswordChangedMessage_TieneElAsuntoYNoIncluyeLinks()
        {
            var message = EmailService.BuildPasswordChangedMessage("MCGames", "no-reply@mcgames.com.ar", "user@test.com");

            Assert.Equal("Tu contraseña de MCGames fue cambiada", message.Subject);
            Assert.Equal("user@test.com", Assert.Single(message.To.Mailboxes).Address);
            Assert.DoesNotContain("href", message.HtmlBody);
        }

        [Fact]
        public async Task SendPasswordChangedNoticeAsync_SinConfiguracion_LanzaInvalidOperationException()
        {
            var service = CreateService(new Dictionary<string, string?>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SendPasswordChangedNoticeAsync("user@test.com"));
        }
    }
}
