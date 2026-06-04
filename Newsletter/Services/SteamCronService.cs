using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
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
            // 💡 Espera 1 minuto antes de arrancar por primera vez al encender la API,
            // así le damos tiempo al servidor web de estabilizarse por completo.
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 🔥 EJECUTAMOS LA RUEDA DE STEAM
                    await ProcesarCupoDiarioAsync();
                }
                catch (Exception)
                {
                    // Manejo de errores global del ciclo para que nunca se rompa el servicio
                }

                // ⏰ ESPERA DE 24 HORAS: Se duerme el hilo hasta el día siguiente
                // Podés cambiarlo a TimeSpan.FromMinutes(5) en local si querés probarlo rápido.
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }

        private async Task ProcesarCupoDiarioAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NewsletterDbContext>();
            using var httpClient = _httpClientFactory.CreateClient();

            // 1️⃣ Cupo A: 12.000 juegos comerciales que NO están en oferta (Buscando nuevos descuentos)
            var buscarNuevasOfertas = await dbContext.Games
                .Where(g => g.SteamAppId != null
                         && g.State == true
                         && g.Price > 0
                         && g.OnOffer == false
                         && g.ReleaseDate != null)
                .OrderBy(g => g.LastPriceCheck)
                .Take(12000)
                .ToListAsync();

            // 2️⃣ Cupo B: 2.000 juegos que SÍ están en oferta en tu BD (Verificando si la oferta ya terminó)
            var verificarOfertasExistentes = await dbContext.Games
                .Where(g => g.SteamAppId != null
                         && g.State == true
                         && g.OnOffer == true) // Trae los que están en oferta para reevaluarlos
                .OrderBy(g => g.LastPriceCheck) // Los que hace más tiempo que no miramos van primero
                .Take(2000)
                .ToListAsync();

            // 3️⃣ Unificamos ambas listas en una sola tanda para el bucle foreach (Total: 14.000 juegos)
            var juegosAActualizar = buscarNuevasOfertas.Concat(verificarOfertasExistentes).ToList();

            if (juegosAActualizar.Count == 0) return;

            foreach (var game in juegosAActualizar)
            {
                game.LastPriceCheck = DateTime.UtcNow;
                string detailsUrl = $"https://store.steampowered.com/api/appdetails?appids={game.SteamAppId}&l=spanish&cc=ar";

                try
                {
                    var response = await httpClient.GetAsync(detailsUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResult = await response.Content.ReadAsStringAsync();
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var steamResponse = JsonSerializer.Deserialize<Dictionary<string, SteamGameWrapper>>(jsonResult, options);

                        string key = game.SteamAppId.ToString();

                        if (steamResponse != null && steamResponse.ContainsKey(key) && steamResponse[key] != null && steamResponse[key].Success)
                        {
                            var data = steamResponse[key].Data;
                            if (data != null)
                            {
                                if (data.IsFree)
                                {
                                    game.Price = 0; game.FinalPrice = 0; game.DiscountPercentage = 0; game.OnOffer = false;
                                }
                                else if (data.PriceOverview == null)
                                {
                                    // 💡 Si antes estaba en oferta y ahora 'PriceOverview' es nulo o no tiene descuento,
                                    // significa que volvió a su precio normal.
                                    game.OnOffer = false;
                                    game.DiscountPercentage = 0;
                                    if (game.Price > 0) game.FinalPrice = game.Price;
                                }
                                else
                                {
                                    // 🔄 Esto actualiza dinámicamente tanto si subió, bajó o si el descuento bajó a 0
                                    game.Price = data.PriceOverview.Initial / 100m;
                                    game.FinalPrice = data.PriceOverview.Final / 100m;
                                    game.DiscountPercentage = data.PriceOverview.DiscountPercent;

                                    // Si 'DiscountPercent' vino en 0, 'OnOffer' pasa a ser false automáticamente, limpiando la oferta vieja.
                                    game.OnOffer = data.PriceOverview.DiscountPercent > 0;
                                }
                            }
                        }
                        else
                        {
                            // Si Steam no devuelve éxito o el juego fue eliminado de la tienda, asumimos que no está en oferta
                            game.OnOffer = false;
                        }
                    }
                    else if ((int)response.StatusCode == 429)
                    {
                        await Task.Delay(60000); // Anti-ban
                        continue;
                    }

                    dbContext.Entry(game).State = EntityState.Modified;
                    await dbContext.SaveChangesAsync();
                }
                catch
                {
                    // Error individual silencioso
                }

                await Task.Delay(1500); // Retraso de 1.5 segundos
            }
        }
    }
}
