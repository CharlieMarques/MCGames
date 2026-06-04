using System.Text.Json.Serialization;

namespace Newsletter.DTOs.SteamDto
{
    public class SteamGameWrapper
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("data")] public SteamGameData Data { get; set; }
    }

    public class SteamGameData
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("short_description")] public string ShortDescription { get; set; }
        [JsonPropertyName("header_image")] public string HeaderImage { get; set; }
        [JsonPropertyName("release_date")] public SteamReleaseDate ReleaseDate { get; set; }
        [JsonPropertyName("price_overview")] public SteamPriceOverview PriceOverview { get; set; }
        [JsonPropertyName("genres")] public List<SteamGenreDto> Genres { get; set; }
        [JsonPropertyName("platforms")] public SteamPlatformsDto Platforms { get; set; }
        [JsonPropertyName("categories")] public List<SteamCategoryDto> Categories { get; set; }
        [JsonPropertyName("is_free")] public bool IsFree { get; set; }
    }

    public class SteamReleaseDate
    {
        [JsonPropertyName("date")] public string Date { get; set; }
    }

    public class SteamPriceOverview
    {
        [JsonPropertyName("initial")] public int Initial { get; set; }
        [JsonPropertyName("final")] public int Final { get; set; }
        [JsonPropertyName("discount_percent")] public int DiscountPercent { get; set; }
    }

    public class SteamGenreDto
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
    }

    public class SteamPlatformsDto
    {
        [JsonPropertyName("windows")] public bool Windows { get; set; }
        [JsonPropertyName("mac")] public bool Mac { get; set; }
        [JsonPropertyName("linux")] public bool Linux { get; set; }
    }
    public class StoreAppListResponse
    {
        [JsonPropertyName("response")]
        public StoreAppListPayload Response { get; set; }
    }

    public class SteamCategoryDto
    {
        [JsonPropertyName("id")] public int Id { get; set; } // Steam usa enteros para el ID de categoría
        [JsonPropertyName("description")] public string Description { get; set; }
    }

    public class StoreAppListPayload
    {
        [JsonPropertyName("apps")]
        public List<StoreApp> Apps { get; set; }

        // ¡Los dos campos mágicos para la paginación!
        [JsonPropertyName("have_more_results")]
        public bool HaveMoreResults { get; set; }

        [JsonPropertyName("last_appid")]
        public int LastAppId { get; set; }
    }

    public class StoreApp
    {
        [JsonPropertyName("appid")]
        public int AppId { get; set; }
    }
}
