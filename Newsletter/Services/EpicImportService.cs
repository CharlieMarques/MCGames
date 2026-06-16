using Microsoft.EntityFrameworkCore;
using Newsletter.Data;
using Newsletter.Models;
using PuppeteerSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Newsletter.Services
{
    // Clase auxiliar para reportar todo transparente en Swagger
   /* public class BotDiagnosticReport
    {
        public int LotesJsonCapturados { get; set; }
        public int TotalJuegosDetectados { get; set; }
        public int TotalJuegosGuardados { get; set; }
        public List<string> ReporteErrores { get; set; } = new List<string>();
        public List<int> PaginasFallidas { get; set; } = new List<int>();
    }

    public class EpicImportService
    {
        private readonly NewsletterDbContext _dbContext;
        private Dictionary<string, int> _generosCacheados = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, int> _categoriasCacheadas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _listaGeneros = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Action", "Adventure", "RPG", "Strategy", "Shooter", "Puzzle", "Simulation",
            "Indie", "Casual", "Sports", "Racing", "Arcade", "Horror", "Survival"
        };

        public EpicImportService(NewsletterDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<BotDiagnosticReport> AutomatizarCargaEpicAsync(int paginaInicio = 1, int paginasACargar = 3)
        {
            var report = new BotDiagnosticReport();
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            _generosCacheados.Clear();
            _categoriasCacheadas.Clear();

            int maxIntentosPorPagina = 10;
            for (int i = 0; i < paginasACargar; i++)
            {
                int paginaActual = paginaInicio + i;
                int currentStart = (paginaActual - 1) * 40;
                string urlDinamica = $"https://store.epicgames.com/es-MX/browse?sortBy=releaseDate&sortDir=DESC&count=40&start={currentStart}";

                bool paginaCapturada = false;
                var jsonsCapturadosLocales = new ConcurrentBag<string>();

                System.Diagnostics.Debug.WriteLine($"\n--- INICIANDO PÁGINA {paginaActual} ---");
                for (int intento = 1; intento <= maxIntentosPorPagina; intento++)
                {
                    if (paginaCapturada) break;

                    System.Diagnostics.Debug.WriteLine($"🚀 [BOT] Abriendo Chrome (Intento {intento}/{maxIntentosPorPagina})...");

                    try
                    {
                        using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                        {
                            Headless = true,
                            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                        });

                        using var page = await browser.NewPageAsync();
                        await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                        page.Response += async (sender, e) =>
                        {
                            if (e.Response.Url.Contains("graphql"))
                            {
                                try
                                {
                                    var json = await e.Response.TextAsync();
                                    if (!string.IsNullOrEmpty(json) && json.Contains("\"Catalog\"") && json.Contains("\"searchStore\"") && !json.Contains("\"errors\":"))
                                    {
                                        jsonsCapturadosLocales.Add(json);
                                    }
                                }
                                catch { }
                            }
                        };
                        await page.GoToAsync(urlDinamica, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } });

                        await page.EvaluateExpressionAsync("window.scrollTo(0, document.body.scrollHeight / 2);");
                        await Task.Delay(2000);
                        await page.EvaluateExpressionAsync("window.scrollTo(0, document.body.scrollHeight);");
                        await Task.Delay(4000);
                        await browser.CloseAsync();
                        if (jsonsCapturadosLocales.Count > 0)
                        {
                            paginaCapturada = true;
                            System.Diagnostics.Debug.WriteLine($"✅ [BOT] JSON de página {paginaActual} capturado.");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ [BOT] Epic no entregó datos. Reintentando...");
                            await Task.Delay(2000);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ [BOT] Error abriendo Chrome: {ex.Message}");
                    }
                }

                // 💾 IMPACTO INMEDIATO EN BASE DE DATOS (Tu idea aplicada)
                if (paginaCapturada)
                {
                    report.LotesJsonCapturados += jsonsCapturadosLocales.Count;

                    System.Diagnostics.Debug.WriteLine($"💾 [BOT] Guardando página {paginaActual} en SQL Server...");
                    foreach (var jsonCrudo in jsonsCapturadosLocales)
                    {
                        await ProcesarLoteJsonAsync(jsonCrudo, report);
                    }
                    System.Diagnostics.Debug.WriteLine($"✅ [BOT] Página {paginaActual} guardada. Pasando a la siguiente...\n");
                }
                else
                {
                    report.ReporteErrores.Add($"La página {paginaActual} volvió a fallar en el rescate tras {maxIntentosPorPagina} refrescos.");
                    report.PaginasFallidas.Add(paginaActual);
                    System.Diagnostics.Debug.WriteLine($"❌ [BOT] Falló el rescate de la página {paginaActual}. Registrada en el historial de fallos.\n");
                }
            }

            return report;
        }

        public async Task<BotDiagnosticReport> RecuperarPaginasCaidasAsync(List<int> paginasFallidas)
        {
            var report = new BotDiagnosticReport();
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            _generosCacheados.Clear();
            _categoriasCacheadas.Clear();

            int maxIntentosPorPagina = 10;

            // 🚀 BUCLE MAESTRO: Recorre SOLO los números de la lista que le pasaste
            foreach (int paginaActual in paginasFallidas)
            {
                int currentStart = (paginaActual - 1) * 40;
                string urlDinamica = $"https://store.epicgames.com/es-MX/browse?sortBy=releaseDate&sortDir=DESC&count=40&start={currentStart}";

                bool paginaCapturada = false;
                var jsonsCapturadosLocales = new ConcurrentBag<string>();

                System.Diagnostics.Debug.WriteLine($"\n--- 🎯 RECUPERANDO PÁGINA {paginaActual} ---");

                try
                {
                    // 1. ABRIMOS CHROME UNA SOLA VEZ PARA ESTA PÁGINA
                    using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                    {
                        Headless = true, // Podés ponerlo en true si ya no querés ver las ventanas
                        Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                    });

                    using var page = await browser.NewPageAsync();
                    await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                    // 2. ENCHUFAMOS EL INTERCEPTOR
                    page.Response += async (sender, e) =>
                    {
                        if (e.Response.Url.Contains("graphql"))
                        {
                            try
                            {
                                var json = await e.Response.TextAsync();
                                if (!string.IsNullOrEmpty(json) && json.Contains("\"Catalog\"") && json.Contains("\"searchStore\"") && !json.Contains("\"errors\":"))
                                {
                                    jsonsCapturadosLocales.Add(json);
                                }
                            }
                            catch { }
                        }
                    };

                    // 🔄 3. BUCLE DE REINTENTOS CON REFRESH (F5)
                    for (int intento = 1; intento <= maxIntentosPorPagina; intento++)
                    {
                        if (paginaCapturada) break;

                        try
                        {
                            if (intento == 1)
                            {
                                System.Diagnostics.Debug.WriteLine($"🚀 [BOT] Entrando por primera vez a la pág {paginaActual}...");
                                await page.GoToAsync(urlDinamica, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } });
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ [BOT] Sin datos. Apretando F5 (Intento {intento}/{maxIntentosPorPagina})...");
                                await page.ReloadAsync(timeout: 60000, waitUntil: new[] { WaitUntilNavigation.Networkidle2 });
                            }

                            // Scrolleamos para forzar la carga
                            await page.EvaluateExpressionAsync("window.scrollTo(0, document.body.scrollHeight / 2);");
                            await Task.Delay(2000);
                            await page.EvaluateExpressionAsync("window.scrollTo(0, document.body.scrollHeight);");
                            await Task.Delay(4000);

                            // 4. EVALUAMOS SI ATRAPAMOS EL JSON EN ESTA VUELTA
                            if (jsonsCapturadosLocales.Count > 0)
                            {
                                paginaCapturada = true;
                                System.Diagnostics.Debug.WriteLine($"✅ [BOT] JSON de página {paginaActual} rescatado al vuelo.");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ [BOT] Error en navegación de rescate: {ex.Message}");
                        }
                    }

                    // 5. CERRAMOS CHROME AL TERMINAR LA PÁGINA (Exitoso o no)
                    await browser.CloseAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ [BOT] Error general abriendo Chrome: {ex.Message}");
                }

                // 💾 6. GUARDADO EN BASE DE DATOS
                if (paginaCapturada)
                {
                    report.LotesJsonCapturados += jsonsCapturadosLocales.Count;

                    System.Diagnostics.Debug.WriteLine($"💾 [BOT] Guardando página {paginaActual} en BD...");
                    foreach (var jsonCrudo in jsonsCapturadosLocales)
                    {
                        // Usamos el mismo procesador de base de datos que ya tenés
                        await ProcesarLoteJsonAsync(jsonCrudo, report);
                    }
                    System.Diagnostics.Debug.WriteLine($"✅ [BOT] Página {paginaActual} asegurada.\n");
                }
                else
                {
                    report.ReporteErrores.Add($"La página {paginaActual} volvió a fallar en el rescate tras {maxIntentosPorPagina} refrescos.");
                    System.Diagnostics.Debug.WriteLine($"❌ [BOT] Falló el rescate de la página {paginaActual}.\n");
                }
            }

            return report;
        }
        private async Task ProcesarLoteJsonAsync(string jsonCrudo, BotDiagnosticReport report)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonCrudo);
                var elements = doc.RootElement.GetProperty("data").GetProperty("Catalog").GetProperty("searchStore").GetProperty("elements");

                foreach (var element in elements.EnumerateArray())
                {
                    report.TotalJuegosDetectados++;
                    string title = "Desconocido";

                    try
                    {
                        if (!element.TryGetProperty("title", out var titleProp) || string.IsNullOrEmpty(titleProp.GetString())) continue;
                        title = titleProp.GetString()!;

                        // Lógica Robusta de Extracción del Slug de Epic
                        string epicSlug = "";

                        if (element.TryGetProperty("pageSlug", out var pageSlugProp) && pageSlugProp.ValueKind == JsonValueKind.String)
                            epicSlug = pageSlugProp.GetString() ?? "";
                        else if (element.TryGetProperty("productSlug", out var prodSlugProp) && prodSlugProp.ValueKind == JsonValueKind.String)
                            epicSlug = prodSlugProp.GetString() ?? "";
                        else if (element.TryGetProperty("urlSlug", out var urlSlugProp) && urlSlugProp.ValueKind == JsonValueKind.String)
                            epicSlug = urlSlugProp.GetString() ?? "";

                        if (string.IsNullOrEmpty(epicSlug) && element.TryGetProperty("catalogNs", out var catalogNs) && catalogNs.TryGetProperty("mappings", out var mappings) && mappings.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var mapping in mappings.EnumerateArray())
                            {
                                if (mapping.TryGetProperty("pageSlug", out var mappingPageSlug))
                                {
                                    epicSlug = mappingPageSlug.GetString() ?? "";
                                    break;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(epicSlug))
                        {
                            epicSlug = element.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                        }

                        if (epicSlug.EndsWith("/home"))
                        {
                            epicSlug = epicSlug.Replace("/home", "");
                        }

                        string description = element.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "";

                        decimal originalPrice = 0;
                        decimal discountPrice = 0;
                        if (element.TryGetProperty("price", out var priceProp) && priceProp.TryGetProperty("totalPrice", out var totalPriceProp))
                        {
                            originalPrice = totalPriceProp.GetProperty("originalPrice").GetDecimal() / 100m;
                            discountPrice = totalPriceProp.GetProperty("discountPrice").GetDecimal() / 100m;
                        }

                        int discountPercent = originalPrice > 0 ? (int)Math.Round(((originalPrice - discountPrice) / originalPrice) * 100) : 0;
                        bool onOffer = discountPercent > 0;

                        string imageUrl = "";
                        if (element.TryGetProperty("keyImages", out var imagesProp) && imagesProp.ValueKind == JsonValueKind.Array)
                        {
                            var imagesList = imagesProp.EnumerateArray().ToList();
                            var thumbnail = imagesList.FirstOrDefault(img => img.TryGetProperty("type", out var t) && t.GetString() == "Thumbnail");
                            imageUrl = thumbnail.ValueKind != JsonValueKind.Undefined ? thumbnail.GetProperty("url").GetString() ?? "" : "";
                        }

                        var tagsJuego = new List<string>();
                        if (element.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var tagElement in tagsProp.EnumerateArray())
                            {
                                if (tagElement.TryGetProperty("name", out var tagNameProp))
                                {
                                    string nameTag = tagNameProp.GetString() ?? "";
                                    if (!string.IsNullOrEmpty(nameTag)) tagsJuego.Add(nameTag);
                                }
                            }
                        }

                        var juegoBase = await _dbContext.Games
                            .Include(g => g.EpicData)
                            .FirstOrDefaultAsync(g => g.Name.ToLower() == title.ToLower());

                        Guid idJuegoReal;

                        // 💡 CASO 1: EL JUEGO YA EXISTE EN LA BASE DE DATOS
                        if (juegoBase != null)
                        {
                            idJuegoReal = juegoBase.Id; // <-- SE ASIGNA AQUÍ

                            if (juegoBase.EpicData != null)
                            {
                                if (juegoBase.EpicData.EpicStoreId != epicSlug)
                                {
                                    _dbContext.Remove(juegoBase.EpicData);
                                    await _dbContext.SaveChangesAsync();

                                    juegoBase.EpicData = new GameInEpic
                                    {
                                        GameId = juegoBase.Id,
                                        EpicStoreId = epicSlug,
                                        EpicPrice = originalPrice,
                                        EpicFinalPrice = discountPrice,
                                        EpicDiscountPercentage = discountPercent,
                                        EpicOnOffer = onOffer,
                                        LastPriceCheck = DateTime.UtcNow
                                    };
                                }
                                else
                                {
                                    juegoBase.EpicData.EpicPrice = originalPrice;
                                    juegoBase.EpicData.EpicFinalPrice = discountPrice;
                                    juegoBase.EpicData.EpicDiscountPercentage = discountPercent;
                                    juegoBase.EpicData.EpicOnOffer = onOffer;
                                    juegoBase.EpicData.LastPriceCheck = DateTime.UtcNow;
                                }
                            }
                            else
                            {
                                juegoBase.EpicData = new GameInEpic
                                {
                                    GameId = juegoBase.Id,
                                    EpicStoreId = epicSlug,
                                    EpicPrice = originalPrice,
                                    EpicFinalPrice = discountPrice,
                                    EpicDiscountPercentage = discountPercent,
                                    EpicOnOffer = onOffer,
                                    LastPriceCheck = DateTime.UtcNow
                                };
                            }
                            _dbContext.Entry(juegoBase).State = EntityState.Modified;
                        }
                        // 💡 CASO 2: EL JUEGO ES NUEVO (Faltaba este bloque)
                        else
                        {
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
                                    EpicStoreId = epicSlug,
                                    EpicPrice = originalPrice,
                                    EpicFinalPrice = discountPrice,
                                    EpicDiscountPercentage = discountPercent,
                                    EpicOnOffer = onOffer,
                                    LastPriceCheck = DateTime.UtcNow
                                }
                            };
                            idJuegoReal = nuevoJuegoBase.Id; // <-- SE ASIGNA AQUÍ
                            await _dbContext.Games.AddAsync(nuevoJuegoBase);
                        }

                        // Ahora sí, la variable idJuegoReal tiene garantizado un valor
                        await ProcesarEstructuraRelacionalAsync(idJuegoReal, tagsJuego);
                        report.TotalJuegosGuardados++;
                    }
                    catch (Exception ex)
                    {
                        report.ReporteErrores.Add($"Fallo el juego '{title}': {ex.Message} -> {ex.InnerException?.Message}");
                    }
                }

                // Impacto final del lote
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                report.ReporteErrores.Add($"Error crítico procesando estructura de lote: {ex.Message}");
            }
        }

        private async Task ProcesarEstructuraRelacionalAsync(Guid gameId, List<string> tags)
        {
            if (tags == null || !tags.Any()) return;

            foreach (var tag in tags)
            {
                bool esGenero = _listaGeneros.Contains(tag);

                if (esGenero)
                {
                    int genreId;
                    if (_generosCacheados.ContainsKey(tag))
                    {
                        genreId = _generosCacheados[tag];
                    }
                    else
                    {
                        var generoDb = await _dbContext.Set<Genre>().FirstOrDefaultAsync(g => g.Description.ToLower() == tag.ToLower());
                        if (generoDb != null)
                        {
                            genreId = generoDb.Id;
                            _generosCacheados[tag] = genreId;
                        }
                        else
                        {
                            var nuevoGenero = new Genre { Description = tag, State = true };
                            await _dbContext.Set<Genre>().AddAsync(nuevoGenero);
                            await _dbContext.SaveChangesAsync();
                            genreId = nuevoGenero.Id;
                            _generosCacheados[tag] = genreId;
                        }
                    }

                    var existeRelacion = await _dbContext.Set<GameGenre>().AnyAsync(gg => gg.GameId == gameId && gg.GenreId == genreId);
                    if (!existeRelacion)
                    {
                        await _dbContext.Set<GameGenre>().AddAsync(new GameGenre { GameId = gameId, GenreId = genreId });
                    }
                }
                else
                {
                    int categoryId;
                    if (_categoriasCacheadas.ContainsKey(tag))
                    {
                        categoryId = _categoriasCacheadas[tag];
                    }
                    else
                    {
                        var categoriaDb = await _dbContext.Set<Category>().FirstOrDefaultAsync(c => c.Description.ToLower() == tag.ToLower());
                        if (categoriaDb != null)
                        {
                            categoryId = categoriaDb.Id;
                            _categoriasCacheadas[tag] = categoryId;
                        }
                        else
                        {
                            var nuevaCategoria = new Category { Description = tag };
                            await _dbContext.Set<Category>().AddAsync(nuevaCategoria);
                            await _dbContext.SaveChangesAsync();
                            categoryId = nuevaCategoria.Id;
                            _categoriasCacheadas[tag] = categoryId;
                        }
                    }

                    var existeRelacionCat = await _dbContext.Set<GameCategory>().AnyAsync(gc => gc.GameId == gameId && gc.CategoryId == categoryId);
                    if (!existeRelacionCat)
                    {
                        await _dbContext.Set<GameCategory>().AddAsync(new GameCategory { GameId = gameId, CategoryId = categoryId });
                    }
                }
            }
        }
    }*/
}