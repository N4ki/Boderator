using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.Extensions
{
	public static class DateTimeExtensions
	{
		public static DateTime MakeEndOfDay(this DateTime date)
		{
			date = date.AddHours(23 - date.Hour);
			date = date.AddMinutes(59 - date.Minute);
			date = date.AddSeconds(59 - date.Second);
			return date;
		}
	}
}
