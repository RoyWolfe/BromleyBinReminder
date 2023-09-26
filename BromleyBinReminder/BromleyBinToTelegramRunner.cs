namespace BromleyBinReminder;

using Microsoft.Extensions.Logging;
using System.Text;

public class BromleyBinToTelegramRunner
{
    private readonly TelegramBinPoster _telegramBinPoster;
    private readonly BromleyBinCalendarFetcher _calendarFetcher;
    private ILogger _logger;

    public BromleyBinToTelegramRunner(TelegramBinPoster telegramBinPoster, BromleyBinCalendarFetcher calendarFetcher, ILogger logger)
    {
        _telegramBinPoster = telegramBinPoster;
        _calendarFetcher = calendarFetcher;
        _logger = logger;
    }

    public async Task RunBinReminder()
    {
        _logger.LogDebug("Loading bin events");
        var binEvents = await _calendarFetcher.LoadBinEvents(DateTime.Now.AddDays(1));

        _logger.LogDebug("Sending bin events to telegram");
        foreach (var binEvent in binEvents)
        {
            if (binEvent.Bins.Any())
            {
                var sb = new StringBuilder($"Remember to put bins out for {binEvent.Date:yyyy-MM-dd}:");

                foreach (var bin in binEvent.Bins)
                {
                    sb.Append($"\n • {bin}");
                }

                await _telegramBinPoster.PostReminderMessage(sb.ToString());
            }
            else
            {
                await _telegramBinPoster.PostReminderMessage($"There is an entry for {binEvent.Date}, but no bins are listed :(");
            }
        }
    }
}