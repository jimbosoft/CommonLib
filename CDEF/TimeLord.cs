using System;
using System.Diagnostics;

namespace CommonLib.CDEF
{
	/// <summary>
	/// TimeLord is the all encompassing time class used by the WIS group
	/// for generation of all the Date / Time representations that are
	/// required for information delivery.
	/// </summary>
	public class TimeLord
	{
		// member variables
		DateTime mDateTime;

		// constants
		protected const Int64	TimeUTC_START_OF_TIME = -59963500800000;
		protected const Int64	TimeUTC_END_OF_TIME   = 255606191999999;
		protected const Int64	TimeUTC_NO_DATE       = -9223372036854775807;
		protected const int		TimeUTC_MS_PER_DAY    = 86400000;

		#region Constructors

		/// <summary>
		/// Default Constructor.
		/// As no date value is passed, default to date time of creation.
		/// </summary>
		public TimeLord()
		{
			mDateTime = DateTime.Now;
		}

		/// <summary>
		/// Alternate Constructor.
		/// Set date time based on a DateTime value.
		/// </summary>
		/// <param name="newDateTime">DateTime - new date time</param>
		public TimeLord(DateTime newDateTime)
		{
			mDateTime = newDateTime;
		}

		/// <summary>
		/// Alternate Constructor.
		/// Set date (no time) based on an Int16 (CDEF Date format) value.
		/// </summary>
		/// <param name="newDate">Int16 - new date time.</param>
		public TimeLord(UInt16 newDate)
		{
			importUI16Date(newDate);
		}

