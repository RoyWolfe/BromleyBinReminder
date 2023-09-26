namespace Tests
{
    using BromleyBinReminder;
    using BromleyBinReminder.Options;
    using Ical.Net;
    using Ical.Net.CalendarComponents;
    using Ical.Net.DataTypes;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using System;
    using Telegram.Bots;
    using Telegram.Bots.Requests;
    using Xunit;

    public class RunnerTests
    {
        [Fact]
        public async void LoadsEventForToday()
        {
            ILogger logger = Substitute.For<ILogger>();

            var yesterday = DateTime.Now.AddDays(1);
            DateTime testDate = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 18, 30, 5);
            var calendar = MakeTestCalendar(testDate, "foo");
            var calendarFetcher = new TestBinCalendarFetcher(calendar, logger, new BromleyApiOptions());

            var botClient = Substitute.For<IBotClient>();
            var runner = new BromleyBinToTelegramRunner(new TelegramBinPoster(botClient, new TelegramOptions()), calendarFetcher, logger);

            await runner.RunBinReminder();

            botClient.Received().HandleAsync(Arg.Is<SendText>(t => t.Text.Contains("foo")));
        }

        private static Calendar MakeTestCalendar(DateTime testDate, string summary)
        {
            var theEvent = new CalendarEvent
            {
                DtStart = new CalDateTime(testDate),
                Summary = summary
            };

            var calendar = new Calendar
            {
                Events = { theEvent }
            };

            return calendar;
        }
    }
}