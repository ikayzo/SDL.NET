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
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Ikayzo.SDL {

    enum Type {
        IDENTIFIER,

        // punctuation
        COLON, EQUALS, START_BLOCK, END_BLOCK,

        // literals
        STRING, CHARACTER, BOOLEAN, NUMBER, DATE, TIME, BINARY, NULL
    }

    internal class Parser {
        private TextReader reader;
        private string line;
        private List<Token> toks;
        private StringBuilder sb;
        private bool startEscapedQuoteLine;
        internal int lineNumber = -1, pos = 0, lineLength = 0, tokenStart = 0;

        /// <summary>
        /// Create an SDL parser
        /// </summary>
        internal Parser(TextReader reader) {
            this.reader = reader;
        }

        /// <summary>
        /// Parse the reader and create a list of tags
        /// </summary>
        /// <returns>A list of tags described by the input</returns>
        /// <exception cref="SDLParserException">If the line is malformed
        /// </exception>
        /// <exception cref="System.IO.IOException">If an IO error occurs while
        /// reading the line</exception>
		internal IList<Tag> Parse() {
			
			List<Tag> tags = new List<Tag>();
			List<Token> toks;
			
			while((toks=GetLineTokens())!=null) {
				int size = toks.Count;
				
				if(toks[size-1].type==Type.START_BLOCK) {
					Tag t = ConstructTag(toks.GetRange(0, size-1));
					AddChildren(t);
					tags.Add(t);
				} else if(toks[0].type==Type.END_BLOCK){
					ParseException("No opening block ({) for close block (}).",
							toks[0].line, toks[0].position);
				} else {
					tags.Add(ConstructTag(toks));
				}
			}
			
			reader.Close();
			
			return tags;
		}
		
		private void AddChildren(Tag parent) {
			List<Token> toks;
			while((toks=GetLineTokens())!=null) {
				int size = toks.Count;
				
				if(toks[0].type==Type.END_BLOCK) {
					return;
				} else if(toks[size-1].type==Type.START_BLOCK) {
					Tag tag = ConstructTag(toks.GetRange(0, size-1));
					AddChildren(tag);
					parent.AddChild(tag);
				} else {
					parent.AddChild(ConstructTag(toks));
				}
			}		
			
			// we have to use -2 for position rather than -1 for unknown because
			// the parseException method adds 1 to line and position
			ParseException("No close block (}).", lineNumber, -2);
		}

        /// <summary>
        /// Construct a tag (but not its children) from a string of tokens
        /// </summary>
        /// <param name="toks"></param>
        /// <returns>A tag with no children</returns>
        /// <exception cref="SDLParseException">If the tokens do not represent
        /// valid SDL</exception>
		Tag ConstructTag(List<Token> toks) {
			if(toks.Count==0)
				// we have to use -2 for position rather than -1 for unknown because
				// the parseException method adds 1 to line and position
				ParseException("Internal Error: Empty token list", lineNumber, -2);
				
			Token t0 = toks[0];

			if(t0.literal) {
				toks.Insert(0, t0=new Token(this, "content", -1, -1));
			} else if(t0.type!=Type.IDENTIFIER) {
				ExpectingButGot("IDENTIFIER", "" + t0.type + " (" + t0.text + ")",
						t0.line, t0.position);	
			}
			
			int size = toks.Count;
			
			Tag tag = null;
			
			if(size==1) {
				tag = new Tag(t0.text);
			} else {
				int valuesStartIndex = 1;
				
				Token t1 = toks[1];
				
				if(t1.type==Type.COLON) {
					if(size==2 || toks[2].type!=Type.IDENTIFIER)
						ParseException("Colon (:) encountered in unexpected " +
								"location.", t1.line, t1.position);
					
					Token t2 = toks[2];
					tag = new Tag(t0.text,t2.text);
					
					valuesStartIndex = 3;
				} else {
					tag = new Tag(t0.text);
				}
					
				// read values
				int i =AddTagValues(tag, toks, valuesStartIndex);
				
				// read attributes
				if(i<size)
					AddTagAttributes(tag, toks, i);
			}
	
			return tag;
		}

        /// <summary>
        /// Add the values
        /// </summary>
        /// <param name="tag">The tag to populate</param>
        /// <param name="toks">The token list to use</param>
        /// <param name="tpos">Where to start</param>
        /// <returns>The position at the end of the value list</returns>
		private int AddTagValues(Tag tag, List<Token> toks, int tpos) {
			
			int size=toks.Count, i=tpos;
			
			for(;i<size;i++) {
				Token t = toks[i];
				if(t.literal) {
					
					// if a DATE token is followed by a TIME token combine them
					if(t.type==Type.DATE && (i+1)<size &&
							toks[i+1].type==Type.TIME) {
	
						SDLDateTime dt = (SDLDateTime)t.GetObjectForLiteral();
						TimeSpanWithZone tswz = (TimeSpanWithZone)
							toks[i+1].GetObjectForLiteral();
						
						if(tswz.Days!=0) {
                            tag.AddValue(dt);
                            tag.AddValue(new TimeSpan(tswz.Days, tswz.Hours,
                                tswz.Minutes, tswz.Seconds, tswz.Milliseconds));

                            if(tswz.TimeZone!=null)
							    ParseException("TimeSpan cannot have a " +
                                    "timezone", t.line, t.position);
                        } else {
						    tag.AddValue(Combine(dt,tswz));
						}

						i++;
					} else {
						object v = t.GetObjectForLiteral();
						if(v is TimeSpanWithZone) {
							TimeSpanWithZone tswz = (TimeSpanWithZone)v;
							
							if(tswz.TimeZone!=null)
								ExpectingButGot("TIME SPAN",
									"TIME (component of date/time)", t.line,
										t.position);
							
							tag.AddValue(new TimeSpan(
								tswz.Days, tswz.Hours,
								tswz.Minutes, tswz.Seconds,
								tswz.Milliseconds
							));
						} else {
							tag.AddValue(v);
						}
					}
				} else if(t.type==Type.IDENTIFIER) {
					break;
				} else {
					ExpectingButGot("LITERAL or IDENTIFIER", t.type, t.line,
							t.position);
				}
			}	
			
			return i;
		}
		
        /// <summary>
        /// Add the attributes
        /// </summary>
        /// <param name="tag">The tag to populate</param>
        /// <param name="toks">The token list to use</param>
        /// <param name="tpos">Where to start</param>
		private void AddTagAttributes(Tag tag, List<Token> toks, int tpos) {
			
			int i=tpos, size=toks.Count;
			
			while(i<size) {
				Token t = toks[i];
				if(t.type!=Type.IDENTIFIER)
					ExpectingButGot("IDENTIFIER", t.type, t.line, t.position);
				string nameOrNamespace = t.text;
				
				if(i==size-1)
					ExpectingButGot("\":\" or \"=\" \"LITERAL\"",
                        "END OF LINE.", t.line, t.position);
				
				t = toks[++i];
				if(t.type==Type.COLON) {
					if(i==size-1)
						ExpectingButGot("IDENTIFIER", "END OF LINE", t.line,
								t.position);
					
					t = toks[++i];
					if(t.type!=Type.IDENTIFIER)
						ExpectingButGot("IDENTIFIER", t.type, t.line,
								t.position);
					string name = t.text;
					
					if(i==size-1)
						ExpectingButGot("\"=\"", "END OF LINE", t.line,
								t.position);
					t = toks[++i];
					if(t.type!=Type.EQUALS)
						ExpectingButGot("\"=\"", t.type, t.line,
								t.position);
					
					if(i==size-1)
						ExpectingButGot("LITERAL", "END OF LINE", t.line,
								t.position);
					t = toks[++i];
					if(!t.literal)
						ExpectingButGot("LITERAL", t.type, t.line, t.position);
					
					if(t.type==Type.DATE && (i+1)<size &&
							toks[i+1].type==Type.TIME) {
						
						SDLDateTime dt = (SDLDateTime)t.GetObjectForLiteral();
						TimeSpanWithZone tswz = (TimeSpanWithZone)
							toks[i+1].GetObjectForLiteral();

						if(tswz.Days!=0)
							ExpectingButGot("TIME (component of date/time) " +
								"in attribute value", "TIME SPAN", t.line,
								t.position);
						tag[nameOrNamespace, name]=Combine(dt,tswz);	
						
						i++;
					} else {
						object v = t.GetObjectForLiteral();
						if(v is TimeSpanWithZone) {
							TimeSpanWithZone tswz = (TimeSpanWithZone)v;
							
							if(tswz.TimeZone!=null)
								ExpectingButGot("TIME SPAN",
									"TIME (component of date/time)", t.line,
										t.position);
							
							TimeSpan ts = new TimeSpan(
								tswz.Days, tswz.Hours,
								tswz.Minutes, tswz.Seconds,
								tswz.Milliseconds);
							
							tag[nameOrNamespace, name]=ts;			
						} else {
							tag[nameOrNamespace, name]=v;
						}
					}
				} else if(t.type==Type.EQUALS){
					if(i==size-1)
						ExpectingButGot("LITERAL", "END OF LINE", t.line,
								t.position);	
					t = toks[++i];
					if(!t.literal)
						ExpectingButGot("LITERAL", t.type, t.line, t.position);	
					
					
					if(t.type==Type.DATE && (i+1)<size &&
							toks[i+1].type==Type.TIME) {
					
						SDLDateTime dc = (SDLDateTime)t.GetObjectForLiteral();
						TimeSpanWithZone tswz = (TimeSpanWithZone)
							toks[i+1].GetObjectForLiteral();
						
						if(tswz.Days!=0)
							ExpectingButGot("TIME (component of date/time) " +
								"in attribute value", "TIME SPAN", t.line,
								t.position);
						tag[nameOrNamespace]=Combine(dc,tswz);	
						
						i++;
					} else {
						object v = t.GetObjectForLiteral();
						if(v is TimeSpanWithZone) {
							TimeSpanWithZone tswz = (TimeSpanWithZone)v;
							
							if(tswz.TimeZone!=null)
								ExpectingButGot("TIME SPAN",
									"TIME (component of date/time)", t.line,
										t.position);
							
							TimeSpan ts = new TimeSpan(
								tswz.Days, tswz.Hours,
								tswz.Minutes, tswz.Seconds,
								tswz.Milliseconds);
							
							tag[nameOrNamespace]=ts;							
						} else {
							tag[nameOrNamespace]=v;	
						}			
					}
				} else {
					ExpectingButGot("\":\" or \"=\"", t.type, t.line,
							t.position);	
				}
				
				i++;
			}
		}

        /// <summary>
        /// Get a line as tokens.  This method handles line continuations both
        /// within and outside String literals.
        /// </summary>
        /// <returns>An SDL line as a list of Tokens</returns>
        /// <exception cref="SDLParserException">If the line is malformed
        /// </exception>
        /// <exception cref="System.IO.IOException">If an IO error occurs while
        /// reading the line</exception>
        List<Token> GetLineTokens() {
			line = ReadLine();
			if(line==null)
				return null;
			toks = new List<Token>();
			lineLength = line.Length; 
			sb = null;
			tokenStart=0;	
			
			for(;pos<lineLength; pos++) {
				char c=line[pos];
	
				if(sb!=null) {
					toks.Add(new Token(this, sb.ToString(), lineNumber,
                        tokenStart));
					sb=null;
				}
				
				if(c=='"') {	
					// handle "" style strings including line continuations
					HandleDoubleQuoteString();
				} else if(c=='\'') {	
					// handle character literals				
					HandleCharacterLiteral();
				} else if("{}=:".IndexOf(c)!=-1) {
					// handle punctuation
					toks.Add(new Token(this, ""+c, lineNumber, pos));
					sb=null;
				} else if(c=='#') {	
					// handle hash comments
					break;
				} else if(c=='/') {	
					// handle /**/ and // style comments
					
					if((pos+1)<lineLength &&
							line[pos+1]=='/')
						break;
					else
						HandleSlashComment();	
				} else if(c=='`') {	
					// handle multiline `` style strings				
					HandleBackQuoteString();
				} else if(c=='[') {	
					// handle binary literals
					
					HandleBinaryLiteral();
				} else if(c==' ' || c=='\t') {
					// eat whitespace
					while((pos+1)<lineLength &&
							" \t".IndexOf(line[pos+1])!=-1)
						pos++;	
				} else if(c=='\\') {
					// line continuations (outside a string literal)
					
					// backslash line continuation outside of a String literal
					// can only occur at the end of a line
					HandleLineContinuation();
				} else if("0123456789-.".IndexOf(c)!=-1) {
					if(c=='-' && (pos+1)<lineLength &&
							line[pos+1]=='-')
						break;
					
					// handle numbers, dates, and time spans
					HandleNumberDateOrTimeSpan();
				} else if(Char.IsLetter(c) || c=='_') {
					// handle identifiers
					HandleIdentifier();
				} else {
					ParseException("Unexpected character \"" + c + "\".)",
							lineNumber, pos);
				}
			}
			
			if(sb!=null) {
				toks.Add(new Token(this, sb.ToString(), lineNumber, tokenStart));
			}
			
			// if toks are empty, try another line
			// this seems a bit dangerous, but eventually we should get a null line
			// which serves as a termination condition for the recursion
			while(toks!=null && toks.Count==0)
				toks=GetLineTokens();
			
			return toks;
		}

        private void AddEscapedCharInString(char c) {
            switch (c) {
                case '\\':
                    sb.Append(c);
                    return;
                case '"':
                    sb.Append(c);
                    return;
                case 'n':
                    sb.Append('\n');
                    return;
                case 'r':
                    sb.Append('\r');
                    return;
                case 't':
                    sb.Append('\t');
                    return;
            }

            ParseException("Ellegal escape character in " +
                            "string literal: \"" + c + "\".",
                            lineNumber, pos);
        }

        private void HandleDoubleQuoteString() {

            bool escaped = false;
            startEscapedQuoteLine = false;

            sb = new StringBuilder("\"");
            pos++;

            for (; pos < lineLength; pos++) {
                char c = line[pos];

                if (" \t".IndexOf(c) != -1 && startEscapedQuoteLine)
                    continue;
                else
                    startEscapedQuoteLine = false;

                if (escaped) {
                    AddEscapedCharInString(c);
                    escaped = false;
                } else if (c == '\\') {
                    // check for String broken across lines
                    if (pos == lineLength - 1 || (pos + 1 < lineLength &&
                            " \t".IndexOf(line[pos + 1]) != -1)) {
                        HandleEscapedDoubleQuotedString();
                    } else {
                        escaped = true;
                    }
                } else {
                    sb.Append(c);
                    if (c == '"') {
                        toks.Add(new Token(this, sb.ToString(), lineNumber,
                                tokenStart));
                        sb = null;
                        return;
                    }
                }
            }

            if (sb != null) {
                string tokString = sb.ToString();
                if (tokString.Length > 0 && tokString[0] == '"' &&
                        tokString[tokString.Length - 1] != '"') {
                    ParseException("String literal \"" + tokString +
                            "\" not terminated by end quote.", lineNumber,
                            line.Length);
                } else if (tokString.Length == 1 && tokString[0] == '"') {
                    ParseException("Orphan quote (unterminated " +
                            "string)", lineNumber, line.Length);
                }
            }
        }

        private void HandleEscapedDoubleQuotedString() {

            if (pos == lineLength - 1) {
                line = ReadLine();
                if (line == null) {
                    ParseException("Escape at end of file.", lineNumber,
                            pos);
                }

                lineLength = line.Length;
                pos = -1;
                startEscapedQuoteLine = true;
            } else {
                // consume whitespace
                int j = pos + 1;
                while (j < lineLength && " \t".IndexOf(line[j]) != -1)
                    j++;

                if (j == lineLength) {
                    line = ReadLine();
                    if (line == null) {
                        ParseException("Escape at end of file.",
                                lineNumber, pos);
                    }

                    lineLength = line.Length;
                    pos = -1;
                    startEscapedQuoteLine = true;

                } else {
                    ParseException("Malformed string literal - " +
                            "escape followed by whitespace " +
                            "followed by non-whitespace.", lineNumber,
                            pos);
                }
            }
        }

        private void HandleCharacterLiteral() {
            if (pos == lineLength - 1)
                ParseException("Got ' at end of line", lineNumber, pos);

            pos++;

            char c2 = line[pos];
            if (c2 == '\\') {

                if (pos == lineLength - 1)
                    ParseException("Got '\\ at end of line", lineNumber,
                            pos);
                pos++;
                char c3 = line[pos];

                if (pos == lineLength - 1)
                    ParseException("Got '\\" + c3 + " at end of " +
                            "line", lineNumber, pos);

                if (c3 == '\\') {
                    toks.Add(new Token(this, "'\\'", lineNumber,
                            pos));
                } else if (c3 == '\'') {
                    toks.Add(new Token(this, "'''", lineNumber,
                            pos));
                } else if (c3 == 'n') {
                    toks.Add(new Token(this, "'\n'", lineNumber,
                            pos));
                } else if (c3 == 'r') {
                    toks.Add(new Token(this, "'\r'", lineNumber,
                            pos));
                } else if (c3 == 't') {
                    toks.Add(new Token(this, "'\t'", lineNumber,
                            pos));
                } else {
                    ParseException("Illegal escape character " +
                            line[pos], lineNumber, pos);
                }

                pos++;
                if (line[pos] != '\'')
                    ExpectingButGot("single quote (')", "\"" + line[pos] + "\"",
                        lineNumber, pos);
            } else {
                toks.Add(new Token(this, "'" + c2 + "'", lineNumber,
                        pos));
                if (pos == lineLength - 1)
                    ParseException("Got '" + c2 + " at end of " +
                            "line", lineNumber, pos);
                pos++;
                if (line[pos] != '\'')
                    ExpectingButGot("quote (')", "\"" + line[pos] +
                            "\"", lineNumber, pos);
            }
        }

	    private void HandleSlashComment() {
    		
		    if(pos==lineLength-1)
			    ParseException("Got slash (/) at end of line.", lineNumber,
					    pos);
    		
		    if(line[pos+1]=='*') {
    	
			    int endIndex = line.IndexOf("*/", pos+1);
			    if(endIndex!=-1) {
				    // handle comment on same line
				    pos=endIndex+1;
			    } else {
				    // handle multiline comments
				    while(true) {
					    line = ReadRawLine();							
					    if(line==null) {
						    ParseException("/* comment not terminated.",
							    lineNumber, -2);
					    }
    					
					    endIndex = line.IndexOf("*/");
    	
					    if(endIndex!=-1) {
						    lineLength = line.Length; 
						    pos=endIndex+1;
						    break;
					    }
				    }	
			    }
		    } else if(line[pos+1]=='/') {
			    ParseException("Got slash (/) in unexpected location.", 
					    lineNumber, pos);
		    }
	    }
    	
	    private void HandleBackQuoteString() {
    		
		    int endIndex = line.IndexOf("`", pos+1);
    		
		    if(endIndex!=-1) {
			    // handle end quote on same line
                toks.Add(new Token(this, line.Substring(pos, (endIndex + 1)-pos),
					    lineNumber, pos));
			    sb=null;					
    			
			    pos=endIndex;
		    } else {
    			
			    sb = new StringBuilder(line.Substring(pos) +
					    "\n");
			    int start = pos;
			    // handle multiline quotes
			    while(true) {
				    line = ReadRawLine();
				    if(line==null) {
					    ParseException("` quote not terminated.",
						    lineNumber, -2);
				    }
    				
				    endIndex = line.IndexOf('`');
				    if(endIndex!=-1) {
					    sb.Append(line.Substring(0, endIndex+1));
    					
					    line=line.Trim();
					    lineLength = line.Length; 
    					
					    pos=endIndex;
					    break;
				    } else {
					    sb.Append(line + "\n");
				    }
			    }	
    			
			    toks.Add(new Token(this, sb.ToString(), lineNumber, start));
			    sb=null;
		    }
	    }

	    private void HandleBinaryLiteral() {
    		
		    int endIndex = line.IndexOf(']', pos+1);
    		
		    if(endIndex!=-1) {
			    // handle end quote on same line
			    toks.Add(new Token(this, line.Substring(pos, (endIndex+1)-pos),
					    lineNumber, pos));
			    sb=null;					
    			
			    pos=endIndex;
		    } else {					
			    sb = new StringBuilder(line.Substring(pos) +
					    "\n");
			    int start = pos;
			    // handle multiline quotes
			    while(true) {
				    line = ReadRawLine();
				    if(line==null) {
					    ParseException("[base64] binary literal not " +
							    "terminated.", lineNumber, -2);
				    }
    				
				    endIndex = line.IndexOf(']');
				    if(endIndex!=-1) {
					    sb.Append(line.Substring(0, endIndex+1));
    					
					    line=line.Trim();
					    lineLength = line.Length; 
    					
					    pos=endIndex;
					    break;
				    } else {
					    sb.Append(line + "\n");
				    }
			    }	
    			
			    toks.Add(new Token(this, sb.ToString(), lineNumber, start));
			    sb=null;
		    }
	    }
    	
	    // handle a line continuation (not inside a string)
	    private void HandleLineContinuation() {
    		
		    if(line.Substring(pos+1).Trim().Length!=0) {
			    ParseException("Line continuation (\\) before end of line",
					    lineNumber, pos);
		    } else {
			    line = ReadLine();
			    if(line==null) {
				    ParseException("Line continuation at end of file.",
					    lineNumber, pos);
			    }
    				
			    lineLength = line.Length; 
			    pos=-1;				
		    }
	    }
    	
	    private void HandleNumberDateOrTimeSpan() {
		    tokenStart=pos;
		    sb=new StringBuilder();
		    char c;
    	
		    for(;pos<lineLength; ++pos) {
			    c=line[pos];
    			
			    if("0123456789.-+:abcdefghijklmnopqrstuvwxyz".IndexOf(
					    Char.ToLower(c))!=-1) {
				    sb.Append(c);
			    } else if(c=='/' && !((pos+1)<lineLength && line[pos+1]=='*')) {
				    sb.Append(c);
			    } else {
				    pos--;
				    break;
			    }
		    }
    		
		    toks.Add(new Token(this, sb.ToString(), lineNumber, tokenStart));
		    sb=null;
	    }
    	
	    private void HandleIdentifier() {
		    tokenStart=pos;
		    sb=new StringBuilder();
		    char c;
    		
		    for(;pos<lineLength; ++pos) {
			    c=line[pos];
    			
			    if(Char.IsLetterOrDigit(c) || c=='-' || c=='_' || c=='.') {
				    sb.Append(c);
			    } else {
				    pos--;
				    break;
			    }
		    }
    		
		    toks.Add(new Token(this, sb.ToString(), lineNumber, tokenStart));
		    sb=null;
	    }

        /// <summary>
        /// Skips comment lines and blank lines.
        /// </summary>
        /// <returns>The next line or null at the end of the file.</returns>
        /// <exception cref="System.IO.IOException">If an IO problem occurs
        /// while reading the line.</exception>
	    private string ReadLine() {
		    string line = reader.ReadLine();
		    pos=0;

		    if(line==null)
			    return null;
		    lineNumber++;
    		
		    string tLine = line.Trim();	

		    while(tLine.StartsWith("#") || tLine.Length==0) {
			    line = reader.ReadLine();
			    if(line==null)
				    return null;
    			
			    lineNumber++;
			    tLine = line.Trim();	
		    }
    		
		    return line;
	    }

        /// <summary>
        /// Reads a "raw" line including lines with comments and blank lines
        /// </summary>
        /// <returns>The next line or null at the end of the file.</returns>
        /// <exception cref="System.IO.IOException">If an IO problem occurs
        /// while reading the line.</exception>
	    string ReadRawLine() {
            string line = reader.ReadLine();
		    pos=0;

		    if(line==null)
			    return null;
		    lineNumber++;
    		
		    return line;
	    }	

        /// <summary>
        /// Combine a SDLDateTime (date only) with a TimeSpanWithZone to create
        /// a date-time
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="tswz"></param>
        /// <returns></returns>
        private static SDLDateTime Combine(SDLDateTime dt,
            TimeSpanWithZone tswz) {

            return new SDLDateTime(dt.Year, dt.Month, dt.Day, tswz.Hours,
                tswz.Minutes, tswz.Seconds, tswz.Milliseconds, tswz.TimeZone);
        }

        internal static int ReverseIfPositive(int val) {
            if (val < 1)
                return val;
            return 0 - val;
        }

        /// <summary>
        /// Close the reader and throw a SDLParseException
        /// </summary>
        /// <param name="description">What happened</param>
        internal void ParseException(string description, int line,
            int position) {

		    try {
			    reader.Close();
		    } catch { /* no recourse */ }
    		
		    // We add one because editors typically start with line 1 and
            // position 1 rather than 0...
		    throw new SDLParseException(description, line+1, position+1);			
	    }

        /// <summary>
        /// Close the reader and throw a SDLParseException using the format
        /// "Was expecting X but got Y"
        /// </summary>
	    internal void ExpectingButGot(string expecting, object got,
            int line, int position) {
    		
		    ParseException("Was expecting " + expecting + " but got " +
		        got, line, position);	
	    }

        ////////////////////////////////////////////////////////////////////////////
        // Parsers for types
        ////////////////////////////////////////////////////////////////////////////
        internal static string ParseString(string literal) {
            if (literal[0] != literal[literal.Length - 1])
                throw new FormatException("Malformed string <" +
                        literal + ">.  Strings must start and end with \" or `");

            return literal.Substring(1, literal.Length - 2);
        }

        internal static Char ParseCharacter(string literal) {
            if (literal[0] != '\'' ||
                    literal[literal.Length - 1] != '\'')
                throw new FormatException("Malformed character <" +
                        literal + ">.  Character literals must start and end " +
                        "with single quotes.");

            return literal[1];
        }

        internal static object ParseNumber(string literal) {

            int textLength = literal.Length;
            bool hasDot = false;
            int tailStart = 0;

            for (int i = 0; i < textLength; i++) {
                char c = literal[i];
                if ("-0123456789".IndexOf(c) == -1) {
                    if (c == '.') {
                        if (hasDot) {
                            new FormatException(
                                "Encountered second decimal point.");
                        } else if (i == textLength - 1) {
                            new FormatException(
                                    "Encountered decimal point at the " +
                                    "end of the number.");
                        } else {
                            hasDot = true;
                        }
                    } else {
                        tailStart = i;
                        break;
                    }
                } else {
                    tailStart = i + 1;
                }
            }

            string number = literal.Substring(0, tailStart);
            string tail = literal.Substring(tailStart);


            if (tail.Length == 0) {
                if (hasDot)
                    return Convert.ToDouble(number);
                else
                    return Convert.ToInt32(number);
            }

            if (tail.ToUpper().Equals("BD")) {
                return Convert.ToDecimal(number);
            } else if (tail.ToUpper().Equals("L")) {
                if (hasDot)
                    new FormatException("Long literal with decimal " +
                            "point");
                return Convert.ToInt64(number);
            } else if (tail.ToUpper().Equals("F")) {
                return Convert.ToSingle(number);
            } else if (tail.ToUpper().Equals("D")) {
                return Convert.ToDouble(number);
            }

            throw new FormatException("Could not parse number <" + literal +
                    ">");
        }

        internal static SDLDateTime ParseDateTime(string literal) {
            int spaceIndex = literal.IndexOf(' ');
            if (spaceIndex == -1) {
                return ParseDate(literal);
            } else {
                SDLDateTime dt = ParseDate(literal.Substring(0, spaceIndex));
                string timestring = literal.Substring(spaceIndex + 1);

                int dashIndex = timestring.IndexOf('-');
                string tzstring = null;
                if (dashIndex != -1)
                {
                    tzstring = timestring.Substring(dashIndex + 1);
                    timestring = timestring.Substring(0, dashIndex);
                }

                string[] timeComps = timestring.Split(':');
                if (timeComps.Length < 2 || timeComps.Length > 3)
                    throw new FormatException("Malformed time " +
                            "component in date/time literal.  Must use " +
                            "hh:mm(:ss)(.xxx)");

                int hour = 0;
                int minute = 0;
                int second = 0;
                int millisecond = 0;

                // TODO - parse the time string, concatenate and return date/time
                try {
                    hour = Convert.ToInt32(timeComps[0]);
                    minute = Convert.ToInt32(timeComps[1]);

                    if (timeComps.Length == 3) {
                        string last = timeComps[2];

                        int dotIndex = last.IndexOf('.');
                        if (dotIndex == -1) {
                            second = Convert.ToInt32(last);
                        }
                        else {
                            second = Convert.ToInt32(last.Substring(0, dotIndex));

                            string millis = last.Substring(dotIndex + 1);
                            if (millis.Length == 1)
                                millis = millis + "00";
                            else if (millis.Length == 2)
                                millis = millis + "0";
                            millisecond = Convert.ToInt32(millis);
                        }
                    }
                } catch (FormatException fe) {
                    throw new FormatException("Number format exception " +
                            "in time portion of date/time literal \"" +
                                fe.Message + "\"");
                }

                return new SDLDateTime(dt.Year, dt.Month, dt.Day, hour, minute,
                    second, millisecond, tzstring);
            }
        }

        internal static SDLDateTime ParseDate(string literal) {
            string[] comps = literal.Split('/');
            if (comps.Length != 3)
                throw new FormatException("Malformed Date <" +
                    literal + ">");

            try {
                return new SDLDateTime(
                        Convert.ToInt32(comps[0]),
                        Convert.ToInt32(comps[1]),
                        Convert.ToInt32(comps[2])
                );
            } catch (FormatException fe) {
                throw new FormatException("Number format exception in " +
                        "date literal \"" + fe.Message + "\"");

            }
        }

        internal static byte[] ParseBinary(string literal) {
            string stripped = literal.Substring(1, literal.Length - 2);
            StringBuilder sb = new StringBuilder();
            int btLength = stripped.Length;
            for (int i = 0; i < btLength; i++) {
                char c = stripped[i];
                if ("\n\r\t ".IndexOf(c) == -1)
                    sb.Append(c);
            }

            return Convert.FromBase64String(sb.ToString());
        }
        
        internal static TimeSpan ParseTimeSpan(string literal) {

            int days = 0; // optional 
            int hours = 0; // mandatory
            int minutes = 0; // mandatory
            int seconds = 0; // mandatory
            int milliseconds = 0; // optional

            string[] segments = literal.Split(':');

            if (segments.Length < 3 || segments.Length > 4)
                throw new FormatException("Malformed time span <" +
                        literal + ">.  Time spans must use the format " +
                        "(d:)hh:mm:ss(.xxx) Note: if the day component is " +
                        "included it must be suffixed with lower case \"d\"");

            try {
                if (segments.Length == 4) {
                    string daystring = segments[0];
                    if (!daystring.EndsWith("d"))
                        new FormatException("The day component of a time " +
                            "span must end with a lower case d");

                    days = Convert.ToInt32(daystring.Substring(0,
                            daystring.Length - 1));

                    hours = Convert.ToInt32(segments[1]);
                    minutes = Convert.ToInt32(segments[2]);

                    if (segments.Length == 4)
                    {
                        string last = segments[3];
                        int dotIndex = last.IndexOf('.');

                        if (dotIndex == -1)
                        {
                            seconds = Convert.ToInt32(last);
                        }
                        else
                        {
                            seconds =
                                Convert.ToInt32(
                                        last.Substring(0, dotIndex));

                            string millis = last.Substring(dotIndex + 1);
                            if (millis.Length == 1)
                                millis = millis + "00";
                            else if (millis.Length == 2)
                                millis = millis + "0";

                            milliseconds =
                                Convert.ToInt32(millis);
                        }
                    }

                    if (days < 0)
                    {
                        hours = ReverseIfPositive(hours);
                        minutes = ReverseIfPositive(minutes);
                        seconds = ReverseIfPositive(seconds);
                        milliseconds = ReverseIfPositive(milliseconds);
                    }
                }
                else
                {
                    hours = Convert.ToInt32(segments[0]);
                    minutes = Convert.ToInt32(segments[1]);

                    string last = segments[2];
                    int dotIndex = last.IndexOf(".");

                    if (dotIndex == -1)
                    {
                        seconds = Convert.ToInt32(last);
                    }
                    else
                    {
                        seconds = Convert.ToInt32(last.Substring(0, dotIndex));

                        string millis = last.Substring(dotIndex + 1);
                        if (millis.Length == 1)
                            millis = millis + "00";
                        else if (millis.Length == 2)
                            millis = millis + "0";
                        milliseconds = Convert.ToInt32(millis);
                    }

                    if (hours < 0)
                    {
                        minutes = ReverseIfPositive(minutes);
                        seconds = ReverseIfPositive(seconds);
                        milliseconds = ReverseIfPositive(milliseconds);
                    }
                }
            } catch (FormatException fe) {
                throw new FormatException("Number format in time span " +
                        "exception: \"" + fe.Message + "\" for literal <" +
                        literal + ">");
            }

            return new TimeSpan(days, hours, minutes, seconds, milliseconds);
        }
    }

    /// <summary>
    /// An SDL token
    /// </summary>
	internal class Token {
        internal Parser parser;
        internal Type type;
        internal string text;
        internal int line;
        internal int position;
        internal int size;
        internal object obj;

        internal bool punctuation;
        internal bool literal;
		
		internal Token(Parser parser, string text, int line, int position) {
            this.parser = parser;
			this.text=text;

			this.line=line;
			this.position=position;
			size=text.Length;
			
			try {
				if(text.StartsWith("\"") || text.StartsWith("`")) {
					type=Type.STRING;
					obj=Parser.ParseString(text);
				} else if(text.StartsWith("'")) {
					type=Type.CHARACTER;
					obj=text[1];
				} else if(text.Equals("null")) {
					type=Type.NULL;
					obj=null;
				} else if(text.Equals("true") || text.Equals("on")) {
					type=Type.BOOLEAN;
					obj=true;
				} else if(text.Equals("false") || text.Equals("off")) {
					type=Type.BOOLEAN;
					obj=false;
				} else if(text.StartsWith("[")) {
					type=Type.BINARY;
					obj=Parser.ParseBinary(text);
				} else if(text[0]!='/' && text.IndexOf('/')!=-1 &&
						text.IndexOf(':')==-1) {
					type=Type.DATE;
                    obj = Parser.ParseDateTime(text);
				} else if(text[0]!=':' && text.IndexOf(':')!=-1) {
					type=Type.TIME;
                    obj = ParseTimeSpanWithZone(text);
				} else if("01234567890-.".IndexOf(text[0])!=-1) {
					type=Type.NUMBER;
					obj=Parser.ParseNumber(text);				
				} else if (text[0]=='{') {
                    type=Type.START_BLOCK;
                } else if (text[0]=='}') {
                    type=Type.END_BLOCK;
                } else if (text[0]=='=') {
                    type=Type.EQUALS;
                } else if (text[0]==':') {
                    type=Type.COLON;	
				} else {
                    type = Type.IDENTIFIER;
                }
			} catch(FormatException fe) {
				throw new SDLParseException(fe.Message, line,
						position);
			}

			punctuation = type==Type.COLON || type==Type.EQUALS ||
				type==Type.START_BLOCK || type==Type.END_BLOCK;
			literal =  type!=Type.IDENTIFIER && !punctuation;
		}
		
		internal Object GetObjectForLiteral() {
			return obj;
		}
		
		public override String ToString() {
			return type + " " + text + " pos:" + position;
		}

        // This special parse method is used only by the Token class for
        // tokens which are ambiguously either a TimeSpan or the time component
        // of a date/time type
        internal TimeSpanWithZone ParseTimeSpanWithZone(string text) {

            int day = 0; // optional (not allowed for date_time)
            int hour = 0; // mandatory
            int minute = 0; // mandatory
            int second = 0; // optional for date_time, mandatory for time span
            int millisecond = 0; // optional

            string timeZone = null;
            string dateText = text;

            int dashIndex = dateText.IndexOf('-', 1);
            if (dashIndex != -1) {
                timeZone = dateText.Substring(dashIndex + 1);
                dateText = text.Substring(0, dashIndex);
            }

            string[] segments = dateText.Split(':');

            // we know this is the time component of a date time type
            // because the time zone has been set
            if (timeZone != null) {
                if (segments.Length < 2 || segments.Length > 3)
                    parser.ParseException("date/time format exception.  Must " +
                            "use hh:mm(:ss)(.xxx)(-z)", line, position);
            } else {
                if (segments.Length < 2 || segments.Length > 4)
                    parser.ParseException("Time format exception.  For time " +
                            "spans use (d:)hh:mm:ss(.xxx) and for the " +
                            "time component of a date/time type use " +
                            "hh:mm(:ss)(.xxx)(-z)  If you use the day " +
                            "component of a time span make sure to " +
                            "prefix it with a lower case d", line,
                            position);
            }

            try {
                if (segments.Length == 4) {
                    string dayString = segments[0];
                    if (!dayString.EndsWith("d"))
                        parser.ParseException("The day component of a time " +
                            "span must end with a lower case d", line,
                            position);

                    day = Convert.ToInt32(dayString.Substring(0,
                            dayString.Length - 1));

                    hour = Convert.ToInt32(segments[1]);
                    minute = Convert.ToInt32(segments[2]);

                    if (segments.Length == 4) {
                        String last = segments[3];
                        int dotIndex = last.IndexOf('.');

                        if (dotIndex == -1) {
                            second = Convert.ToInt32(last);
                        } else {
                            second =
                                Convert.ToInt32(
                                        last.Substring(0, dotIndex));

                            String millis = last.Substring(dotIndex + 1);
                            if (millis.Length == 1)
                                millis = millis + "00";
                            else if (millis.Length == 2)
                                millis = millis + "0";

                            millisecond =
                                Convert.ToInt32(millis);
                        }
                    }

                    if (day < 0) {
                        hour = Parser.ReverseIfPositive(hour);
                        minute = Parser.ReverseIfPositive(minute);
                        second = Parser.ReverseIfPositive(second);
                        millisecond = Parser.ReverseIfPositive(millisecond);
                    }
                } else {
                    hour = Convert.ToInt32(segments[0]);
                    minute = Convert.ToInt32(segments[1]);

                    if (segments.Length == 3) {
                        String last = segments[2];
                        int dotIndex = last.IndexOf(".");

                        if (dotIndex == -1) {
                            second = Convert.ToInt32(last);
                        } else {
                            second = Convert.ToInt32(last.Substring(0, dotIndex));

                            String millis = last.Substring(dotIndex + 1);
                            if (millis.Length == 1)
                                millis = millis + "00";
                            else if (millis.Length == 2)
                                millis = millis + "0";

                            millisecond = Convert.ToInt32(millis);
                        }
                    }

                    if (hour < 0) {
                        minute = Parser.ReverseIfPositive(minute);
                        second = Parser.ReverseIfPositive(second);
                        millisecond = Parser.ReverseIfPositive(millisecond);
                    }
                }
            } catch (FormatException fe) {
                parser.ParseException("Time format: " + fe.Message, line,
                        position);
            }

            TimeSpanWithZone tswz = new TimeSpanWithZone(
                    day, hour, minute, second, millisecond, timeZone
            );

            return tswz;
        }
    }

    // An intermediate object used to store a time span or the time
    // component of a date/time instance.  The types are disambiguated at
    // a later stage.
    internal class TimeSpanWithZone {

        private string timeZone;
        private int days, hours, minutes, seconds, milliseconds;

        internal TimeSpanWithZone(int days, int hours, int minutes,
                int seconds, int milliseconds, string timeZone) {

            this.days = days;
            this.hours = hours;
            this.minutes = minutes;
            this.seconds = seconds;
            this.milliseconds = milliseconds;
            this.timeZone = timeZone;
        }

        internal int Days { get { return days; } }
        internal int Hours { get { return hours; } }
        internal int Minutes { get { return minutes; } }
        internal int Seconds { get { return seconds; } }
        internal int Milliseconds { get { return milliseconds; } }

        internal string TimeZone { get { return timeZone; } }
    }
}