		/// <summary>
		/// Alternate Constructor.
		/// Set date time based on an Int64 (CDEF TimeUTC format) value.
		/// </summary>
		/// <param name="newDateTime"></param>
		public TimeLord(Int64 newDateTime)
		{
			importUTCDateTime(newDateTime);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Return to caller standard DateTime construct.
		/// </summary>
		public DateTime dtDateTime
		{
			get { return mDateTime; }
			set { mDateTime = value; }
		}

		/// <summary>
		/// Return to caller an Int16 hashed Date.
		/// </summary>
		public UInt16 ui16Date
		{
			get { return exportUI16Date(); }
			set { importUI16Date(value); }
		}

		/// <summary>
		/// Return to caller an Int64 UTC date.
		/// </summary>
		public Int64 utcDateTime
		{
			get { return exportUTCDateTime(); }
			set { importUTCDateTime(value); }
		}

		#endregion

		#region ui16Date Handlers

		/// <summary>
		/// exportUI16Date()
		/// Extract Year, Month, Day from the internal DateTime value and
		/// generate a hashed Int16 Date value.
		/// </summary>
		/// <returns>An UInt16 hashed Date value.</returns>
		private UInt16 exportUI16Date()
		{
			UInt16 ui16ReturnValue = 0;

			// initial variables...
			int iYear = mDateTime.Year, iMonth = mDateTime.Month, iDay = mDateTime.Day;

			// We only want a two-digit year,
			// if it is 99 then we assume 1999, otherwise 20xx.
			iYear = (iYear % 100);

			// Return a 16 bit number representing the given date
			ui16ReturnValue = (UInt16)((((iYear * 12) + (iMonth - 1)) * 31) + (iDay - 1));

			return ui16ReturnValue;
		}

		/// <summary>
		/// importUI16Date()
		/// Break down a hashed Int16 Date value into a useable form.
		/// </summary>
		/// <param name="newDate">hashed UInt16 Date.</param>
		private void importUI16Date(UInt16 newDate)
		{
			// initial variables...
			int iYear = 0, iMonth = 0, iDay = 0;

			// Determine the day, month & year from the given date

			// 1. Handle Year...
			iYear  = (newDate / 31) / 12;
			if(iYear < 90)
			{
				iYear = iYear + 2000;
			}
			else
			{
				iYear = iYear + 1900;
			}

			// 2. Handle Month...
			iMonth = ((newDate / 31) % 12) + 1;

			// 3. Handle Day...
			iDay   = (newDate % 31) + 1;

			// 4. Update mDateTime...
			try
			{
				mDateTime = new DateTime(iYear, iMonth, iDay);
			}
			catch(Exception e)
			{
				// an error?
				Debug.WriteLine(e.Message);
			}
		}

		#endregion

		#region utcDateTime Handlers

		/// <summary>
		/// exportUTCDateTime()
		/// Insane set of calculations needed to generate a TimeUTC value.
		/// </summary>
		/// <returns>Int64 - UTC Date Time</returns>
		private Int64 exportUTCDateTime()
		{
			Int64 i64ReturnValue = 0;

			// prelim values...
			int iYear = mDateTime.Year, iMonth = mDateTime.Month, iDay = mDateTime.Day,
				iHours = mDateTime.Hour, iMinutes = mDateTime.Minute, iSeconds = mDateTime.Second,
				iMillisecond = mDateTime.Millisecond, iTimeOfDay = 0;

			// calculate time of day now... for simpler processing later
			iTimeOfDay = (((((iHours * 60) + iMinutes) * 60) + iSeconds) * 1000) + iMillisecond;

			// temp variables...
			Int32 x, y;
			Int64 z;

			if( iYear < 0		|| iYear > 9999		||
				iMonth < 1		|| iMonth > 12		||
				iDay < 1		|| iDay > 31		||
				iTimeOfDay >= TimeUTC_MS_PER_DAY	||
				iTimeOfDay < 0)
			{
				// if date is outside of certain bounds, return 0
				i64ReturnValue = 0;
			}
			else
			{
				x = (-36525 * 4) + 366; // set to 01/01/-399
				y = iYear + 399;

				while((y > 399) && (x < (36525 * 4 * 4))) // dont go past 1601
				{	// step to year 0001, 0401, 0801, 1201 or 1601
					x += (36525 * 4); //add 400 years of pre 1601 days
					y -= 400;
				}

				while((y > 99) && (x < (36525 * 16))) // dont go past 1601
				{	// step to 1401, 1501, 1601
					x += 36525; // add 100 years of pre 1601 days
					y -= 100;
				}

				while(y > 399)
				{
					x += ((36524 * 3) + 36525); // add 400 years of post 1601 days
					y -= 400;
				}

				while(y > 99)
				{
					x += 36524; // add 100 yrs of post 1601 days
					y -= 100;
				}

				while(y > 19)
				{
					x += (1461 * 5); // add 20 yrs of days
					y -= 20;
				}

				while(y > 3)
				{
					x += 1461; // add 4 yrs of days
					y -= 4;
				}

				while(y > 0)
				{
					x += 365; // add 1 yr of days
					y --;
				}

				int[] daysBeforeMonths = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };

				bool bLeapYear = testLeapYear(iYear);

				x = x + daysBeforeMonths[iMonth-1] + iDay - 1;

				if((bLeapYear == true) && (iMonth > 2))
				{
					x++;
				}

				if(iYear > 1582)
				{
					x -= 10; // 10 days missing from 1582
				}
				else if(iYear == 1582)
				{
					if(iMonth > 10)
					{
						x -= 10; // October had 10 days missing
					}
					else if(iMonth == 10)
					{
						if (iDay > 4)
						{
							x -= 10; // 5th through the 14th inclusive were missing
						}
					}
				}

				z = x;
				z -= 694022; // adjust to account for difference between 01/01/0000 and 01/03/1900
				z = (z * TimeUTC_MS_PER_DAY) + iTimeOfDay;

				i64ReturnValue = z;
			}

			return i64ReturnValue;
		}

