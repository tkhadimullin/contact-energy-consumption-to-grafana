using System;

namespace ContactEnergyPoller.Models
{
    class Usage
    {
        public string Currency { get; set; } // "NZD"
        public DateTime Date { get; set; }
        public int Hour { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public double Percentage { get; set; } // seems to represent how tall the bar should be for graphing purposes
        public string TimeZone { get; set; }
        public double UnchargedValue { get; set; } // amount of free power received during period
        public string Unit { get; set; } //  "kWh"
        public double Value { get; set; } // Total amount of power received
    }
}
