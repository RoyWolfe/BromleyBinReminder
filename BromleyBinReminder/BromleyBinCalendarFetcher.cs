﻿namespace BromleyBinReminder;

using Ical.Net;
using Microsoft.Extensions.Logging;
using Options;
using System.Net;

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

    public async Task<IEnumerable<BinEvent>> LoadBinEvents(DateTime dayToLoadFor)
    {
        _logger.LogDebug("Ensuring calendar file exists");
        await EnsureRecentCalendarFile();

        var calendar = await LoadCalendar();

        var tomorrowsEvents = calendar.Events
            .Where(e => e.DtStart.AsSystemLocal.Date == dayToLoadFor.Date)
            .GroupBy(e => e.DtStart.AsSystemLocal)
            .Select(g => new BinEvent
            {
                Date = g.Key.Date,
                Bins = g.Select(cev => cev.Summary.Replace(" collection", string.Empty))
            });

        return tomorrowsEvents;
    }

    protected virtual async Task<Calendar> LoadCalendar()
    {
        var icalText = await File.ReadAllTextAsync(_bromleyApiOptions.CalendarSaveFileName);
        var calendar = Ical.Net.Calendar.Load(icalText);
        return calendar;
    }

    protected virtual async Task EnsureRecentCalendarFile()
    {
        var calendarFileInfo = new FileInfo(_bromleyApiOptions.CalendarSaveFileName);

        if ((DateTime.UtcNow - calendarFileInfo.CreationTimeUtc) >= TimeSpan.FromDays(30))
        {
            _logger.LogInformation("Calendar is out of date, fetching new calendar ...");

            var baseUrl = "https://recyclingservices.bromley.gov.uk/waste";
            var calendarUrl = $"{baseUrl}/{_bromleyApiOptions.HouseIdentifier}/calendar.ics";

            _logger.LogInformation("Getting cookie ...");
            var bromAuthCookie = await GetBromAuthCookie(baseUrl);

            _logger.LogDebug($"Adding cookie to new URL ({calendarUrl})");
            cookies.Add(new Uri(calendarUrl), bromAuthCookie);

            _logger.LogInformation("Loading new calendar file");
            
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, calendarUrl);
            request.Headers.Referrer = new Uri($"{baseUrl}/{_bromleyApiOptions.HouseIdentifier}");
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36");
            request.Headers.Add("Cookie", $"{bromAuthCookie.Name}={bromAuthCookie.Value}");
            
            using HttpResponseMessage response = await client.SendAsync(request);
            
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

    private async Task<Cookie> GetBromAuthCookie(string cookieUrl)
    {
        var cookieName = "fixmystreet_app_session";
        
        using HttpResponseMessage cookieResponse = await client.GetAsync(cookieUrl);
        cookieResponse.EnsureSuccessStatusCode();

        var cookieCollection = cookies.GetCookies(new Uri(cookieUrl));
        var bromAuthCookie = cookieCollection[cookieName];
        _logger.LogDebug($"Got cookie: {bromAuthCookie.Name}:{bromAuthCookie.Value}");
        return bromAuthCookie;
    }
}