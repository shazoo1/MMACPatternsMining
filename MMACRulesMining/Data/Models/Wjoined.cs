using System;
using System.Collections.Generic;

namespace MMACRulesMining.Data.Models
{
    public partial class Wjoined
    {
        public int? EventId { get; set; }
        public DateTime? EventDate { get; set; }
        public decimal? Temp { get; set; }
        public decimal? Press { get; set; }
        public int? Humid { get; set; }
        public string Winddir { get; set; }
        public int? Windspeed { get; set; }
        public int? Windgust { get; set; }
        public string Cloudiness { get; set; }
        public string Event1 { get; set; }
        public string Event2 { get; set; }
        public decimal? Mintemp { get; set; }
        public decimal? Maxtemp { get; set; }
        public string Clheight { get; set; }
        public decimal? Sight { get; set; }
        public decimal? Droppoint { get; set; }
        public string Precip { get; set; }
        public string Surface { get; set; }
        public decimal? Mingroundtemp { get; set; }
        public string Snowdepth { get; set; }
    }
}
