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
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcesarCupoDiarioAsync();
                }
                catch (Exception)
                {
                }
                await Task.Delay(TimeSpan.FromHours(4), stoppingToken);
            }
        }
        private async Task ProcesarCupoDiarioAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NewsletterDbContext>();

            var verificarOfertasExistentes = await dbContext.Games
                .Where(g => g.SteamAppId != null
                         && g.State == true
                         && g.OnOffer == true)
                .OrderBy(g => g.LastPriceCheck)
                .Take(3000)
                .ToListAsync();
            var buscarNuevasOfertas = await dbContext.Games
                .Where(g => g.SteamAppId != null
                         && g.State == true
                         && g.OnOffer == false
                         && g.Price > 0
                         && g.ReleaseDate != null)
                .OrderBy(g => g.LastPriceCheck)
                .Take(11000)
                .ToListAsync();
            var juegosAActualizar = verificarOfertasExistentes.Concat(buscarNuevasOfertas).ToList();
            if (juegosAActualizar.Count == 0) return;
            int maximoHilosSimultaneos = 3;
            using var semaforo = new SemaphoreSlim(maximoHilosSimultaneos);

            var tareas = juegosAActualizar.Select(async game =>
            {
                await semaforo.WaitAsync();
                using var httpClient = _httpClientFactory.CreateClient();

                try
                {
                    game.LastPriceCheck = DateTime.UtcNow;
                    string detailsUrl = $"https://store.steampowered.com/api/appdetails?appids={game.SteamAppId}&l=spanish&cc=ar";

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
                            game.OnOffer = false;
                        }
                        using var scopeHijo = _serviceProvider.CreateScope();
                        var dbContextHijo = scopeHijo.ServiceProvider.GetRequiredService<NewsletterDbContext>();

                        dbContextHijo.Entry(game).State = EntityState.Modified;
                        await dbContextHijo.SaveChangesAsync();
                    }
                    else if ((int)response.StatusCode == 429)
                    {
                        await Task.Delay(60000);
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    await Task.Delay(2000);
                    semaforo.Release();
                }
            });

            await Task.WhenAll(tareas);
        }
    }
}
