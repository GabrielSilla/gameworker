using GamingWorkerService;
class Program
{
    static async Task Main(string[] args)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<GameUsbWorker>();

        var worker = new GameUsbWorker(logger);

        await worker.StartAsync(CancellationToken.None);

        Console.WriteLine("Pressione qualquer tecla para parar...");
        Console.ReadKey();

        await worker.StopAsync(CancellationToken.None);
    }
}