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

    /// <summary>Various SDL related utility methods</summary>
    /// <author>Daniel Leuck from Ikayzo</author>
    public class SDLUtil {

        /// <summary>
        /// <para>The SDL standard time format HH:mm:ss.SSS-z</para>
        /// <para>Note: SDL uses a 24 hour clock (0-23)</para>
        /// </summary>
	    public const string TIME_FORMAT = "HH:mm:ss.fff-z";

        /// <summary>
        /// <para>The SDL standard date format yyyy/MM/dd</para>
        /// <para>Note: SDL uses the Gregorian calendar</para>
        /// </summary>
	    public const string DATE_FORMAT = "yyyy/MM/dd";

        /// <summary>
        /// <para>The SDL standard date time format yyyy/MM/dd HH:mm:ss.fff-z
        /// </para>
        /// <para>Note: SDL uses a 24 hour clock (0-23)</para>
        /// <para>Note: SDL uses the Gregorian calendar</para>
        /// </summary>
        public const string DATE_TIME_FORMAT = DATE_FORMAT + " " +
		    TIME_FORMAT;

        /// <summary>Validates an SDL identifier String.  SDL Identifiers must
        /// start with a Unicode letter or underscore (_) and contain only 
        /// unicode letters, digits, underscores, and dashes.
        /// </summary>
        /// 
        /// <exception cref="System.ArgumentException">If <c>identifier</c> is
        /// not a valid SDL identifier</exception>
        public static string ValidateIdentifier(string identifier) {
            if (identifier == null || identifier.Length == 0)
                throw new ArgumentException("SDL identifiers cannot be " +
                        "null or empty.");

            if (!Char.IsLetter(identifier[0]) && identifier[0]!='_')
                throw new ArgumentException("'" + identifier[0] +
                        "' is not a legal first character for an SDL " +
                        "identifier. SDL Identifiers must start with a " +
                        "unicode letter or underscore (_).");

            int identifierSize = identifier.Length;
            for (int i = 1; i < identifierSize; i++)
                if (!Char.IsLetterOrDigit(identifier[i]) && identifier[i]!='_'
                    && identifier[i] != '-' && identifier[i] != '.')
                    throw new ArgumentException("'" + identifier[i] +
                            "' is not a legal character for an SDL " +
                            "identifier. SDL Identifiers must start with a " +
                            "unicode letter or underscore (_) followed by " +
                            "zero or more unicode letters, digits, dashes " +
                            "(-) or underscores (_)");

            return identifier;
        }

        /// <summary>
        /// Create an SDL string representation for an object (note: Strings
        /// and Characters will be surrounded by quotes.)
        /// </summary>
        /// <param name="obj">The object to format</param>
        /// <returns>an SDL string representation for an object</returns>
	    public static string Format(object obj) {
		    return Format(obj, true);
	    }

        /// <summary>
        /// Create an SDL string representation for an object
        /// </summary>
        /// <param name="obj">The object to format</param>
        /// <param name="addQuotes">Quotes will be added to Strings and
        ///     Characters if true</param>
        /// <returns>an SDL string representation for an object</returns>
	    public static string Format(object obj, bool addQuotes) {
            if (obj == null) {
                return "null";
            } else if(obj is string) {
			    if(addQuotes)
				    return "\"" + Escape((string)obj) + "\"";
			    else
				    return Escape((string)obj);
		    } else if(obj is Char) {
			    if(addQuotes) {
                    return "'" + Escape((Char)obj) + "'";
			    } else {
				    return Escape((Char)obj);
			    }
		    } else if(obj is Decimal) {
			    return obj.ToString() + "BD";
		    } else if(obj is Single) {
			    return obj.ToString() + "F";
		    } else if(obj is Int64) {
			    return obj.ToString() + "L";
		    } else if(obj is byte[]) {
			    return "[" + Convert.ToBase64String((byte[])obj) + "]";
            } else if (obj is Boolean) {
                return obj.Equals(true) ? "true" : "false";
            } else if (obj is TimeSpan) {
                TimeSpan span = (TimeSpan)obj;
                StringBuilder sb = new StringBuilder();
                if(span.Days!=0) {
                    sb.Append(span.Days).Append("d:");
                    String hs = "" + (int)Math.Abs(span.Hours);
                    if(hs.Length==1)
                        hs="0" + hs;
                    sb.Append(hs);
                } else {
                    if(span.Hours<0)
                        sb.Append('-');

                    String hs = "" + (int)Math.Abs(span.Hours);
                    if (hs.Length == 1)
                        hs = "0" + hs;
                    sb.Append(hs);
                }
                sb.Append(":");
                String ms = "" + (int)Math.Abs(span.Minutes);
                if (ms.Length == 1)
                    ms = "0" + ms;
                sb.Append(ms);

                sb.Append(":");
                String ss = "" + (int)Math.Abs(span.Seconds);
                if (ss.Length == 1)
                    ss = "0" + ss;
                sb.Append(ss);

                if (span.Milliseconds != 0)
                {
                    String milis = "" + (int)Math.Abs(span.Milliseconds);
                    if (milis.Length == 1)
                        milis = "00" + milis;
                    else if (milis.Length == 2)
                        milis = "0" + milis;

                    sb.Append(".").Append(milis);

                    string s = sb.ToString();

                    int i = s.Length - 1;
                    for (; i > -1; i--)
                        if (s[i] != '0') break;

                    return s.Substring(0, i + 1);
                } else {
                    return sb.ToString();
                }
            }
    		
            // We don't have to explicitly handle SDLDateTime because it
            // returns the proper SDL format from its ToString method
		    return obj.ToString();
        }

	    private static string Escape(string s) {
		    StringBuilder sb = new StringBuilder();

            int size = s.Length;
		    for(int i=0; i<size; i++) {
			    char c = s[i];
			    if(c=='\\')
				    sb.Append("\\\\");
			    else if(c=='"')
				    sb.Append("\\\"");
			    else if(c=='\t')
				    sb.Append("\\t");
			    else if(c=='\r')
				    sb.Append("\\r");	
			    else if(c=='\n')
				    sb.Append("\\n");
			    else
				    sb.Append(c);
		    }
    		
		    return sb.ToString();
	    }
    	
	    private static string Escape(char c) {
		    switch(c) {
			    case '\\': return "\\\\";
			    case '\'': return "\\'";
			    case '\t': return "\\t";
			    case '\r': return "\\r";
			    case '\n': return "\\n";
			    default: return ""+c;
		    }
	    }

        /// <summary>
        /// Coerce the type to a standard SDL type or throw an argument 
	    /// exception if no coercion is possible.
        /// </summary>
        /// <example>
	    ///     string, char, int, long, float, double, decimal,
	    ///         bool, SDLDateTime, TimeSpan -> No change
        ///     byte, sbyte, short, ushort -> int
        ///     uint -> long
	    ///     DateTime -> SDLDateTime    
        /// </example>
        /// 
        /// <param name="obj">The object to coerce</param>
        /// <returns>The same obj or an SDL equivalent</returns>
        /// <exception cref="System.ArgumentException">If no coercion is
        /// possible</exception>
	    public static object CoerceOrFail(Object obj) {	
		    if(obj==null)
			    return null;
    		
		    if(obj is string || obj is char || obj is int || obj is long ||
                obj is float || obj is double || obj is decimal ||
                obj is bool || obj is TimeSpan || obj is SDLDateTime ||
                obj is byte[]) {

			    return obj;
		    } else if(obj is DateTime) {
                return new SDLDateTime((DateTime)obj, null);
            } else if(obj is sbyte || obj is byte || obj is short ||
                obj is ushort) {
                return Convert.ToInt32(obj);
            } else if(obj is uint) {
                return Convert.ToInt64(obj);
            }

		    throw new ArgumentException("" + obj.GetType() +
                " is not coercible to an SDL type");
        }

        /// <summary>
        /// Get the value represented by a string containing an SDL literal.
        /// </summary>
        /// <param name="literal">An SDL literal</param>
        /// <returns>A value for an SDL literal</returns>
        /// <exception cref="FormatException">(or subclass SDLParseException) If
        /// the string cannot be interpreted as an SDL literal</exception>
        /// <exception cref="ArgumentNullException">If <c>literal</c> is null.
        /// </exception>
        public static Object Value(String literal) {
            if (literal == null)
                throw new ArgumentNullException("literal argument to " +
                        "SDL.Value(string) cannot be null");

            if (literal.StartsWith("\"") || literal.StartsWith("`"))
                return Parser.ParseString(literal);
            if (literal.StartsWith("'"))
                return Parser.ParseCharacter(literal);
            if (literal.Equals("null"))
                return null;
            if (literal.Equals("true") || literal.Equals("on"))
                return true;
            if (literal.Equals("false") || literal.Equals("off"))
                return false;
            if (literal.StartsWith("["))
                return Parser.ParseBinary(literal);
            if (literal[0] != '/' && literal.IndexOf('/') != -1)
                return Parser.ParseDateTime(literal);
            if (literal[0] != ':' && literal.IndexOf(':') != -1)
                return Parser.ParseTimeSpan(literal);
            if ("01234567890-.".IndexOf(literal[0]) != -1)
                return Parser.ParseNumber(literal);

            throw new FormatException("String " + literal + " does not " +
                    "represent an SDL type.");
        }

        /// <summary>
        /// Parse the string of values and return a list.  The string is handled
        /// as if it is the values portion of an SDL tag.
        /// </summary>
        /// <example>
        /// <code>
        ///     List list = SDLUtil.list("1 true 12:24:01");
        /// </code>
        /// </example>
        /// <param name="valueList">A string of space separated SDL literals
        /// </param>
        /// <returns>A list of values</returns>
        /// <exception cref="FormatException">(or subclass SDLParseException) If
        /// the string contains literals that cannot be parsed</exception>
        /// <exception cref="ArgumentNullException">If <c>valueList</c>
        /// is null.</exception>
        public static IList<object> List(String valueList) {
            if(valueList==null)
                throw new ArgumentNullException("valueList cannot be null");
            return new Tag("root").ReadString(valueList).GetChild("content")
                    .Values;
        }

        /// <summary>
        /// Parse a string representing the attributes portion of an SDL tag
        /// and return the results as a map.
        /// </summary>
        /// <example>
        /// <code>
        ///     IDictionary<string,pbject> d =
        ///         SDLUtil.Map("value=1 debugging=on time=12:24:01");
        /// </code>
        /// </example>
        /// <param name="attributeString">A string of space separated key=value
        /// pairs</param>
        /// <returns>A map created from the attribute string</returns>
        /// <exception cref="FormatException">(or subclass SDLParseException) If
        /// the string contains literals that cannot be parsed or the map is
        /// malformed</exception>
        /// <exception cref="ArgumentNullException">If <c>attributeString</c>
        /// is null.</exception>
        public static IDictionary<string, object> Map(string attributeString) {
            if(attributeString==null)
                throw new ArgumentNullException(
                    "attributeString cannot be null");
            return new Tag("root").ReadString("atts " + attributeString)
                                        .GetChild("atts").Attributes;
        }	
    }
}
