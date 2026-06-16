using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
namespace Newsletter.Services
{
   /* public class EpicBotBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly int _totalPaginas = 249;
        private readonly int _tamañoLote = 50;

        public EpicBotBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("🚀 [EPIC WORKER] Iniciando ciclo completo del catálogo...");

                    for (int paginaActual = 1; paginaActual <= _totalPaginas; paginaActual += _tamañoLote)
                    {
                        if (stoppingToken.IsCancellationRequested) break;
                        int paginasAProcesar = Math.Min(_tamañoLote, (_totalPaginas - paginaActual) + 1);
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var epicService = scope.ServiceProvider.GetRequiredService<EpicImportService>();

                            System.Diagnostics.Debug.WriteLine($"\n🤖 [EPIC WORKER] Procesando LOTE: Pág {paginaActual} a {paginaActual + paginasAProcesar - 1}");
                            var reporteDelLote = await epicService.AutomatizarCargaEpicAsync(paginaActual, paginasAProcesar);
                            if (reporteDelLote.PaginasFallidas != null && reporteDelLote.PaginasFallidas.Count > 0)
                            {
                                string logPath = Path.Combine(Directory.GetCurrentDirectory(), "EpicFailedPages_Log.txt");

                                string fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                string paginasUnidas = string.Join(", ", reporteDelLote.PaginasFallidas);
                                string lineaLog = $"[{fecha}] Fallo en Lote {paginaActual}-{paginaActual + paginasAProcesar - 1} | Páginas para revisar: {paginasUnidas}\n";
                                await File.AppendAllTextAsync(logPath, lineaLog);

                                System.Diagnostics.Debug.WriteLine($"📝 [LOG] Se anotaron {reporteDelLote.PaginasFallidas.Count} páginas fallidas en EpicFailedPages_Log.txt");
                            }
                        }
                        if (paginaActual + paginasAProcesar <= _totalPaginas)
                        {
                            System.Diagnostics.Debug.WriteLine("⏸️ [EPIC WORKER] Lote terminado. Pausando 5 minutos para enfriar memoria RAM...");
                            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                        }
                    }

                    System.Diagnostics.Debug.WriteLine("✅ [EPIC WORKER] ¡Catálogo completo (249 páginas) actualizado con éxito!");
                    System.Diagnostics.Debug.WriteLine("💤 [EPIC WORKER] Durmiendo por 24 horas. Hasta mañana...");
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ [EPIC WORKER] Error crítico global: {ex.Message}");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }
    }*/
}