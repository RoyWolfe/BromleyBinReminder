namespace Tests
{
    using BromleyBinReminder.Options;
    using Ical.Net;
    using Ical.Net.CalendarComponents;
    using Ical.Net.DataTypes;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Linq;
    using Xunit;

    public class CalendarFetcherTests
    {
        [Fact]
        public async void LoadsEventForToday()
        {
            DateTime testDate = new DateTime(2020, 1, 1, 18, 30, 5);

            var calendar = MakeTestCalendar(testDate);

            ILogger logger = NSubstitute.Substitute.For<ILogger>();
            var sut = new TestBinCalendarFetcher(calendar, logger, new BromleyApiOptions());

            var binEvents = await sut.LoadBinEvents(testDate);

            Assert.Single(binEvents);

            var binEvent = binEvents.First();
            Assert.Equal(testDate.ToString("yyyy-MM-dd"), binEvent.Date.ToString("yyyy-MM-dd"));

            Assert.Single(binEvent.Bins);
            Assert.Equal("foo", binEvent.Bins.First());
        }

        [Fact]
        public async void LoadsNoEventForYesterday()
        {
            DateTime testDate = new DateTime(2020, 1, 1, 18, 30, 5);

            var calendar = MakeTestCalendar(testDate);

            ILogger logger = NSubstitute.Substitute.For<ILogger>();
            var sut = new TestBinCalendarFetcher(calendar, logger, new BromleyApiOptions());

            var binEvents = await sut.LoadBinEvents(testDate.Subtract(TimeSpan.FromDays(1)));

            Assert.Empty(binEvents);
        }

        [Fact]
        public async void LoadsNoEventForTomorrow()
        {
            DateTime testDate = new DateTime(2020, 1, 1, 18, 30, 5);

            var calendar = MakeTestCalendar(testDate);

            ILogger logger = NSubstitute.Substitute.For<ILogger>();
            var sut = new TestBinCalendarFetcher(calendar, logger, new BromleyApiOptions());

            var binEvents = await sut.LoadBinEvents(testDate.Add(TimeSpan.FromDays(1)));

            Assert.Empty(binEvents);
        }

        private static Calendar MakeTestCalendar(DateTime testDate)
        {
            var theEvent = new CalendarEvent
            {
                DtStart = new CalDateTime(testDate),
                Summary = "foo"
            };

            var calendar = new Calendar
            {
                Events = { theEvent }
            };

            return calendar;
        }
    }
}