using Newsletter.Integrations.Steam;

namespace Newsletter.UnitTests.Integrations.Steam
{
    public class SteamDateParserTests
    {
        [Theory]
        [InlineData("12 oct 2026", 2026, 10, 12)]
        [InlineData("1 sept. 2026", 2026, 9, 1)]
        [InlineData("3 de dic de 2025", 2025, 12, 3)]
        [InlineData("05 ENE, 2024", 2024, 1, 5)]
        [InlineData("20 septiembre 2026", 2026, 9, 20)]
        [InlineData("7 set 2026", 2026, 9, 7)]
        public void TryParse_ConFechasValidas_DevuelveLaFechaEnUtc(string text, int year, int month, int day)
        {
            var ok = SteamDateParser.TryParse(text, out var date);

            Assert.True(ok);
            Assert.Equal(new DateTime(year, month, day), date.Date);
            Assert.Equal(DateTimeKind.Utc, date.Kind);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Próximamente")]
        [InlineData("Por anunciar")]
        [InlineData("oct 2026")]
        [InlineData("2026")]
        [InlineData("31 feb 2026")]
        [InlineData("12 xyz 2026")]
        public void TryParse_ConTextosInvalidos_DevuelveFalse(string? text)
        {
            Assert.False(SteamDateParser.TryParse(text, out _));
        }
    }
}
