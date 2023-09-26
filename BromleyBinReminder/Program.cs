using BromleyBinReminder;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = new ConfigureThings().GetServiceProvider();

var runner = serviceProvider.GetRequiredService<BromleyBinToTelegramRunner>();

await runner.RunBinReminder();