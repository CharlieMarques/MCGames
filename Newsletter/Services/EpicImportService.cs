using Microsoft.EntityFrameworkCore;
using Newsletter.Data;
using Newsletter.DTOs.EpicDto;
using Newsletter.Models;
using System.Text;
using System.Text.Json;

namespace Newsletter.Services
{
    public class EpicImportService
    {
        private readonly NewsletterDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;

        public EpicImportService(NewsletterDbContext dbContext, IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
        }

        public async Task SincronizarCatalogoEpicAsync()
        {
            using var httpClient = _httpClientFactory.CreateClient();

            // Al ser una API comunitaria abierta, no requiere encabezados raros
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Newsletter-Game-Bot");

            // 🚀 URL ABIERTA Y PÚBLICA: Espejo libre de autenticación que parsea la tienda de Epic
            string url = "https://epic-free-games-api.vercel.app/api/freegames?locale=es-MX&country=AR";

            try
            {
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en espejo comunitario: {response.StatusCode}");
                    return;
                }

                var jsonResult = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResult);

                // Esta API nos devuelve directamente una propiedad "games" en la raíz
                if (!doc.RootElement.TryGetProperty("games", out var gamesArray))
                {
                    System.Diagnostics.Debug.WriteLine("La estructura de la API alternativa no contiene la lista 'games'.");
                    return;
                }

                foreach (var element in gamesArray.EnumerateArray())
                {
                    if (!element.TryGetProperty("title", out var titleProp) || string.IsNullOrEmpty(titleProp.GetString())) continue;
                    string title = titleProp.GetString()!;

                    // El ID de Epic y la descripción
                    string id = element.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                    string description = element.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "";

                    // 💡 Precios: Esta API los devuelve normalizados y limpios (Ya divididos por 100)
                    decimal originalPrice = 0;
                    decimal discountPrice = 0;

                    if (element.TryGetProperty("price", out var priceProp))
                    {
                        originalPrice = priceProp.TryGetProperty("originalPrice", out var op) ? op.GetDecimal() : 0;
                        discountPrice = priceProp.TryGetProperty("discountPrice", out var dp) ? dp.GetDecimal() : originalPrice;
                    }

                    // Calculamos el porcentaje de descuento
                    int discountPercent = originalPrice > 0
                        ? (int)Math.Round(((originalPrice - discountPrice) / originalPrice) * 100)
                        : 0;
                    bool onOffer = discountPercent > 0;

                    // Imagen de portada
                    string imageUrl = element.TryGetProperty("image", out var imgProp) ? imgProp.GetString() ?? "" : "";

                    // 🔍 Buscamos si el juego ya existe en tu tabla Games por Nombre
                    var juegoBase = await _dbContext.Games
                        .Include(g => g.EpicData)
                        .FirstOrDefaultAsync(g => g.Name.ToLower() == title.ToLower());

                    if (juegoBase != null)
                    {
                        // CASO A: Ya existía (ej: de Steam). Creamos o actualizamos la extensión en GamesInEpic
                        if (juegoBase.EpicData != null)
                        {
                            juegoBase.EpicData.EpicPrice = originalPrice;
                            juegoBase.EpicData.EpicFinalPrice = discountPrice;
                            juegoBase.EpicData.EpicDiscountPercentage = discountPercent;
                            juegoBase.EpicData.EpicOnOffer = onOffer;
                            juegoBase.EpicData.LastPriceCheck = DateTime.UtcNow;
                        }
                        else
                        {
                            juegoBase.EpicData = new GameInEpic
                            {
                                GameId = juegoBase.Id,
                                EpicStoreId = id,
                                EpicPrice = originalPrice,
                                EpicFinalPrice = discountPrice,
                                EpicDiscountPercentage = discountPercent,
                                EpicOnOffer = onOffer,
                                LastPriceCheck = DateTime.UtcNow
                            };
                        }

                        if (string.IsNullOrEmpty(juegoBase.GameCoverUrl)) juegoBase.GameCoverUrl = imageUrl;
                        _dbContext.Entry(juegoBase).State = EntityState.Modified;
                    }
                    else
                    {
                        // CASO B: El juego es exclusivo de Epic. Creamos el registro base + extensión
                        var nuevoJuegoBase = new Game
                        {
                            Id = Guid.NewGuid(), // Tu GUID impecable
                            Name = title,
                            ShortDescription = description.Length > 600 ? description.Substring(0, 597) + "..." : description,
                            GameCoverUrl = imageUrl,
                            State = true,
                            SteamAppId = null,
                            EpicData = new GameInEpic
                            {
                                EpicStoreId = id,
                                EpicPrice = originalPrice,
                                EpicFinalPrice = discountPrice,
                                EpicDiscountPercentage = discountPercent,
                                EpicOnOffer = onOffer,
                                LastPriceCheck = DateTime.UtcNow
                            }
                        };

                        await _dbContext.Games.AddAsync(nuevoJuegoBase);
                    }
                }

                // Guardamos todo el lote en SQL Server local
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error crítico en importación de Epic alternativa: {ex.Message}");
            }
        }
    }
}