		/// <summary>
		/// importUTCDateTime()
		/// Insane set of calculations needed to parse a TimeUTC value.
		/// </summary>
		/// <param name="newDateTime">Int64 - UTC Date Time</param>
		private void importUTCDateTime(Int64 newDateTime)
		{
			// prelim values...
			int iYear = 0, iMonth = 0, iDay = 0, iHour = 0, iMinute = 0, iSecond = 0,
				iMillisecond = 0, iTimeOfDay = 0;

			// temp variables...
			Int64 z;
			Int32 x;

			// valid times are 01/01/0000 00:00:00.000 to 31/12/9999 23:59:59.999
			if((newDateTime == TimeUTC_NO_DATE) || (newDateTime < TimeUTC_START_OF_TIME))
			{
				// set date time to earliest possible utc time.
				mDateTime = new DateTime(0, 1, 1, 0, 0, 0, 0);
			}
			else if(newDateTime > TimeUTC_END_OF_TIME)
			{
				// if too late, set date time to latest possible utc time.
				mDateTime = new DateTime(9999, 12, 31, 23, 59, 59, 999);
			}
			else
			{
				z = (((Int64)840062) * TimeUTC_MS_PER_DAY) + newDateTime; // adjust back to 01/03/-400 00:00:00.000
				iTimeOfDay = (int)(z % TimeUTC_MS_PER_DAY); // time of day
				x = (int)(z / TimeUTC_MS_PER_DAY); // number of days

				if(x > 724142) // after 04/10/1582
				{
					x += 10; // add 10 days to compensate for missing days in October 1582
				}

				iYear = -400;

				while((iYear < 1600) && (x >= (36525 * 4)))
				{
					iYear += 400;
					x -= (36525 * 4); // subtract a four Julian centuries
				}

				while((iYear < 1600) && (x >= 36525))
				{
					iYear += 100;
					x -= 36525; // subtract a Julian century
				}

				while(x >= ((36524 * 3) + 36525))
				{
					iYear += 400;
					x -= ((36524 * 3) + 36525); // subtract 4 Gregorian centuries
				}

				if(iYear > 1599)
				{
					while(x > 36524)
					{
						iYear += 100;
						x -= 36524; // subtract a Gregorian century
					}
				}

				while(x > (1461 * 7))
				{
					iYear += 28;
					x -= (1461 * 7); // subtract 7 Olympiads
				}

				while(x > 1461)
				{
					iYear += 4;
					x -= 1461; // subtract an Olympiad
				}

				while(x > 366)
				{
					iYear++;
					x -= 365; // subtract a year
				}

				int[] daysInMonths = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

				if(testLeapYear(iYear + 1) == true)
				{
					daysInMonths[1] = 29;
				}

				iMonth = 2; // March

				while(x >= daysInMonths[iMonth])
				{
					x -= daysInMonths[iMonth];
					iMonth++; // pre increment
					iMonth = iMonth % 12;

					if(iMonth == 0)
					{
						iYear++;
					}
				}

				iMonth++;
				iDay = x + 1;

				bool bLeapYear = testLeapYear(iYear);

				if(bLeapYear == false)
				{
					if(iMonth == 2)
					{
						if(iDay == 29)
						{
							iMonth = 3;
							iDay = 1;
						}
					}
				}

				// time calculations...
				iHour = iTimeOfDay / (60 * 60 * 1000);
				iMinute = (iTimeOfDay / (60 * 1000)) % 60;
				iSecond = (iTimeOfDay / 1000) % 60;
				iMillisecond = iTimeOfDay % 1000;

				// update mDateTime...
				try
				{
					mDateTime = new DateTime(iYear, iMonth, iDay, iHour, iMinute, iSecond, iMillisecond);
				}
				catch(Exception e)
				{
					// an error?
					Debug.WriteLine(e.Message);
				}
			}
		}

		/// <summary>
		/// testLeapYear()
		/// Confirm that the year passed is a Leap Year or not.
		/// </summary>
		/// <param name="iYear">Int16 - Year to test</param>
		/// <returns>bool - success or failure</returns>
		private bool testLeapYear(int iYear)
		{
			bool bReturnValue = false;

			if(iYear > 1600)
			{
				if(iYear % 400 == 0)
				{
					bReturnValue = true;
				}
				else if(iYear % 100 == 0)
				{
					bReturnValue = false;
				}
				else if(iYear % 4 == 0)
				{
					bReturnValue = true;
				}
				else
				{
					bReturnValue = false;
				}
			}
			else
			{
				bReturnValue = (iYear % 4 == 0);
			}

			return bReturnValue;
		}

		#endregion

		#region Method Overrides

		/// <summary>
		/// Overrided method used to produce text output of class.
		/// </summary>
		/// <returns>Date / time string.</returns>
		public override string ToString()
		{
			return mDateTime.ToString();
		}

		/// <summary>
		/// Method used to produce text output of class.
		/// </summary>
		/// <returns>Formatted date / time string.</returns>
		public string ToString(string format)
		{
			return mDateTime.ToString(format);
		}

		#endregion
	}
}
