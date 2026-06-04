using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newsletter.Data;
using Newsletter.Models;
using Newsletter.Services;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Newsletter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EpicTestController : ControllerBase
    {
        private readonly NewsletterDbContext _dbContext;

        public EpicTestController(NewsletterDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("cargar-json-manual")]
        public async Task<IActionResult> CargarJsonManual([FromBody] JsonElement jsonCrudo)
        {
            try
            {
                // Navegamos el JSON que pegaste manualmente
                if (!jsonCrudo.TryGetProperty("data", out var dataProp) ||
                    !dataProp.TryGetProperty("Catalog", out var catalogProp) ||
                    !catalogProp.TryGetProperty("searchStore", out var searchStoreProp) ||
                    !searchStoreProp.TryGetProperty("elements", out var elements))
                {
                    return BadRequest("La estructura del JSON pegado no es correcta. Asegurate de copiar el 'Response' entero de la query de Epic.");
                }

                int juegosProcesados = 0;

                foreach (var element in elements.EnumerateArray())
                {
                    if (!element.TryGetProperty("title", out var titleProp) || string.IsNullOrEmpty(titleProp.GetString())) continue;
                    string title = titleProp.GetString()!;

                    string id = element.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                    string description = element.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "";

                    // Precios (vienen multiplicados por 100)
                    decimal originalPrice = 0;
                    decimal discountPrice = 0;

                    if (element.TryGetProperty("price", out var priceProp) &&
                        priceProp.TryGetProperty("totalPrice", out var totalPriceProp))
                    {
                        originalPrice = totalPriceProp.GetProperty("originalPrice").GetDecimal() / 100m;
                        discountPrice = totalPriceProp.GetProperty("discountPrice").GetDecimal() / 100m;
                    }

                    int discountPercent = originalPrice > 0
                        ? (int)Math.Round(((originalPrice - discountPrice) / originalPrice) * 100)
                        : 0;
                    bool onOffer = discountPercent > 0;

                    // Imagen
                    string imageUrl = "";
                    if (element.TryGetProperty("keyImages", out var imagesProp) && imagesProp.ValueKind == JsonValueKind.Array)
                    {
                        var imagesList = imagesProp.EnumerateArray().ToList();
                        var thumbnail = imagesList.FirstOrDefault(img => img.TryGetProperty("type", out var t) && t.GetString() == "Thumbnail");
                        imageUrl = thumbnail.ValueKind != JsonValueKind.Undefined ? thumbnail.GetProperty("url").GetString() ?? "" : "";
                    }

                    // 🔍 Buscamos por Nombre en tu tabla Games (con tu GUID)
                    var juegoBase = await _dbContext.Games
                        .Include(g => g.EpicData)
                        .FirstOrDefaultAsync(g => g.Name.ToLower() == title.ToLower());

                    if (juegoBase != null)
                    {
                        // CASO A: Ya existía en tu BD. Vinculamos o actualizamos GamesInEpic
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
                        _dbContext.Entry(juegoBase).State = EntityState.Modified;
                    }
                    else
                    {
                        // CASO B: Exclusivo de Epic. Creamos registro base + extensión
                        var nuevoJuegoBase = new Game
                        {
                            Id = Guid.NewGuid(),
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

                    juegosProcesados++;
                }

                await _dbContext.SaveChangesAsync();
                return Ok(new { Mensaje = $"¡Se procesaron {juegosProcesados} juegos con éxito directamente en las tablas!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al procesar el lote manual: {ex.Message}");
            }
        }
    }
}
