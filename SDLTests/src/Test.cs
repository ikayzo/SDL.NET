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
using System.IO;
using SDL;

namespace SDLTests {
    public class Test {
		private static int assertCount=0;
		private static int failures=0;
		
		// Tag datastructure tests
		const string TAG = "Tag";
		const string TAG_WRITE_PARSE = "Tag Write Parse";
		
		// Basic Types Tests
		const string STRING_DECLARATIONS = "string Declarations";	
		const string CHARACTER_DECLARATIONS = "Character Declarations";
		const string NUMBER_DECLARATIONS = "Number Declarations";	
		const string BOOLEAN_DECLARATIONS = "Boolean Declarations";
		const string NULL_DECLARATION = "Null Declaration";	
		const string DATE_DECLARATIONS = "Date Declarations";		
		const string TIME_SPAN_DECLARATIONS = "Time Span Declarations";	
		const string DATE_TIME_DECLARATIONS = "Date Time Declarations";	
		const string BINARY_DECLARATIONS = "Binary Declarations";		
	
		// Structure Tests
		const string EMPTY_TAG = "Empty Tag";
		const string VALUES = "Values";		
		const string ATTRIBUTES = "Attributes";	
		const string VALUES_AND_ATTRIBUTES = "Values and Attributes";		
		const string CHILDREN = "Children";	
		const string NAMESPACES = "Namespaces";	
		
		////////////////////////////////////////////////////////////////////////////
		// Tag Tests
		////////////////////////////////////////////////////////////////////////////
		private static void TestTag() {	
			Console.WriteLine("Doing basic Tag tests...");
			
			// Test to make sure Tag ignores the order in which attributes are
			// added.
			Console.WriteLine("    Making sure attributes are consistently ordered...");
			Tag t1 = new Tag("test");
			t1["foo"]="bar";
            t1["john"]="doe";
			
			Tag t2 = new Tag("test");
            t2["john"]="doe";
			t2["foo"]="bar";
			
			AssertEquals(TAG, t1, t2);
			
			Console.WriteLine("    Making sure tags with different structures return " +
					"false from .equals...");
			
			t2.Value="item";
			AssertNotEquals(TAG, t1, t2);
			
			t2.RemoveValue("item");
			t2["another"] = "attribute";
			AssertNotEquals(TAG, t1, t2);
			
			Console.WriteLine("    Checking attributes namespaces...");
			
			t2["name"]="bill";

            // setting attributes with namespaces
			t2["private","smoker"]=true;
			t2["public", "hobby"]="hiking";
			t2["private", "nickname"]="tubby";

			AssertEquals(TAG, t2.GetAttributesForNamespace("private"),
					Map("smoker",true,"nickname","tubby"));
		}	

		private static void TestTagWriteParse(string fileName, Tag root) {
			
			Console.WriteLine("Doing Tag write/parse tests for file " + fileName + "...");
			
			// Write out the contents of a tag, read the output back in and
			// test for equality.  This is a very rigorous test for any non-trivial
			// file.  It tests the parsing, output, and .equals implementation.
			Console.WriteLine("    Write out the tag and read it back in...");
			
			AssertEquals(TAG_WRITE_PARSE, root, new Tag("test")
					.ReadString(root.ToString()).GetChild("root"));
			
			
		}
		
		////////////////////////////////////////////////////////////////////////////
		// Basic Types Tests
		////////////////////////////////////////////////////////////////////////////
		
