namespace BromleyBinReminder;

using Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bots;
using Microsoft.Extensions.Logging;

public class ConfigureThings
{
    public ServiceProvider GetServiceProvider()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

        var config = builder.Build();

        var bromleyApiOptions = config.GetSection("BromleyApiOptions").Get<BromleyApiOptions>();
        var telegramOptions = config.GetSection("TelegramOptions").Get<TelegramOptions>();

        var services = new ServiceCollection()
            .AddBotClient(telegramOptions.BotKey).Services;

        services.AddSingleton<TelegramOptions>(telegramOptions);
        services.AddSingleton<BromleyApiOptions>(bromleyApiOptions); 
        services.AddSingleton<TelegramBinPoster>();
        services.AddSingleton<BromleyBinToTelegramRunner>();
        services.AddSingleton<BromleyBinCalendarFetcher>();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("BromleyBinReminder", LogLevel.Debug)
                .AddConsole();
        });

        services.AddSingleton<ILoggerFactory>(loggerFactory);
        services.AddTransient<ILogger>(serviceProvider => serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(BromleyBinReminder)));

        return services.BuildServiceProvider();
    }
}