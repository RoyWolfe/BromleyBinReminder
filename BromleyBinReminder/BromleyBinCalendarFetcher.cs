namespace BromleyBinReminder;

using System.Net;
using Ical.Net;
using Microsoft.Extensions.Logging;
using Options;

public class BromleyBinCalendarFetcher
{
    private HttpClient client = null;
    private CookieContainer cookies = null;

    private readonly ILogger _logger;
    private BromleyApiOptions _bromleyApiOptions;

    public BromleyBinCalendarFetcher(ILogger logger, BromleyApiOptions bromleyApiOptions)
    {
        _logger = logger;
        _bromleyApiOptions = bromleyApiOptions;

        CreateHttpClient();
    }

    private void CreateHttpClient()
    {
        cookies = new CookieContainer();
        HttpClientHandler handler = new HttpClientHandler();
        handler.CookieContainer = cookies;

        client = new HttpClient(handler);
    }

    public async Task<IEnumerable<BinEvent>> LoadBinEvents()
    {
        _logger.LogDebug("Ensuring calendar file exists");
        await EnsureRecentCalendarFile();

        var icalText = await File.ReadAllTextAsync(_bromleyApiOptions.CalendarSaveFileName);
        var calendar = Calendar.Load(icalText);

        var tomorrowsEvents = calendar.Events
            .Where(e => e.DtStart.AsSystemLocal.Day == DateTime.Now.AddDays(1).Day)
            .GroupBy(e => e.DtStart.AsSystemLocal)
            .Select(g => new BinEvent
            {
                Date = g.Key.Date,
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
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                var foo = await response.Content.ReadAsStringAsync();
                _logger.LogError(e, foo);
                throw;
            }

            var responseBody = await response.Content.ReadAsStreamAsync();

            using (var fileStream = File.Create(_bromleyApiOptions.CalendarSaveFileName))
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(fileStream);
            }
        }
    }
}