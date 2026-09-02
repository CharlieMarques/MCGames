using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newsletter.Data;
using Newsletter.DTOs.SteamDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Newsletter.Services
{
    public class SteamCronService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        public SteamCronService(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory)
        {
            _serviceProvider = serviceProvider;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"\n▶️ [STEAM CRON] Iniciando escaneo masivo de base de datos a las {DateTime.Now:HH:mm:ss}...");

                    // Iniciamos el cronómetro
                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    // Llamamos al motor
                    await ProcesarCupoDiarioAsync();

                    // Frenamos el cronómetro
                    watch.Stop();

                    System.Diagnostics.Debug.WriteLine($"✅ [STEAM CRON] ¡Ciclo terminado exitosamente!");
                    System.Diagnostics.Debug.WriteLine($"⏱️ Tiempo total de ejecución: {watch.Elapsed.TotalMinutes:0.00} minutos.");
                    System.Diagnostics.Debug.WriteLine($"💤 Entrando en suspensión por 24 horas...\n");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ [STEAM CRON] Error general catastrófico: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
       
        private async Task ProcesarCupoDiarioAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NewsletterDbContext>();

            // 1. Buscamos TODOS los juegos que ACTUALMENTE están en oferta (para ver si se les terminó el descuento)
             var verificarOfertasExistentesIds = await dbContext.Games
                 .Where(g => g.SteamAppId != null
                          && g.State == true
                          && g.OnOffer == true)
                 .OrderBy(g => g.LastPriceCheck)
                 .Select(g => g.Id)
                 .ToListAsync();

             // 2. Buscamos los juegos normales, EXCLUYENDO los gratuitos (Price == 0) y los que aún no salieron
             var buscarNuevasOfertasIds = await dbContext.Games
                 .Where(g => g.SteamAppId != null
                          && g.State == true
                          && g.OnOffer == false
                          && g.Price >= 0)
                          //&& g.ReleaseDate != null)
                 .OrderBy(g => g.LastPriceCheck)
                 .Select(g => g.Id)
                 .ToListAsync();

            // Unimos ambas listas. Priorizamos chequear primero los que ya estaban en oferta.
             var juegosAActualizarIds = verificarOfertasExistentesIds.Concat(buscarNuevasOfertasIds).ToList();

            if (juegosAActualizarIds.Count == 0) return;

            // Cortamos la lista de 100.000 IDs en paquetes de 100
            var lotesDeJuegos = juegosAActualizarIds.Chunk(100).ToList();

            // Usamos 2 hilos paralelos. Cada hilo procesa 100 juegos por vuelta.
            int maximoHilosSimultaneos = 2;
            using var semaforo = new SemaphoreSlim(maximoHilosSimultaneos);

            var tareas = lotesDeJuegos.Select(async loteIds =>
            {
                await semaforo.WaitAsync();
                using var httpClient = _httpClientFactory.CreateClient();

                // Creamos el contexto de BD aislado para este hilo específico
                using var scopeHijo = _serviceProvider.CreateScope();
                var dbContextHijo = scopeHijo.ServiceProvider.GetRequiredService<NewsletterDbContext>();

                try
                {
                    // Traemos las entidades completas de estos 100 juegos desde la BD
                    var juegosDb = await dbContextHijo.Games
                        .Where(g => loteIds.Contains(g.Id))
                        .ToListAsync();

                    // Armamos la URL para Steam uniendo los 100 AppIDs con comas
                    string appIdsAgrupados = string.Join(",", juegosDb.Select(g => g.SteamAppId));
                    string detailsUrl = $"https://store.steampowered.com/api/appdetails?appids={appIdsAgrupados}&filters=price_overview&cc=ar&l=spanish";

                    var response = await httpClient.GetAsync(detailsUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResult = await response.Content.ReadAsStringAsync();
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var steamResponse = JsonSerializer.Deserialize<Dictionary<string, SteamGameWrapper>>(jsonResult, options);

                        if (steamResponse != null)
                        {
                            // Iteramos sobre los 100 juegos de nuestro contexto
                            foreach (var game in juegosDb)
                            {
                                game.LastPriceCheck = DateTime.UtcNow;
                                string key = game.SteamAppId.ToString();

                                if (steamResponse.ContainsKey(key) && steamResponse[key] != null && steamResponse[key].Success)
                                {
                                    var dataElement = steamResponse[key].Data;

                                    // Verificamos que Steam haya mandado un Objeto "{}" y no un Array vacío "[]"
                                    if (dataElement.ValueKind == JsonValueKind.Object)
                                    {
                                        // Como confirmamos que es un objeto, lo deserializamos de forma 100% segura
                                        var data = JsonSerializer.Deserialize<Newsletter.DTOs.SteamDto.SteamGameData>(dataElement.GetRawText(), options);

                                        if (data != null)
                                        {
                                            if (data.IsFree)
                                            {
                                                game.Price = 0; game.FinalPrice = 0; game.DiscountPercentage = 0; game.OnOffer = false;
                                            }
                                            else if (data.PriceOverview == null)
                                            {
                                                game.OnOffer = false;
                                                game.DiscountPercentage = 0;
                                                if (game.Price > 0) game.FinalPrice = game.Price;
                                            }
                                            else
                                            {
                                                game.Price = data.PriceOverview.Initial / 100m;
                                                game.FinalPrice = data.PriceOverview.Final / 100m;
                                                game.DiscountPercentage = data.PriceOverview.DiscountPercent;
                                                game.OnOffer = data.PriceOverview.DiscountPercent > 0;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Si Steam mandó "[]" o cualquier basura, asumimos que no hay oferta
                                        game.OnOffer = false;
                                    }
                                }
                                else
                                {
                                    game.OnOffer = false;
                                }
           

                                dbContextHijo.Entry(game).State = EntityState.Modified;
                            }

                            // Guardamos los 100 cambios de un solo golpe
                            await dbContextHijo.SaveChangesAsync();
                        }
                    }
                    else if ((int)response.StatusCode == 429)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ [STEAM] Castigo 429 de Steam. Pausa de 60 segundos...");
                        await Task.Delay(60000); // Esperamos 1 minuto 
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ [STEAM] Error en lote: {ex.Message}");
                }
                finally
                {
                    // PAUSA 
                    await Task.Delay(2000);
                    semaforo.Release();
                }
            });

            await Task.WhenAll(tareas);
        }

    }
}