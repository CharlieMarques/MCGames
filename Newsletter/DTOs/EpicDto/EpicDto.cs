using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Newsletter.DTOs.EpicDto
{
    // 1️⃣ El contenedor raíz que envuelve la propiedad "data" de GraphQL
    public class EpicResponseWrapper
    {
        [JsonPropertyName("data")]
        public EpicData Data { get; set; } = null!;
    }

    // 2️⃣ La capa "data" de GraphQL
    public class EpicData
    {
        [JsonPropertyName("Catalog")]
        public EpicCatalog Catalog { get; set; } = null!;
    }

    // 3️⃣ El catálogo que contiene el buscador de la tienda
    public class EpicCatalog
    {
        [JsonPropertyName("searchStore")]
        public EpicSearchStore SearchStore { get; set; } = null!;
    }

    // 4️⃣ El contenedor que trae la lista de elementos (juegos)
    public class EpicSearchStore
    {
        [JsonPropertyName("elements")]
        public List<EpicGameElement> Elements { get; set; } = new();
    }

    // 5️⃣ El DTO clave: Mapea las propiedades nativas de cada juego de Epic
    public class EpicGameElement
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("releaseDate")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("keyImages")]
        public List<EpicImageDto> KeyImages { get; set; } = new();

        [JsonPropertyName("price")]
        public EpicPriceContainer Price { get; set; } = null!;
    }

    // 6️⃣ Las imágenes asociadas (buscaremos la que tenga tipo "Thumbnail")
    public class EpicImageDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    // 7️⃣ El contenedor del precio por país
    public class EpicPriceContainer
    {
        [JsonPropertyName("totalPrice")]
        public EpicTotalPrice TotalPrice { get; set; } = null!;
    }

    // 8️⃣ Los valores numéricos puros del precio original, final y descuento
    public class EpicTotalPrice
    {
        [JsonPropertyName("originalPrice")]
        public decimal OriginalPrice { get; set; }

        [JsonPropertyName("discountPrice")]
        public decimal DiscountPrice { get; set; }

        [JsonPropertyName("discount")]
        public decimal Discount { get; set; }
    }
}
