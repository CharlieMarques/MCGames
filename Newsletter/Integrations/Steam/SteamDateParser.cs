using System.Text.RegularExpressions;

namespace Newsletter.Integrations.Steam
{
    /// <summary>
    /// Lee las fechas de lanzamiento que Steam manda en español ("12 oct 2026", "1 sept. 2026").
    /// No usa la cultura del servidor: depende solo de las tres primeras letras del mes.
    /// </summary>
    public static class SteamDateParser
    {
        private static readonly Regex DatePattern = new(
            @"^\s*(\d{1,2})\s+(?:de\s+)?([^\W\d_]+)\.?,?\s+(?:de\s+)?(\d{4})\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Dictionary<string, int> Months = new()
        {
            ["ene"] = 1, ["feb"] = 2, ["mar"] = 3, ["abr"] = 4, ["may"] = 5, ["jun"] = 6,
            ["jul"] = 7, ["ago"] = 8, ["sep"] = 9, ["set"] = 9, ["oct"] = 10, ["nov"] = 11, ["dic"] = 12
        };

        public static bool TryParse(string? text, out DateTime date)
        {
            date = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var match = DatePattern.Match(text);
            if (!match.Success)
            {
                return false;
            }

            var monthText = match.Groups[2].Value.ToLowerInvariant();
            if (monthText.Length < 3 || !Months.TryGetValue(monthText[..3], out var month))
            {
                return false;
            }

            if (!int.TryParse(match.Groups[1].Value, out var day)
                || !int.TryParse(match.Groups[3].Value, out var year))
            {
                return false;
            }

            try
            {
                date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }
}
