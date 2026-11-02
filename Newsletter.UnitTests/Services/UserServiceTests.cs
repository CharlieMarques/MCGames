using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newsletter.DTOs.Users;
using Newsletter.Models;
using Newsletter.Services;

namespace Newsletter.UnitTests.Services
{
    public class UserServiceTests
    {
        private const string FrontendUrl = "https://www.mcgames.com.ar/pages/ConfirmarEmail.html";
        private const string IdentityToken = "token+con/caracteres=";

        private readonly Mock<UserManager<User>> _userManager;
        private readonly Mock<RoleManager<IdentityRole>> _roleManager;
        private readonly Mock<ILibraryService> _libraryService = new();
        private readonly Mock<IEmailService> _emailService = new();
        private const string ResetUrl = "https://www.mcgames.com.ar/pages/RecuperarContrasena.html";
        private readonly Dictionary<string, string?> _settings = new()
        {
            ["FrontendUrl:Url"] = FrontendUrl,
            ["FrontendUrl:ResetPasswordUrl"] = ResetUrl
        };
        private readonly List<string> _calls = new();
        private string? _sentLink;

        public UserServiceTests()
        {
            _userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            _roleManager = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(), null!, null!, null!, null!);

            _userManager.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .Callback<User, string>((user, _) =>
                {
                    user.Id = "user-1";
                    _calls.Add("crear");
                })
                .ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "Usuario"))
                .Callback(() => _calls.Add("rol"))
                .ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityToken);
            _libraryService.Setup(l => l.CreateLibraryAsync(It.IsAny<string>()))
                .Callback(() => _calls.Add("biblioteca"))
                .ReturnsAsync((true, string.Empty, (Guid?)Guid.NewGuid()));
            _emailService.Setup(e => e.SendEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((_, link) =>
                {
                    _sentLink = link;
                    _calls.Add("email");
                })
                .Returns(Task.CompletedTask);
        }

        private UserService CreateService() => new(
            _userManager.Object,
            _roleManager.Object,
            new ConfigurationBuilder().AddInMemoryCollection(_settings).Build(),
            _libraryService.Object,
            _emailService.Object,
            NullLogger<UserService>.Instance);

        private static UserRegisterDto Registro() => new()
        {
            UserName = "gamer",
            Email = "gamer@test.com",
            Password = "secreto1",
            ConfirmPassword = "secreto1"
        };

        [Fact]
        public async Task RegisterAsync_AsignaRolYBibliotecaAntesDeMandarElEmail()
        {
            var (success, _) = await CreateService().RegisterAsync(Registro());

            Assert.True(success);
            Assert.Equal(new[] { "crear", "rol", "biblioteca", "email" }, _calls);
        }

        [Fact]
        public async Task RegisterAsync_ArmaElLinkConFrontendUrlUserIdYToken()
        {
            await CreateService().RegisterAsync(Registro());

            var expectedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(IdentityToken));
            Assert.Equal($"{FrontendUrl}?userId=user-1&token={expectedToken}", _sentLink);
            Assert.DoesNotContain("/ConfirmEmail", _sentLink);
        }

        [Fact]
        public async Task RegisterAsync_CuandoFrontendUrlYaTieneParametros_AgregaConAmpersand()
        {
            _settings["FrontendUrl:Url"] = FrontendUrl + "?origen=email";

            await CreateService().RegisterAsync(Registro());

            Assert.StartsWith(FrontendUrl + "?origen=email&userId=user-1&token=", _sentLink);
        }

        [Fact]
        public async Task RegisterAsync_CuandoElEmailFalla_IgualDevuelveExitoConRolYBiblioteca()
        {
            _emailService.Setup(e => e.SendEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("SMTP caído"));

            var (success, _) = await CreateService().RegisterAsync(Registro());

            Assert.True(success);
            Assert.Contains("rol", _calls);
            Assert.Contains("biblioteca", _calls);
        }

        [Fact]
        public async Task RegisterAsync_SinFrontendUrl_NoMandaElEmailPeroRegistraIgual()
        {
            _settings.Clear();

            var (success, _) = await CreateService().RegisterAsync(Registro());

            Assert.True(success);
            _emailService.Verify(e => e.SendEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ConContrasenasDistintas_NoCreaElUsuario()
        {
            var dto = Registro();
            dto.ConfirmPassword = "otra";

            var (success, errors) = await CreateService().RegisterAsync(dto);

            Assert.False(success);
            Assert.Contains("Las contraseñas no coinciden", errors);
            _userManager.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_CuandoIdentityRechazaElUsuario_DevuelveLosErroresSinRolNiEmail()
        {
            _userManager.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "El nombre ya está en uso" }));

            var (success, errors) = await CreateService().RegisterAsync(Registro());

            Assert.False(success);
            Assert.Contains("El nombre ya está en uso", errors);
            _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
            _emailService.Verify(e => e.SendEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        // ---------- LoginAsync ----------

        private User UsuarioExistente(bool emailConfirmado)
        {
            var user = new User { Id = "user-1", UserName = "gamer", Email = "gamer@test.com", EmailConfirmed = emailConfirmado };
            _userManager.Setup(m => m.FindByNameAsync("gamer")).ReturnsAsync(user);
            _userManager.Setup(m => m.CheckPasswordAsync(user, "secreto1")).ReturnsAsync(true);
            _userManager.Setup(m => m.CheckPasswordAsync(user, "incorrecta")).ReturnsAsync(false);
            _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Usuario" });
            return user;
        }

        [Fact]
        public async Task LoginAsync_ConUsuarioInexistente_DevuelveMensajeGenerico()
        {
            _userManager.Setup(m => m.FindByNameAsync("nadie")).ReturnsAsync((User?)null);

            var result = await CreateService().LoginAsync(new UserLoginDto { UserName = "nadie", Password = "x" });

            Assert.False(result.success);
            Assert.False(result.EmailNotConfirmed);
            Assert.Equal("Usuario o contraseña incorrectos", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginAsync_ConContrasenaIncorrectaYEmailSinConfirmar_NoRevelaQueFaltaConfirmar()
        {
            UsuarioExistente(emailConfirmado: false);

            var result = await CreateService().LoginAsync(new UserLoginDto { UserName = "gamer", Password = "incorrecta" });

            Assert.False(result.success);
            Assert.False(result.EmailNotConfirmed);
            Assert.Equal("Usuario o contraseña incorrectos", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginAsync_ConEmailSinConfirmar_DevuelveEmailNotConfirmedSinToken()
        {
            UsuarioExistente(emailConfirmado: false);

            var result = await CreateService().LoginAsync(new UserLoginDto { UserName = "gamer", Password = "secreto1" });

            Assert.False(result.success);
            Assert.True(result.EmailNotConfirmed);
            Assert.Empty(result.Token);
        }

        [Fact]
        public async Task LoginAsync_ConEmailConfirmado_DevuelveUnToken()
        {
            UsuarioExistente(emailConfirmado: true);
            _settings["Jwt:Key"] = "clave-de-prueba-de-al-menos-32-caracteres-123456";
            _settings["Jwt:Issuer"] = "MyApiGames";
            _settings["Jwt:Audience"] = "MyGamesUsers";

            var result = await CreateService().LoginAsync(new UserLoginDto { UserName = "gamer", Password = "secreto1" });

            Assert.True(result.success);
            Assert.False(result.EmailNotConfirmed);
            Assert.False(string.IsNullOrEmpty(result.Token));
        }

        // ---------- ResendConfirmationEmailAsync ----------

        [Fact]
        public async Task ResendConfirmationEmailAsync_ConEmailInexistente_NoMandaNada()
        {
            _userManager.Setup(m => m.FindByEmailAsync("nadie@test.com")).ReturnsAsync((User?)null);

            await CreateService().ResendConfirmationEmailAsync("nadie@test.com");

            _emailService.Verify(e => e.SendEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResendConfirmationEmailAsync_ConEmailYaConfirmado_NoMandaNada()
        {
            _userManager.Setup(m => m.FindByEmailAsync("gamer@test.com"))
                .ReturnsAsync(new User { Id = "user-1", UserName = "gamer", Email = "gamer@test.com", EmailConfirmed = true });

            await CreateService().ResendConfirmationEmailAsync("gamer@test.com");

            _emailService.Verify(e => e.SendEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResendConfirmationEmailAsync_ConEmailSinConfirmar_MandaElLinkAEseUsuario()
        {
            _userManager.Setup(m => m.FindByEmailAsync("gamer@test.com"))
                .ReturnsAsync(new User { Id = "user-7", UserName = "gamer", Email = "gamer@test.com", EmailConfirmed = false });

            await CreateService().ResendConfirmationEmailAsync("gamer@test.com");

            _emailService.Verify(e => e.SendEmailConfirmationAsync("gamer@test.com", It.IsAny<string>()), Times.Once);
            Assert.StartsWith($"{FrontendUrl}?userId=user-7&token=", _sentLink);
        }

        [Fact]
        public async Task ResendConfirmationEmailAsync_CuandoLaBusquedaFalla_NoPropagaElError()
        {
            _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Hay más de un usuario con ese email"));

            var ex = await Record.ExceptionAsync(() => CreateService().ResendConfirmationEmailAsync("gamer@test.com"));

            Assert.Null(ex);
        }

        // ---------- ForgotPasswordAsync ----------

        private User UsuarioConEmail(bool emailConfirmado, string id = "user-9")
        {
            var user = new User { Id = id, UserName = "gamer", Email = "gamer@test.com", EmailConfirmed = emailConfirmado };
            _userManager.Setup(m => m.FindByEmailAsync("gamer@test.com")).ReturnsAsync(user);
            _userManager.Setup(m => m.FindByIdAsync(id)).ReturnsAsync(user);
            _userManager.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
            return user;
        }

        private void VerificarResetEnviado(Times times) =>
            _emailService.Verify(e => e.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>()), times);

        private void VerificarAvisoEnviado(Times times) =>
            _emailService.Verify(e => e.SendPasswordChangedNoticeAsync(It.IsAny<string>()), times);

        [Fact]
        public async Task ForgotPasswordAsync_ConEmailInexistente_NoMandaNada()
        {
            _userManager.Setup(m => m.FindByEmailAsync("nadie@test.com")).ReturnsAsync((User?)null);

            await CreateService().ForgotPasswordAsync("nadie@test.com");

            VerificarResetEnviado(Times.Never());
        }

        [Fact]
        public async Task ForgotPasswordAsync_ConEmailSinConfirmar_NoMandaNada()
        {
            UsuarioConEmail(emailConfirmado: false);

            await CreateService().ForgotPasswordAsync("gamer@test.com");

            VerificarResetEnviado(Times.Never());
        }

        [Fact]
        public async Task ForgotPasswordAsync_ConEmailConfirmado_MandaElLinkDeRecuperacion()
        {
            UsuarioConEmail(emailConfirmado: true);
            string? link = null;
            _emailService.Setup(e => e.SendPasswordResetAsync("gamer@test.com", It.IsAny<string>()))
                .Callback<string, string>((_, l) => link = l)
                .Returns(Task.CompletedTask);

            await CreateService().ForgotPasswordAsync("gamer@test.com");

            var expectedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("reset-token"));
            Assert.Equal($"{ResetUrl}?userId=user-9&token={expectedToken}", link);
        }

        [Fact]
        public async Task ForgotPasswordAsync_SinResetPasswordUrl_NoMandaNadaNiLanzaError()
        {
            UsuarioConEmail(emailConfirmado: true);
            _settings.Remove("FrontendUrl:ResetPasswordUrl");

            var ex = await Record.ExceptionAsync(() => CreateService().ForgotPasswordAsync("gamer@test.com"));

            Assert.Null(ex);
            VerificarResetEnviado(Times.Never());
        }

        // ---------- ResetPasswordAsync ----------

        private static ResetPasswordDto Reset(string userId = "user-9", string? token = null) => new()
        {
            UserId = userId,
            Token = token ?? WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("reset-token")),
            NewPassword = "nueva123",
            ConfirmPassword = "nueva123"
        };

        [Fact]
        public async Task ResetPasswordAsync_ConUsuarioInexistente_DevuelveLinkInvalido()
        {
            _userManager.Setup(m => m.FindByIdAsync("nadie")).ReturnsAsync((User?)null);

            var result = await CreateService().ResetPasswordAsync(Reset(userId: "nadie"));

            Assert.False(result.success);
            Assert.True(result.InvalidLink);
        }

        [Fact]
        public async Task ResetPasswordAsync_ConTokenMalCodificado_DevuelveLinkInvalido()
        {
            UsuarioConEmail(emailConfirmado: true);

            var result = await CreateService().ResetPasswordAsync(Reset(token: "%%%"));

            Assert.False(result.success);
            Assert.True(result.InvalidLink);
        }

        [Fact]
        public async Task ResetPasswordAsync_ConTokenVencido_DevuelveLinkInvalido()
        {
            var user = UsuarioConEmail(emailConfirmado: true);
            _userManager.Setup(m => m.ResetPasswordAsync(user, "reset-token", "nueva123"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "Invalid token." }));

            var result = await CreateService().ResetPasswordAsync(Reset());

            Assert.False(result.success);
            Assert.True(result.InvalidLink);
            VerificarAvisoEnviado(Times.Never());
        }

        [Fact]
        public async Task ResetPasswordAsync_ConContrasenaQueNoCumpleLasReglas_DevuelveEsosErrores()
        {
            var user = UsuarioConEmail(emailConfirmado: true);
            _userManager.Setup(m => m.ResetPasswordAsync(user, "reset-token", "nueva123"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort", Description = "Muy corta" }));

            var result = await CreateService().ResetPasswordAsync(Reset());

            Assert.False(result.success);
            Assert.False(result.InvalidLink);
            Assert.Contains("Muy corta", result.Errors);
        }

        [Fact]
        public async Task ResetPasswordAsync_Correcto_MandaElAviso()
        {
            var user = UsuarioConEmail(emailConfirmado: true);
            _userManager.Setup(m => m.ResetPasswordAsync(user, "reset-token", "nueva123")).ReturnsAsync(IdentityResult.Success);

            var result = await CreateService().ResetPasswordAsync(Reset());

            Assert.True(result.success);
            _emailService.Verify(e => e.SendPasswordChangedNoticeAsync("gamer@test.com"), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_CuandoElAvisoFalla_IgualDevuelveExito()
        {
            var user = UsuarioConEmail(emailConfirmado: true);
            _userManager.Setup(m => m.ResetPasswordAsync(user, "reset-token", "nueva123")).ReturnsAsync(IdentityResult.Success);
            _emailService.Setup(e => e.SendPasswordChangedNoticeAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("SMTP caído"));

            var result = await CreateService().ResetPasswordAsync(Reset());

            Assert.True(result.success);
        }

        // ---------- ChangePasswordAsync ----------

        private static UserChangePasswordDto Cambio() => new()
        {
            CurrentPassword = "actual1",
            NewPassword = "nueva123",
            ConfirmNewPassword = "nueva123"
        };

        [Fact]
        public async Task ChangePasswordAsync_ConUsuarioInexistente_DevuelveError()
        {
            _userManager.Setup(m => m.FindByIdAsync("nadie")).ReturnsAsync((User?)null);

            var (success, error) = await CreateService().ChangePasswordAsync("nadie", Cambio());

            Assert.False(success);
            Assert.Equal("Usuario no encontrado", error);
        }

        [Fact]
        public async Task ChangePasswordAsync_ConContrasenaActualIncorrecta_DevuelveMensajeClaro()
        {
            var user = UsuarioConEmail(emailConfirmado: true);
            _userManager.Setup(m => m.ChangePasswordAsync(user, "actual1", "nueva123"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordMismatch", Description = "Incorrect password." }));

            var (success, error) = await CreateService().ChangePasswordAsync("user-9", Cambio());

            Assert.False(success);
            Assert.Equal("Contraseña actual incorrecta", error);
            VerificarAvisoEnviado(Times.Never());
        }

        [Fact]
        public async Task ChangePasswordAsync_ConNuevaQueNoCumpleLasReglas_DevuelveEsosErrores()
        {
            var user = UsuarioConEmail(emailConfirmado: true);
            _userManager.Setup(m => m.ChangePasswordAsync(user, "actual1", "nueva123"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordRequiresDigit", Description = "Falta un número" }));

            var (success, error) = await CreateService().ChangePasswordAsync("user-9", Cambio());

            Assert.False(success);
            Assert.Contains("Falta un número", error);
        }

        [Fact]
        public async Task ChangePasswordAsync_Correcto_UsaUserManagerYMandaElAviso()
        {
            var user = UsuarioConEmail(emailConfirmado: true);
            _userManager.Setup(m => m.ChangePasswordAsync(user, "actual1", "nueva123")).ReturnsAsync(IdentityResult.Success);

            var (success, _) = await CreateService().ChangePasswordAsync("user-9", Cambio());

            Assert.True(success);
            _userManager.Verify(m => m.ChangePasswordAsync(user, "actual1", "nueva123"), Times.Once);
            _emailService.Verify(e => e.SendPasswordChangedNoticeAsync("gamer@test.com"), Times.Once);
        }
    }
}
