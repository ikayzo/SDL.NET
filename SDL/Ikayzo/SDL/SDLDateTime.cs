/*
 * Simple Declarative Language (SDL) for .NET
 * Copyright 2005 Ikayzo, inc.
 *
 * This program is free software. You can distribute or modify it under the 
 * terms of the GNU Lesser General Public License version 2.1 as published by  
 * the Free Software Foundation.
 *
 * This program is distributed AS IS and WITHOUT WARRANTY. OF ANY KIND,
 * INCLUDING MERCHANTABILITY OR FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, contact the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Ikayzo.SDL {

    /// <summary>
    /// An SDL compliant DateTime class.  This class wraps a DateTime instance
    /// and allows a timezone to be specified.  SDL uses the Gregorian calendar
    /// and a 24 hour clock (0-23)
    /// </summary>
    /// <author>Daniel Leuck from Ikayzo</author>
    public class SDLDateTime {

        private DateTime dateTime;
        private String timeZone;

        /// <summary>
        /// Create an instance representing an SDL date or SDL DateTime type.
        /// </summary>
        /// <param name="dateTime">The date (and possibly time)</param>
        /// <param name="timeZone">The timezone specified as a timezone ID
        /// (ex America/Los_Angeles), GMT(+/-)hh(:mm), or three letter code
        /// (such as PST or JST).  For three letter timezones that are
        /// ambiguous behavior is unspecified.  In these cases, the full
        /// timezone ID or GMT(+/-)hh(:mm) format should be used.
        /// </param>
        /// <exception cref="System.ArgumentException">If the time zone is not
        /// a valid time zone ID (ex. America/Los_Angeles), three letter
        /// abbreviation (ex. HST), or GMT(+/-)hh(:mm) formatted custom
        /// timezone (ex. GMT+02 or GMT+02:30)</exception>
        public SDLDateTime(DateTime dateTime, string timeZone) {
            this.dateTime = dateTime;

            if (timeZone == null)
                timeZone = getCurrentTimeZone();
            this.timeZone = timeZone;
        }

        /// <summary>
        /// Create a date instance
        /// </summary>
        /// <param name="year">The year</param>
        /// <param name="month">The month (1-12)</param>
        /// <param name="day">The day of the month</param>
        public SDLDateTime(int year, int month, int day)
            : this(new DateTime(year, month, day), null) {
        }

        /// <summary>
        /// Create a date time instance
        /// </summary>
        /// <param name="year">The year</param>
        /// <param name="month">The month (1-12)</param>
        /// <param name="day">The day of the month (1-31)</param>
        /// <param name="hour">The hour of the day (0-23)</param>
        /// <param name="minute">0-59</param>
        /// <param name="second">0-59</param>
        /// <param name="millisecond">0-999</param>
        public SDLDateTime(int year, int month, int day, int hour, int minute,
            int second, int millisecond)
            : this(new DateTime(year, month, day, hour, minute, second,
                millisecond), null) {
        }

        /// <summary>
        /// Create a date time instance with the specified timezone
        /// </summary>
        /// <param name="year">The year</param>
        /// <param name="month">The month (1-12)</param>
        /// <param name="day">The day of the month (1-31)</param>
        /// <param name="hour">The hour of the day (0-23)</param>
        /// <param name="minute">0-59</param>
        /// <param name="second">0-59</param>
        /// <param name="millisecond">0-999</param>
        /// <param name="timeZone">The timezone for this date/time</param>
        public SDLDateTime(int year, int month, int day, int hour, int minute,
            int second, int millisecond, string timeZone)
            : this(new DateTime(year, month, day, hour, minute, second,
                millisecond), timeZone) {
        }

        /// <summary>
        /// Get the DateTime instance being wrapped
        /// </summary>
        public DateTime DateTime {
            get {
                return dateTime;
            }
        }

        /// <summary>
        /// Indicates whether or not this SDLDateTime includes a time component
        /// (as apposed to just being a date)
        /// </summary>
        public bool HasTime {
            get {
                return dateTime.Hour!=0 || dateTime.Minute!=0 ||
                    dateTime.Second!=0 || dateTime.Millisecond!=0;
            }
        }

        /// <summary>
        /// The timezone for this SDLDateTime.  This is only relevant if a
        /// time component is present (timeSet==true)
        /// </summary>
        public String TimeZone {
            //TODO: Validation of timezones

            get {
                return timeZone;
            }
        }

        /// <summary>
        /// Get the year
        /// </summary>
        public int Year { get { return dateTime.Year; } }

        /// <summary>
        /// Get the month (1-12)
        /// </summary>
        public int Month { get { return dateTime.Month; } }

        /// <summary>
        /// Get the day of the month (1-31)
        /// </summary>
        public int Day { get { return dateTime.Day; } }

        /// <summary>
        /// Get the hour of the day (0-23)
        /// </summary>
        public int Hour { get { return dateTime.Hour; } }

        /// <summary>
        /// Get the minute (0-59)
        /// </summary>
        public int Minute { get { return dateTime.Minute; } }

        /// <summary>
        /// Get the second (0-59)
        /// </summary>
        public int Second { get { return dateTime.Second; } }

        /// <summary>
        /// Get the millisecond (0-999)
        /// </summary>
        public int Millisecond { get { return dateTime.Millisecond; } }

        /// <summary>
        /// Create a string in the standard SDL format.  For dates the format
        /// is <c>yyyy/MM/dd</c> for date/time instances the format is
        /// <c>yyyy/MM/dd HH:mm:ss.fff-z</c>
        /// </summary>
        /// <returns>SDL code describing this tag</returns>
        public override string ToString() {
            StringBuilder sb = new StringBuilder("" + Year + "/");
            if (Month < 10)
                sb.Append("0");
            sb.Append(Month + "/");
            if (Day < 10)
                sb.Append("0");
            sb.Append(Day);

            if (this.HasTime) {
                sb.Append(" ");

                if (Hour < 10)
                    sb.Append("0");
                sb.Append(Hour);

                sb.Append(":");

                if (Minute < 10)
                    sb.Append("0");
                sb.Append(Minute);

                if(Second!=0 || Millisecond!=0) {
                    sb.Append(":");

                    if (Second < 10)
                        sb.Append("0");
                    sb.Append(Second);

                    if(Millisecond!=0) {
                        sb.Append(".");

                        string millis = "" + Millisecond;
                        if (millis.Length == 1)
                            millis = "00" + millis;
                        else if(millis.Length==2)
                            millis = "0" + millis;

                        sb.Append(millis);
                    }
                }

                sb.Append("-");
                sb.Append(TimeZone==null ? getCurrentTimeZone() :
                    TimeZone);
            }

            return sb.ToString();
        }

        internal static string getCurrentTimeZone() {
            TimeZone tz = System.TimeZone.CurrentTimeZone;
            TimeSpan ts = tz.GetUtcOffset(DateTime.Now);

            StringBuilder sb = new StringBuilder("GMT");
            sb.Append(ts.Hours < 0 ? "-" : "+");

            int hours = Math.Abs(ts.Hours);
            sb.Append((hours < 10) ? "0" + hours : "" + hours);

            sb.Append(":");

            int minutes = Math.Abs(ts.Minutes);
            sb.Append((minutes < 10) ? "0" + minutes : "" + minutes);

            return sb.ToString();
        }

        /// <summary>
        /// Tests for equality using <c>ToString()</c>
        /// </summary>
        /// <param name="obj">The object to test</param>
        /// <returns>true if <c>ToString().Equals(obj.ToString)</c></returns>
        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            return ToString().Equals(obj.ToString());
        }

        /// <summary>
        /// Generates a hashcode using <c>ToString().GetHashCode()</c>
        /// </summary>
        /// <returns>A unique hashcode for this SDLDateTime</returns>
        public override int GetHashCode() {
            return ToString().GetHashCode();
        }
    }
}
