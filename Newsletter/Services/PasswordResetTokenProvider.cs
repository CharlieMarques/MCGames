using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Newsletter.Services
{
    /// <summary>Tokens de recuperación de contraseña con vida corta (2 horas).</summary>
    public class PasswordResetTokenProvider<TUser> : DataProtectorTokenProvider<TUser> where TUser : class
    {
        public const string ProviderName = "PasswordReset";

        public PasswordResetTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<PasswordResetTokenProviderOptions> options,
            ILogger<DataProtectorTokenProvider<TUser>> logger)
            : base(dataProtectionProvider, options, logger)
        {
        }
    }

    public class PasswordResetTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        public PasswordResetTokenProviderOptions()
        {
            Name = "PasswordResetDataProtectorTokenProvider";
            TokenLifespan = TimeSpan.FromHours(2);
        }
    }
}
