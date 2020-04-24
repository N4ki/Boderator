using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.Extensions
{
	public static class StringExtensions
	{
		public static int CountStrings(this String str, String search)
		{
			return (str.Length - str.Replace(search, "").Length) / search.Length;
		}
	}
}
