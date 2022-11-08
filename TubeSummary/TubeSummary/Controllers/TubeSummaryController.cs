using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TubeSummary.Models;

namespace TubeSummary.Controllers
{
    public class TubeSummaryController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public TubeSummaryController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _clientFactory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get,
                "https://api.tfl.gov.uk/Line/Hammersmith-City/Timetable/940GZZLUGPS?direction=inbound");

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Bad call");
            }

            var responseString = await response.Content.ReadAsStringAsync();

            var payload = JsonConvert.DeserializeObject<TimetablePayload>(responseString);

            var now = DateTime.Now;

            const string weekdayFilter = "Monday - Friday";
            const string saturdayFilter = "Saturdays and Public Holidays";
            const string sundayFilter = "Sunday";

            var dayFilter = now.DayOfWeek switch
            {
                DayOfWeek.Sunday => sundayFilter,
                DayOfWeek.Monday => weekdayFilter,
                DayOfWeek.Tuesday => weekdayFilter,
                DayOfWeek.Wednesday => weekdayFilter,
                DayOfWeek.Thursday => weekdayFilter,
                DayOfWeek.Friday => weekdayFilter,
                DayOfWeek.Saturday => saturdayFilter,
                _ => throw new NotImplementedException(),
            };

            // Assumption: Not interested in the early hours tomorrow, or Bank Holidays
            var times = payload
                .Timetable
                .Routes.First()
                .Schedules.Single(s => s.Name == dayFilter)
                .KnownJourneys.Where(j =>
                {
                    var hour = int.Parse(j.Hour);
                    var min = int.Parse(j.Minute);
                    return hour > now.Hour || hour == now.Hour && min > now.Minute;
                })
                .Select(j => new FoundService
                {
                    Hour = j.Hour,
                    Minute = j.Minute
                })
                .OrderBy(j => j.Hour)
                .ThenBy(j => j.Minute)
                .Take(10);

            return Json(new
            {
                times
            });
        }
    }
}