		private static void TestStrings(Tag root) {
			Console.WriteLine("Doing string tests...");
			Console.WriteLine("    Doing basic tests including new line handling...");
			AssertEquals(STRING_DECLARATIONS, root.GetChild("string1").Value, "hello");
			AssertEquals(STRING_DECLARATIONS, root.GetChild("string2").Value, "hi");
			AssertEquals(STRING_DECLARATIONS, root.GetChild("string3").Value, "aloha");
			AssertEquals(STRING_DECLARATIONS, root.GetChild("string4").Value, "hi there");		
			AssertEquals(STRING_DECLARATIONS, root.GetChild("string5").Value, "hi there joe");		
			AssertEquals(STRING_DECLARATIONS, root.GetChild("string6").Value, "line1\nline2");
			AssertEquals(STRING_DECLARATIONS, root.GetChild("string7").Value, "line1\nline2");
			AssertEquals(STRING_DECLARATIONS, root.GetChild("string8").Value, "line1\nline2\nline3");			
			AssertEquals(STRING_DECLARATIONS, root.GetChild("string9").Value,
					"Anything should go in this line without escapes \\ \\\\ \\n " +
					"\\t \" \"\" ' ''");
			AssertEquals(STRING_DECLARATIONS, root.GetChild("string10").Value, "escapes \"\\\n\t");
			
			Console.WriteLine("    Checking unicode strings...");
			AssertEquals(STRING_DECLARATIONS, root.GetChild("japanese").Value, "\u65e5\u672c\u8a9e");
			AssertEquals(STRING_DECLARATIONS, root.GetChild("korean").Value, "\uc5ec\ubcf4\uc138\uc694");
			AssertEquals(STRING_DECLARATIONS, root.GetChild("russian").Value,
					"\u0437\u0434\u0440\u0430\u0432\u0441\u0442\u0432\u0443\u043b\u0442\u0435");
			
			Console.WriteLine("    More new line tests...");
			AssertContains(STRING_DECLARATIONS, (string)root.GetChild("xml").Value,
					"<text>Hi there!</text>");
			AssertEquals(STRING_DECLARATIONS, root.GetChild("line_test").Value,
					"\nnew line above and below\n");
		}
	
		private static void TestCharacters(Tag root) {		
			Console.WriteLine("Doing character tests...");
			AssertEquals(CHARACTER_DECLARATIONS, root.GetChild("char1").Value, 'a');
			AssertEquals(CHARACTER_DECLARATIONS, root.GetChild("char2").Value, 'A');
			AssertEquals(CHARACTER_DECLARATIONS, root.GetChild("char3").Value, '\\');
			AssertEquals(CHARACTER_DECLARATIONS, root.GetChild("char4").Value, '\n');
			AssertEquals(CHARACTER_DECLARATIONS, root.GetChild("char5").Value, '\t');
			AssertEquals(CHARACTER_DECLARATIONS, root.GetChild("char6").Value, '\'');
			AssertEquals(CHARACTER_DECLARATIONS, root.GetChild("char7").Value, '"');
			
			Console.WriteLine("    Doing unicode character tests...");
			AssertEquals(CHARACTER_DECLARATIONS, root.GetChild("char8").Value, '\u65e5');
			AssertEquals(CHARACTER_DECLARATIONS, root.GetChild("char9").Value, '\uc5ec');
			AssertEquals(CHARACTER_DECLARATIONS, root.GetChild("char10").Value, '\u0437');
		}
	
