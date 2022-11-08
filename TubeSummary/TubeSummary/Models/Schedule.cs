using System.Collections.Generic;

namespace TubeSummary.Models
{
    public class Schedule
    {
        public string Name { get; set; }
        public List<KnownJourney> KnownJourneys { get; set; }
    }
}