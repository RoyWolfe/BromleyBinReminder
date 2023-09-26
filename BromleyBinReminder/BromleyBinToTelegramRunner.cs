namespace BromleyBinReminder;

using Ical.Net;
using Options;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

public class BromleyBinToTelegramRunner
{
    private HttpClient client = null;
    private CookieContainer cookies = null;
    
    private readonly TelegramBinPoster _telegramBinPoster;
    private readonly BromleyApiOptions _bromleyApiOptions;
    private readonly ILogger _logger;

    public BromleyBinToTelegramRunner(TelegramBinPoster telegramBinPoster, BromleyApiOptions bromleyApiOptions, ILogger logger)
    {
        _telegramBinPoster = telegramBinPoster;
        _bromleyApiOptions = bromleyApiOptions;
        _logger = logger;

        CreateHttpClient();
    }

    private void CreateHttpClient()
    {
        cookies = new CookieContainer();
        HttpClientHandler handler = new HttpClientHandler();
        handler.CookieContainer = cookies;

        client = new HttpClient(handler);
    }

    public async Task RunBinReminder()
    {
        _logger.LogDebug("Ensuring calendar file exists");
        await EnsureRecentCalendarFile();

        _logger.LogDebug("Loading bin events");
        var binEvents = await LoadBinEvents();

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

    private async Task<IEnumerable<BinEvent>> LoadBinEvents()
    {
        var icalText = await File.ReadAllTextAsync(_bromleyApiOptions.CalendarSaveFileName);
        var calendar = Calendar.Load(icalText);

        var tomorrowsEvents = calendar.Events
            .Where(e => (e.DtStart.AsSystemLocal - DateTime.Now) < TimeSpan.FromDays(1))
            .GroupBy(e => e.DtStart.AsSystemLocal)
            .Select(g => new BinEvent
            {
                Date = g.Key,
                Bins = g.Select(cev => cev.Summary.Replace(" collection", string.Empty))
            });

        return tomorrowsEvents;
    }

    private async Task EnsureRecentCalendarFile()
    {
        var calendarFileInfo = new FileInfo(_bromleyApiOptions.CalendarSaveFileName);

        if ((DateTime.UtcNow - calendarFileInfo.CreationTimeUtc) >= TimeSpan.FromDays(30))
        {
            _logger.LogInformation("Calendar is out of date, fetching new calendar ...");

            var cookieName = "fixmystreet_app_session";
            var cookieUrl = "https://recyclingservices.bromley.gov.uk/waste";
            var calendarUrl = $"https://recyclingservices.bromley.gov.uk/waste/{_bromleyApiOptions.HouseIdentifier}/calendar.ics";

            _logger.LogInformation("Getting cookie ...");
            using HttpResponseMessage cookieRresponse = await client.GetAsync(cookieUrl);
            cookieRresponse.EnsureSuccessStatusCode();

            var cookieCollection = cookies.GetCookies(new Uri(cookieUrl));
            var bromAuthCookie = cookieCollection[cookieName];
            _logger.LogDebug($"Got cookie: {bromAuthCookie.Name}:{bromAuthCookie.Value}");

            _logger.LogDebug($"Adding cookie to new URL ({calendarUrl})");
            cookies.Add(new Uri(calendarUrl), bromAuthCookie);

            _logger.LogInformation("Loading new calendar file");
            using HttpResponseMessage response = await client.GetAsync(calendarUrl);

            try
            {
                var foo = response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                var foo = await response.Content.ReadAsStringAsync();
                _logger.LogError(e, foo);
                throw;
            }

            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStreamAsync();

            using (var fileStream = File.Create(_bromleyApiOptions.CalendarSaveFileName))
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(fileStream);
            }
        }
    }
}

public class BinEvent
{
    public DateTime Date { get; set; }
    public IEnumerable<string> Bins { get; set; }
}