		private static void TestNumbers(Tag root) {		
			Console.WriteLine("Doing number tests...");
			
			Console.WriteLine("    Testing ints...");
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("int1").Value, 0);
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("int2").Value, 5);
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("int3").Value, -100);
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("int4").Value, 234253532);
			
			Console.WriteLine("    Testing longs...");
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("long1").Value, 0L);
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("long2").Value, 5L);
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("long3").Value, 5L);
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("long4").Value, 3904857398753453453L);		
			
			Console.WriteLine("    Testing floats...");
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("float1").Value, 1F);
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("float2").Value, .23F);
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("float3").Value, -.34F);
	
			Console.WriteLine("    Testing doubles...");
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("double1").Value, 2D);
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("double2").Value, -.234D);
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("double3").Value, 2.34D);
			
			Console.WriteLine("    Testing decimals (BigDouble in Java)...");
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("decimal1").Value,
					Convert.ToDecimal("0"));
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("decimal2").Value,
					Convert.ToDecimal("11.111111"));
			AssertEquals(NUMBER_DECLARATIONS, root.GetChild("decimal3").Value,
					Convert.ToDecimal("234535.3453453453454345345341242343"));		
		}
		
		private static void TestBooleans(Tag root) {		
			Console.WriteLine("Doing bool tests...");
	
			AssertEquals(BOOLEAN_DECLARATIONS, root.GetChild("light-on").Value, true);
			AssertEquals(BOOLEAN_DECLARATIONS, root.GetChild("light-off").Value, false);
			AssertEquals(BOOLEAN_DECLARATIONS, root.GetChild("light1").Value, true);
			AssertEquals(BOOLEAN_DECLARATIONS, root.GetChild("light2").Value, false);
		}
		
		private static void TestNull(Tag root) {		
			Console.WriteLine("Doing null test...");
	
			AssertEquals(NULL_DECLARATION, root.GetChild("nothing").Value, null);
		}	
		
		private static void TestDates(Tag root) {
			Console.WriteLine("Doing date tests...");
	
			AssertEquals(DATE_DECLARATIONS, root.GetChild("date1").Value,
					GetDate(2005,12,31));
			AssertEquals(DATE_DECLARATIONS, root.GetChild("date2").Value,
					GetDate(1882,5,2));
			AssertEquals(DATE_DECLARATIONS, root.GetChild("date3").Value,
					GetDate(1882,5,2));		
			AssertEquals(DATE_DECLARATIONS, root.GetChild("_way_back").Value,
					GetDate(582,9,16));			
		}
	
		private static void TestTimeSpans(Tag root) {
			Console.WriteLine("Doing time span tests...");
	
			AssertEquals(TIME_SPAN_DECLARATIONS, root.GetChild("time1").Value,
					new TimeSpan(0,12,30,0,0));
			AssertEquals(TIME_SPAN_DECLARATIONS, root.GetChild("time2").Value,
					new TimeSpan(0,24,0,0,0));
			AssertEquals(TIME_SPAN_DECLARATIONS, root.GetChild("time3").Value,
					new TimeSpan(0,1,0,0,0));	
			AssertEquals(TIME_SPAN_DECLARATIONS, root.GetChild("time4").Value,
					new TimeSpan(0,1,0,0,0));	
			AssertEquals(TIME_SPAN_DECLARATIONS, root.GetChild("time5").Value,
					new TimeSpan(0,12,30,2,0));	
			AssertEquals(TIME_SPAN_DECLARATIONS, root.GetChild("time6").Value,
					new TimeSpan(0,12,30,23,0));	
			AssertEquals(TIME_SPAN_DECLARATIONS, root.GetChild("time7").Value,
					new TimeSpan(0,12,30,23,100));	
			AssertEquals(TIME_SPAN_DECLARATIONS, root.GetChild("time8").Value,
					new TimeSpan(0,12,30,23,120));	
			AssertEquals(TIME_SPAN_DECLARATIONS, root.GetChild("time9").Value,
					new TimeSpan(0,12,30,23,123));
			
			Console.WriteLine("    Checking time spans with days...");
			AssertEquals(TIME_SPAN_DECLARATIONS, root.GetChild("time10").Value,
					new TimeSpan(34,12,30,23,100));	
			AssertEquals(TIME_SPAN_DECLARATIONS, root.GetChild("time11").Value,
					new TimeSpan(1,12,30,0,0));	
			AssertEquals(TIME_SPAN_DECLARATIONS, root.GetChild("time12").Value,
					new TimeSpan(5,12,30,23,123));
			
			Console.WriteLine("    Checking negative time spans...");
			AssertEquals(TIME_SPAN_DECLARATIONS, root.GetChild("time13").Value,
					new TimeSpan(0,-12,-30,-23,-123));	
			AssertEquals(TIME_SPAN_DECLARATIONS, root.GetChild("time14").Value,
					new TimeSpan(-5,-12,-30,-23,-123));
		}
		
		private static void TestDateTimes(Tag root) {
			Console.WriteLine("Doing date time tests...");
	
			AssertEquals(DATE_TIME_DECLARATIONS, root.GetChild("date_time1").Value,
					GetDateTime(2005,12,31,12,30,0,0,null));	
			AssertEquals(DATE_TIME_DECLARATIONS, root.GetChild("date_time2").Value,
					GetDateTime(1882,5,2,12,30,0,0,null));	
			AssertEquals(DATE_TIME_DECLARATIONS, root.GetChild("date_time3").Value,
					GetDateTime(2005,12,31,1,0,0,0,null));	
			AssertEquals(DATE_TIME_DECLARATIONS, root.GetChild("date_time4").Value,
					GetDateTime(1882,5,2,1,0,0,0,null));	
			AssertEquals(DATE_TIME_DECLARATIONS, root.GetChild("date_time5").Value,
					GetDateTime(2005,12,31,12,30,23,120,null));	
			AssertEquals(DATE_TIME_DECLARATIONS, root.GetChild("date_time6").Value,
					GetDateTime(1882,5,2,12,30,23,123,null));	
			
			Console.WriteLine("    Checking timezones...");
			AssertEquals(DATE_TIME_DECLARATIONS, root.GetChild("date_time7").Value,
					GetDateTime(1882,5,2,12,30,23,123,"JST"));	
			AssertEquals(DATE_TIME_DECLARATIONS, root.GetChild("date_time8").Value,
					GetDateTime(985,04,11,12,30,23,123,"PST"));	
		}
		
		private static void TestBinaries(Tag root) {
			Console.WriteLine("Doing binary tests...");
			AssertEquals(BINARY_DECLARATIONS, root.GetChild("hi").Value,
				new byte[] {104,105});	
			AssertEquals(BINARY_DECLARATIONS, root.GetChild("png").Value,
				Convert.FromBase64String(
					"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAKnRFWHRDcmVhdGlvbiBUaW1l" +
					"AERpIDQgTXJ6IDIwMDMgMDA6MjQ6MDQgKzAxMDDdSQ6OAAAAB3RJTUUH0wMEAAcllPlrJgAA" +
					"AAlwSFlzAAAK8AAACvABQqw0mAAAAARnQU1BAACxjwv8YQUAAADQSURBVHjaY2CgEDCCyZn/" +
					"3YHkDhL1ejCkM+5kgXJ2zDQmXueShwwMh9+ALWSEGcCQfhZIvHlDnAk8PAwMHBxgJtyAa7bX" +
					"UdT8/cvA8Ps3hP7zB4FBYn/+vGbweqyJaoCmpiaKASDFv35BNMBoZMzwGKKOidJYoNgAuBdm" +
					"naXQgHRKDfgagxD89w8S+iAaFICwGIHFAgjrHUczAByySAaAMEgDLBphhv7/D8EYLgDZhAxA" +
					"mkAKYYbAMMwwDAOQXYDuDXRXgDC6AR7SW8jITNQAACjZgdj4VjlqAAAAAElFTkSuQmCC"						
				));		
		}

		////////////////////////////////////////////////////////////////////////////
		// Structure Tests (values, attributes, children)
		////////////////////////////////////////////////////////////////////////////
		
		public static void TestEmptyTag(Tag root) {
			Console.WriteLine("Doing empty tag test...");
			
			AssertEquals(EMPTY_TAG, root.GetChild("empty_tag"), new Tag("empty_tag"));
		}
		
		public static void TestValues(Tag root) {
			Console.WriteLine("Doing values tests...");
	
			AssertEquals(VALUES, root.GetChild("values1").Values, List("hi"));
			AssertEquals(VALUES, root.GetChild("values2").Values, List("hi","ho"));
			AssertEquals(VALUES, root.GetChild("values3").Values, List(1, "ho"));	
			AssertEquals(VALUES, root.GetChild("values4").Values, List("hi",5));
			AssertEquals(VALUES, root.GetChild("values5").Values, List(1,2));	
			AssertEquals(VALUES, root.GetChild("values6").Values, List(1,2,3));	
			AssertEquals(VALUES, root.GetChild("values7").Values,
					List(null,"foo",false,GetDate(1980,12,5)));		
			AssertEquals(VALUES, root.GetChild("values8").Values,
					List(null, "foo", false, GetDateTime(1980,12,5,12,30,0,0,null),
							"there", new TimeSpan(0,15,23,12,234)));
			AssertEquals(VALUES, root.GetChild("values9").Values,
					List(null, "foo", false, GetDateTime(1980,12,5,12,30,0,0,null),
							"there", GetDateTime(1989,8,12,15,23,12,234,"JST")));
			AssertEquals(VALUES, root.GetChild("values10").Values,
					List(null, "foo", false, GetDateTime(1980,12,5,12,30,0,0,null),
							"there", new TimeSpan(0,15,23,12,234), "more stuff"));
			AssertEquals(VALUES, root.GetChild("values11").Values,
					List(null, "foo", false, GetDateTime(1980,12,5,12,30,0,0,null),
							"there", new TimeSpan(123,15,23,12,234),
							"more stuff here"));		
			AssertEquals(VALUES, root.GetChild("values12").Values, List(1,3));
			AssertEquals(VALUES, root.GetChild("values13").Values, List(1,3));
			AssertEquals(VALUES, root.GetChild("values14").Values, List(1,3));	
			AssertEquals(VALUES, root.GetChild("values15").Values, List(1,2,4,5,6));
			AssertEquals(VALUES, root.GetChild("values16").Values, List(1,2,5));	
			AssertEquals(VALUES, root.GetChild("values17").Values, List(1,2,5));		
			AssertEquals(VALUES, root.GetChild("values18").Values, List(1,2,7));	
			AssertEquals(VALUES, root.GetChild("values19").Values,
					List(1,3,5,7));	
			AssertEquals(VALUES, root.GetChild("values20").Values,
					List(1,3,5));		
			AssertEquals(VALUES, root.GetChild("values21").Values,
					List(1,3,5));			 
			AssertEquals(VALUES, root.GetChild("values22").Values,
					List("hi","ho","ho",5,"hi"));			
		}
		
		public static void TestAttributes(Tag root) {
			Console.WriteLine("Doing attribute tests...");
	
			AssertEquals(ATTRIBUTES, root.GetChild("atts1").Attributes,
					Map("name","joe"));
			AssertEquals(ATTRIBUTES, root.GetChild("atts2").Attributes,
					Map("size",5));	
			AssertEquals(ATTRIBUTES, root.GetChild("atts3").Attributes,
					Map("name","joe","size",5));	
			AssertEquals(ATTRIBUTES, root.GetChild("atts4").Attributes,
					Map("name","joe","size",5,"smoker",false));
			AssertEquals(ATTRIBUTES, root.GetChild("atts5").Attributes,
					Map("name","joe","smoker",false));
			AssertEquals(ATTRIBUTES, root.GetChild("atts6").Attributes,
					Map("name","joe","smoker",false));	
			AssertEquals(ATTRIBUTES, root.GetChild("atts7").Attributes,
					Map("name","joe"));
			AssertEquals(ATTRIBUTES, root.GetChild("atts8").Attributes,
					Map("name","joe","size",5,"smoker",false,"text","hi","birthday",
							GetDate(1972,5,23)));
			AssertEquals(ATTRIBUTES, root.GetChild("atts9")["key"],
					new byte[] {109, 121, 107, 101, 121});
		}	
		
		public static void TestValuesAndAttributes(Tag root) {
			Console.WriteLine("Doing values and attributes tests...");
	
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts1")
					.Values, List("joe"));
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts1")
					.Attributes, Map("size", 5));		
			
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts2")
					.Values, List("joe"));
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts2")
					.Attributes, Map("size", 5));			
			
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts3")
					.Values, List("joe"));
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts3")
					.Attributes, Map("size", 5));
	
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts4")
					.Values, List("joe"));
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts4")
					.Attributes, Map("size", 5, "weight", 160, "hat", "big"));
			
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts5")
					.Values, List("joe", "is a\n nice guy"));
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts5")
					.Attributes, Map("size", 5, "smoker", false));		
	
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts6")
					.Values, List("joe", "is a\n nice guy"));
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts6")
					.Attributes, Map("size", 5, "house", "big and\n blue"));
			
			//////////
			
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts7")
					.Values, List("joe", "is a\n nice guy"));
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts7")
					.Attributes, Map("size", 5, "smoker", false));
			
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts8")
					.Values, List("joe", "is a\n nice guy"));
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts8")
					.Attributes, Map("size", 5, "smoker", false));
			
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts9")
					.Values,List("joe", "is a\n nice guy"));
			AssertEquals(VALUES_AND_ATTRIBUTES, root.GetChild("valatts9")
					.Attributes, Map("size", 5, "smoker", false));			
		}	
		
		public static void TestChildren(Tag root) {
			Console.WriteLine("Doing children tests...");
	
			Tag parent = root.GetChild("parent");
			
			AssertEquals(CHILDREN, parent.Children.Count, 2);
			AssertEquals(CHILDREN, parent.Children[1].Name,
					"daughter");
			
			
			Tag grandparent = root.GetChild("grandparent");
			
			AssertEquals(CHILDREN, grandparent.Children.Count, 2);
			// recursive fetch of children
			AssertEquals(CHILDREN, grandparent.GetChildren(true).Count, 6);		
			AssertEquals(CHILDREN, grandparent.GetChildren("son", true).Count, 2);	
			
			Tag grandparent2 = root.GetChild("grandparent2");
			AssertEquals(CHILDREN, grandparent2.GetChildren("child", true)
					.Count, 5);
			AssertEquals(CHILDREN, grandparent2.GetChild("daughter", true)
                ["birthday"],GetDate(1976,04,18));
			
			Tag files = root.GetChild("files");
			
			AssertEquals(CHILDREN, files.GetChildrenValues("content"),
					List("c:/file1.txt", "c:/file2.txt", "c:/folder"));
			
			Tag matrix = root.GetChild("matrix");
			
			AssertEquals(CHILDREN, matrix.GetChildrenValues("content"),
					List(List(1,2,3),List(4,5,6)));		
		}
		
		public static void TestNamespaces(Tag root) {
			Console.WriteLine("Doing namespaces tests...");
			
			AssertEquals(NAMESPACES, root.GetChildrenForNamespace("person", true)
					.Count, 8);
			
			Tag grandparent2 = root.GetChild("grandparent3");
			
			// get only the attributes for Akiko in the public namespace
			AssertEquals(NAMESPACES, grandparent2.GetChild("daughter", true)
                    .GetAttributesForNamespace("public"), Map("name", "Akiko",
							"birthday", GetDate(1976,04,18)));
		}

		static void Main(string[] args) {
			Go();
		}
		
		public static void Go() {
	
			Console.WriteLine("Begin tests");
	
			try { TestTag(); } catch(Exception e) {
				ReportException(TAG, e);
			}		
			
			TestBasicTypes();
			TestStructures();
			
			Console.WriteLine();
			
			Console.WriteLine("Checked " + assertCount + " assertions");
			
			if(failures==0)
				Console.WriteLine(":-) All tests succeeded!");
			else
				Console.WriteLine("Summary: " + failures + (failures==1 ? " failure" :
					" failures."));
		}
	
		private static void TestBasicTypes() {
			
			Console.WriteLine("Reading test_basic_types.sdl");
			
			Tag root = null;
			
			try {
				root=new Tag("root").ReadFile("test_basic_types.sdl");
			} catch(IOException ioe) {
				ReportException("Problem reading test_basic_types.sdl", ioe);
			} catch(SDLParseException spe) {
				ReportException("Problem parsing test_basic_types.sdl", spe);
			}
			
			Console.WriteLine("Successfully read and parsed test_basic_types.sdl");	
			
			try { TestTagWriteParse("test_basic_types.sdl", root); } catch(Exception e) {
				ReportException(TAG_WRITE_PARSE, e);
			}
			
			try { TestStrings(root); } catch(Exception e) {
				ReportException(STRING_DECLARATIONS, e);
			}
			
			try { TestCharacters(root); } catch(Exception e) {
				ReportException(CHARACTER_DECLARATIONS, e);
			}
			
			try { TestNumbers(root); } catch(Exception e) {
				ReportException(NUMBER_DECLARATIONS, e);
			}		
	
			try { TestBooleans(root); } catch(Exception e) {
				ReportException(BOOLEAN_DECLARATIONS, e);
			}
			
			try { TestNull(root); } catch(Exception e) {
				ReportException(NULL_DECLARATION, e);
			}
			
			try { TestDates(root); } catch(Exception e) {
				ReportException(DATE_DECLARATIONS, e);
			}
			
			try { TestTimeSpans(root); } catch(Exception e) {
				ReportException(TIME_SPAN_DECLARATIONS, e);
			}
			
			try { TestDateTimes(root); } catch(Exception e) {
				ReportException(DATE_TIME_DECLARATIONS, e);
			}
			
			try { TestBinaries(root); } catch(Exception e) {
				ReportException(BINARY_DECLARATIONS, e);
			}
		}
		
		private static void TestStructures() {
			
			Console.WriteLine("Reading test_structures.sdl");
			
			Tag root = null;
			
			try {
				root=new Tag("root").ReadFile("test_structures.sdl");
			} catch(IOException ioe) {
				ReportException("Problem reading test_structures.sdl", ioe);
			} catch(SDLParseException spe) {
				ReportException("Problem parsing test_structures.sdl", spe);
			}
			
			Console.WriteLine("Successfully read and parsed test_structures.sdl");
	
			try { TestTagWriteParse("test_structures.sdl", root); } catch(Exception e) {
				ReportException(TAG_WRITE_PARSE, e);
			}		
			
			try { TestEmptyTag(root); } catch(Exception e) {
				ReportException(EMPTY_TAG, e);
			}		
			
			try { TestValues(root); } catch(Exception e) {
				ReportException(VALUES, e);
			}
			
			try { TestAttributes(root); } catch(Exception e) {
				ReportException(ATTRIBUTES, e);
			}
			
			try { TestValuesAndAttributes(root); } catch(Exception e) {
				ReportException(VALUES_AND_ATTRIBUTES, e);
			}	
			
			try { TestChildren(root); } catch(Exception e) {
				ReportException(CHILDREN, e);
			}	
			
			try { TestNamespaces(root); } catch(Exception e) {
				ReportException(NAMESPACES, e);
			}
		}
          
		private static void AssertEquals(string testName, object o1, object o2) {
			assertCount++;
			if(!Equals(o1, o2)) {
				failures++;
				Console.WriteLine("!! Failure: " + testName + " - " + SDLUtil.Format(o1) +
						" does not equal " + SDLUtil.Format(o2));
			}
		}
		
		private static void AssertNotEquals(string testName, object o1, object o2) {
			assertCount++;
			if(Equals(o1, o2)) {
				failures++;
				Console.WriteLine("!! Failure: " + testName + " - " + SDLUtil.Format(o1) +
						" equals " + SDLUtil.Format(o2));
			}	
		}
		
		private static void AssertContains(string testName, string o1, string o2) {
			assertCount++;
			if(o1==null) {
				failures++;
				Console.WriteLine("!! Failure: " + testName +
						" (contains type assertion) - first string argument " +
						"is null");
			} else if(o2==null) {
				failures++;
				Console.WriteLine("!! Failure: " + testName + " (contains type " +
						"assertion) - second string argument is null");
			} else {
				if(!o1.Contains(o2)) {
					failures++;
					Console.WriteLine("!! Failure: " + testName + " - " + o1 +
							" does not contain " + o2);
				}
			}
		}
		
		private static void AssertTrue(string testName, string evalstring,
				bool value) {
			assertCount++;
			if(!value) {
				failures++;
				Console.WriteLine("!! Failure: " + testName + " - " + evalstring +
						" is false");			
			}
		}
		
		private static void AssertFalse(string testName, string evalstring,
				bool value) {
			assertCount++;
			if(value) {
				failures++;
				Console.WriteLine("!! Failure: " + testName + " - " + evalstring +
						" is true");			
			}
		}
		
		private static void ReportException(string testName, Exception e) {
			failures++;
			Console.WriteLine("!! Failure: " + testName + " - " + e.Message);
		}
		
		////////////////////////////////////////////////////////////////////////////
		// Utility methods
		////////////////////////////////////////////////////////////////////////////
	
		private static SDLDateTime GetDate(int year, int month, int day) {	
			return new SDLDateTime(year, month, day);
		}

        private static SDLDateTime GetDateTime(int year, int month, int day, int hour,
				int minute, int second, int millisecond, string timeZone) {
		
            return new SDLDateTime(year, month, day, hour, minute, second,
                millisecond, timeZone);
		}
		
		private static List<object> List(params object[] obs) {
			List<object> list = new List<object>();
			foreach(object o in obs)
				list.Add(o);
			return list;
		}
		
		/**
		 * Make a map from alternating key/value pairs
		 */
		private static IDictionary<string,object> Map(params object[] obs) {
			SortedDictionary<string,object> map =
                new SortedDictionary<string,object>();
			for(int i=0; i<obs.Length;)
				map[(string)obs[i++]]=obs[i++];		
			return map;
		}
		
		private static bool Equals(object o1, object o2) {
			if (o1==null)
				return o2==null;
			else if(o2==null)
				return false;
			
            if(o1 is IDictionary<string,object>) {
            // note sure why this is necessary, but .Equals isn't working for
            // dictionaries

                if (!(o2 is IDictionary<string, object>))
                    return false;

                IDictionary<string, object> d1 = (IDictionary<string, object>)o1;
                IDictionary<string, object> d2 = (IDictionary<string, object>)o2;

                if (d1.Count != d2.Count)
                    return false;

                foreach (string key in d1.Keys) {
                    if (!Equals(d1[key], d2[key]))
                        return false;
                }

                return true;
            } else if(o1 is IList<object>) {
            // note sure why this is necessary, but .Equals isn't working for
            // lists
                if (!(o2 is IList<object>))
                    return false;

                IList<object> list1 = (IList<object>)o1;
                IList<object> list2 = (IList<object>)o2;

                if (list1.Count != list2.Count)
                    return false;

                for(int i=0; i<list1.Count; i++) {
                    if (!Equals(list1[i],list2[i]))
                        return false;
                }

                return true;
            } else if (o1 is byte[]) {
                if (!(o2 is byte[]))
                    return false;

                byte[] b1 = (byte[])o1;
                byte[] b2 = (byte[])o2;

                if (b1.Length != b2.Length)
                    return false;

                for(int i=0; i<b1.Length; i++) {
                    if (b2[i] != b1[i])
                        return false;
                }
                return true;
            } else {
                return o1.Equals(o2);
            }
		}

        private static string MapToString(IDictionary<string,object> map) {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            bool first = true;
            foreach(string key in map.Keys) {
                if(first)
                    first=false;
                else
                    sb.Append(" ");
                sb.Append(key + "=" + map[key]);
            }
            sb.Append("]");

            return sb.ToString();
        }
    }
}
