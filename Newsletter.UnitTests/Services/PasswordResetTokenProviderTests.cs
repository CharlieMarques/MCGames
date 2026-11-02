using Microsoft.AspNetCore.Identity;
using Newsletter.Models;
using Newsletter.Services;

namespace Newsletter.UnitTests.Services
{
    public class PasswordResetTokenProviderTests
    {
        [Fact]
        public void Opciones_DuranDosHorasYTienenNombrePropio()
        {
            var options = new PasswordResetTokenProviderOptions();

            Assert.Equal(TimeSpan.FromHours(2), options.TokenLifespan);
            Assert.NotEqual(new DataProtectionTokenProviderOptions().Name, options.Name);
        }

        [Fact]
        public void ElTokenDeConfirmacionDeEmailSigueDurandoUnDia()
        {
            Assert.Equal(TimeSpan.FromDays(1), new DataProtectionTokenProviderOptions().TokenLifespan);
        }

        [Fact]
        public void ElNombreDelProveedorEsEstable()
        {
            Assert.Equal("PasswordReset", PasswordResetTokenProvider<User>.ProviderName);
        }
    }
}
