namespace Tests;

using BromleyBinReminder;
using BromleyBinReminder.Options;
using Ical.Net;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public class TestBinCalendarFetcher : BromleyBinCalendarFetcher
{
    private readonly Calendar _calendar;

    public TestBinCalendarFetcher(Calendar calendar, ILogger logger, BromleyApiOptions options) : base(logger, options)
    {
        _calendar = calendar;
    }

    protected override async Task<Calendar> LoadCalendar()
    {
        return _calendar;
    }

    protected override async Task EnsureRecentCalendarFile()
    {
        // nop
    }
}