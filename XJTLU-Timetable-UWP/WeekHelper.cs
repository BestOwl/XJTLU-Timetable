using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XJTLU_Timetable_UWP
{
    public static class WeekHelper
    {
        public const int Interval_EndOfWeek = 6;
        public const int Interval_StartOfNextWeek = 7;
        public const int Interval_EndOfNextWeek = 13;

        public static DateTime GetStartDayOfWeek(DateTime currentTime)
        {
            if (currentTime.DayOfWeek == DayOfWeek.Sunday)
            {
                return currentTime.Date.AddDays(-6);
            }
            return currentTime.Date.AddDays(DayOfWeek.Monday - currentTime.DayOfWeek);
        }

        public static DateTime GetStartDayOfWeek()
        {
            return GetStartDayOfWeek(DateTime.Today);
        }

        public static DateTime GetEndDayOfWeek(this ClassCache classCache)
        {
            return classCache.StartOfWeek.AddDays(6);
        }

        /// <summary>
        /// Get a DateTime value represents the last second of this week
        /// </summary>
        public static DateTime GetEndOfWeek(DateTime currentTime)
        {
            return GetStartDayOfWeek(currentTime).AddDays(7).AddMilliseconds(-1);
        }

        public static DateTime GetEndOfWeek()
        {
            return GetEndOfWeek(DateTime.Now);
        }

        public  static string GetDateStr(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }
    }
}
