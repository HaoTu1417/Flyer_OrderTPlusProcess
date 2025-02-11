// Decompiled with JetBrains decompiler
// Type: vn_matchstock.TradeTime
// Assembly: vn_matchstock, Version=2023.10.31.237, Culture=neutral, PublicKeyToken=null
// MVID: F04EE2D5-AF78-4C81-9699-CA665C6E41C5
// Assembly location: /Users/tunghaotu/www/service/vn_matchstock/vn_matchstock.dll

using Common.Services;
using System;
using System.Collections.Generic;

#nullable enable
namespace vn_matchstock
{
    public class TradeTime
    {
        private List<DateTime> _holidays;
        private static readonly TimeSpan _post_time = new TimeSpan(0, 0, 5);

        public TradeTime(string market) => this._holidays = StockHolidayService.FindHolidays(market);

        private bool Check(DateTime dt)
        {
            DayOfWeek dayOfWeek = dt.DayOfWeek;
            return dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday || this._holidays.Contains(dt);
        }

        public bool IsMatchTime(DateTime dt, Exchange exchange, out DateTime time_limit)
        {
            time_limit = dt.Date;
            if (this.IsHoliday(dt))
                return false;
            TimeSpan timeOfDay = dt.TimeOfDay;
            foreach (Period period in exchange.periods)
            {
                if (timeOfDay >= period.start && timeOfDay <= period.end + TradeTime._post_time)
                {
                    time_limit = time_limit.Add(period.end);
                    return true;
                }
            }
            return false;
        }

        public bool IsHoliday(DateTime dt) => this.Check(dt);
    }
}