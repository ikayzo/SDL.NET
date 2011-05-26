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
using System.Net;

namespace Ikayzo.SDL {
    
    /// <summary>
    /// SDL documents are composed of tags.  A tag is composed of a name and
    /// optionally a value list, attribute list, and child list.  Tags and
    /// attributes may specify namespaces.
    /// </summary>
    /// <author>Daniel Leuck from Ikayzo</author>
    public class Tag {
	    private string sdlNamespace;
	    private string name;

        private List<object> values;
	    private Dictionary<string,string> attributeToNamespace;
        private SortedDictionary<string, object> attributes;
	    private List<Tag> children;
	    
	    /// Remove and replace with read only views once .NET adds proper
	    /// read only generic lists and maps
        private List<object> valuesSnapshot;
        private bool valuesDirty;
        private SortedDictionary<string, object> attributesSnapshot;
        private bool attributesDirty;
        private Dictionary<string, string> attributeToNamespaceSnapshot;
	    private List<Tag> childrenSnapshot;	    
		private bool childrenDirty;
		
        /// <summary>
        /// Create an empty tag with a name and no namespace
        /// </summary>
        /// <param name="name">The name of this tag</param>
        /// <exception cref="System.ArgumentException">If the <c>name</c> is not 
        /// a valid SDL identifier</exception>
        public Tag(string name) : this("", name) {
        }

        /// <summary>
        /// Create a tag with the given namespace and name
        /// </summary>
        /// <param name="sdlNamespace">The namespace for this tag.  null will
        /// be coerced to the empty string("")</param>
        /// <param name="name">The name for this tag</param>
        /// <exception cref="System.ArgumentException">If <c>name</c> is not 
        /// a valid SDL identifier or <c>sdlNamespace</c> is not empty and is
        /// not a valid SDL identifier</exception>
        public Tag(string sdlNamespace, string name) {
            SDLNamespace = sdlNamespace;
            Name = name;

            values = new List<object>();
            attributeToNamespace = new Dictionary<string, string>();
            attributes = new SortedDictionary<string, object>();
            children = new List<Tag>();
            
	 	    /// Remove and replace with read only views once .NET adds proper
		    /// read only generic lists and maps
            valuesSnapshot = new List<object>();
            attributeToNamespaceSnapshot = new Dictionary<string, string>();
            attributesSnapshot = new SortedDictionary<string, object>();
            childrenSnapshot = new List<Tag>();		          
        }

		////////////////////////////////////////////////////////////////////////
		// Properties
		////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The tag's name.  Must be a valid SDL identifier.
        /// </summary>
        public string Name {
            set { name=SDLUtil.ValidateIdentifier(value); }
            get { return name; }
        }

        /// <summary>
        /// The tag's namespace.  Must be a valid SDL identifier or empty.
        /// </summary>
        public string SDLNamespace {
            set {
                if (value == null)
                    value = "";

                if (value.Length != 0)
                    SDLUtil.ValidateIdentifier(value);

                sdlNamespace = value;
            }
            get { return sdlNamespace; }
        }
        
        /// <summary>
        /// A convenience property that sets and gets the first value in the
        /// value list.
        /// </summary>
        /// <exception cref="">If the value is not coercible to an SDL
        /// type</exception>
        public object Value {
			set {
                value = SDLUtil.CoerceOrFail(value);

				if(values.Count==0)
					AddValue(value);
				else
					values[0]=value;
					
				valuesDirty=true;
			}
			get {
                if (values.Count == 0)
					return null;
				return values[0];				
			}
        }

        /// <summary>
        /// A property for the tag's children.  When read, this property returns
        /// a copy of the children. 
        /// </summary>
        public IList<Tag> Children {
            set {
                childrenDirty = true;
                children = new List<Tag>(value);
            }

            get {
                if(childrenDirty) {
                    childrenSnapshot = new List<Tag>(children);
                }

                return childrenSnapshot;
            }
        }

        /// <summary>
        /// A property for the tag's values.  When read, this property returns
        /// a copy of the values. 
        /// </summary>
        public IList<object> Values {
            set {
                valuesDirty = true;

                // we need to use this instead of a copy constructor because
                // validation is required for value (performed by AddValue(obj))
                values.Clear();
                foreach (object v in value)
                    AddValue(v);
            }

            get {
                if (valuesDirty) {
                    valuesSnapshot = new List<object>(values);
                }

                return valuesSnapshot;
            }
        }

        // HERE! TODO: From here down - validate all values using CoerceOrFail

        /// <summary>
        /// A property for the tag's attributes.  When read, this property
        /// returns a copy of the attributes. 
        /// </summary>
        public IDictionary<string, object> Attributes {
            set {
                attributesDirty = true;

                // we need to use this instead of a copy constructor because
                // validation is required for the key and the value
                // (performed by the indexer)
                attributes.Clear();
                foreach(string key in value.Keys) {
                    this[key] = value[key];
                }
            }

            get {
                if (attributesDirty) {
                    attributesSnapshot =
                        new SortedDictionary<string, object>(attributes);
                }

                return attributesSnapshot;
            }
        }

        /// <summary>
        /// <para>A property for the mapping of this tag's attributes to their
        /// respective namespaces.  Attributes with no namespace are mapped to
        /// an empty string ("")</para>
        /// 
        ///<para>When read, this property returns a copy of the mapping.  It is 
        /// not write-through.</para>
        /// 
        /// <para>This property is read only</para>
        /// </summary>
        public IDictionary<string, string> AttributeToNamespace
        {
            get
            {
                if (attributesDirty) {
                    attributeToNamespaceSnapshot =
                        new Dictionary<string, string>(attributeToNamespace);
                }

                return attributeToNamespaceSnapshot;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // Indexers
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// An indexer for the tag's attributes.  This indexer sets the
        /// attribute's namespace to an empty string ("")
        /// </summary>
        /// <param name="key">The key for this attribute</param>
        /// <returns>The value associated with the <c>key</c></returns>
        public object this[string key] {
            get {
                return attributes[key];
            }

            set {
                this["", key] = value;
            }
        }

        /// <summary>
        /// Set the <c>key</c> to the given value and sets the namespace.
        /// </summary>
        /// <param name="sdlNamespace">The namespace for this attribute</param>
        /// <param name="key">The key for this attribute</param>
        public object this[string sdlNamespace, string key]
        {
            set {
                attributesDirty = true;
                attributes[SDLUtil.ValidateIdentifier(key)] =
                    SDLUtil.CoerceOrFail(value);

                if (sdlNamespace == null)
                    sdlNamespace = "";
                if (sdlNamespace.Length != 0)
                    SDLUtil.ValidateIdentifier(sdlNamespace);

                attributeToNamespace[key] = sdlNamespace;
            }
        }

        /// <summary>
        /// An indexer for the tag's values
        /// </summary>
        /// <param name="index">The <c>index</c> to get or set</param>
        /// <returns>The value at the given <c>index</c></returns>
        public object this[int index] {
            get {
                return values[index];
            }

            set {
                valuesDirty = true;
                values[index] = SDLUtil.CoerceOrFail(value);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        
        /// <summary>
        /// Add a child to this tag
        /// </summary>
        /// <param name="child">The child to add</param>
        public void AddChild(Tag child) {
            childrenDirty = true;
            children.Add(child);
        }

        /// <summary>
        ///  Remove a child from this tag
        /// </summary>
        /// <param name="child">The child to remove</param>
        /// <returns>true if the child is removed</returns>
        public bool RemoveChild(Tag child) {
            childrenDirty = true;
            return children.Remove(child);
        }

        /// <summary>
        /// Add a value to this tag
        /// </summary>
        /// <param name="value">The value to add</param>
        public void AddValue(object value) {
            valuesDirty = true;
            values.Add(SDLUtil.CoerceOrFail(value));
        }

        /// <summary>
        ///  Remove a child from this tag
        /// </summary>
        /// <param name="child">The child to remove</param>
        /// <returns>true if the child is removed</returns>
        public bool RemoveValue(object value) {
            valuesDirty = true;
            return values.Remove(value);
        }




        /// <summary>
        /// Get all the children of this tag optionally recursing through all
        /// descendents.
        /// </summary>
        /// <param name="recursively">If true, recurse through all descendents
        /// </param>
        /// <returns>A snapshot of the children</returns>
        public IList<Tag> GetChildren(bool recursively) {
		    if(!recursively)
			    return this.Children;
    		
		    List<Tag> kids = new List<Tag>();
		    foreach (Tag t in this.Children) {
			    kids.Add(t);
    			
			    if(recursively)
				    kids.AddRange(t.GetChildren(true));
		    }

            return kids;
	    }

        /// <summary>
        /// Get the first child with the given name.  The search is not
        /// recursive.
        /// </summary>
        /// <param name="childName">The name of the child Tag</param>
        /// <returns>The first child tag having the given name or null if no 
        /// such child exists</returns>
        public Tag GetChild(string childName) {
            return GetChild(childName, false);
        }

        /// <summary>
        /// Get the first child with the given name, optionally using a
        /// recursive search.
        /// </summary>
        /// <param name="childName">The name of the child Tag</param>
        /// <param name="recursive">If the search should be recursive</param>
        /// <returns>The first child tag having the given name or null if no
        /// such child exists</returns>
        public Tag GetChild(string childName, bool recursive) {
		    foreach(Tag t in children) {
			    if(t.Name.Equals(childName))
				    return t;
    			
			    if(recursive) {
				    Tag rc = t.GetChild(childName, true);
				    if(rc!=null)
					    return rc;
			    }
		    }
    		
		    return null;
	    }

        /// <summary>
        /// Get all children with the given name.  The search is not recursive.
        /// </summary>
        /// <param name="childName">The name of the children to fetch</param>
        /// <returns>All the child tags having the given name</returns>
        public IList<Tag> GetChildren(string childName) {
            return GetChildren(childName, false);
        }

        /// <summary>
        /// Get all the children with the given name (optionally searching
        /// descendants recursively)
        /// </summary>
        /// <param name="childName">The name of the children to fetch</param>
        /// <param name="recursive">If true search all descendents</param>
        /// <returns>All the child tags having the given name</returns>
        public IList<Tag> GetChildren(string childName, bool recursive) {
            List<Tag> kids = new List<Tag>();
		    foreach(Tag t in children) {
			    if(t.Name.Equals(childName))
				    kids.Add(t);

			    if(recursive)
				    kids.AddRange(t.GetChildren(childName, true));
		    }
    		
		    return kids;
	    }

        /// <summary>
        /// Get the values for all children with the given name.  If the child
        /// has more than one value, all the values will be added as a list.  If
        /// the child has no value, null will be added.  The search is not
        /// recursive.
        /// </summary>
        /// <param name="name">The name of the children from which values are
        /// retrieved</param>
        /// <returns>A list of values (or lists of values)</returns>
        public IList<object> GetChildrenValues(String name) {
            List<object> results = new List<object>();
            IList<Tag> children = GetChildren(name);

            foreach(Tag t in children) {
                IList<object> values = t.Values;
                if (values.Count == 0)
                    results.Add(null);
                else if (values.Count == 1)
                    results.Add(values[0]);
                else
                    results.Add(values);
            }

            return results;
        }

        /// <summary>
        /// Get all children in the given namespace.  The search is not
        /// recursive.
        /// </summary>
        /// <param name="?">The namespace to search</param>
        /// <returns>All the child tags in the given namespace</returns>
	    public IList<Tag> GetChildrenForNamespace(string sdlNamespace) {
		    return GetChildrenForNamespace(sdlNamespace, false);
	    }

        /// <summary>
        /// Get all the children in the given namespace (optionally searching
        /// descendants recursively)
        /// </summary>
        /// <param name="sdlNamespace">The namespace of the children to
        /// fetch</param>
        /// <param name="recursive">If true search all descendents</param>
        /// <returns>All the child tags in the given namespace</returns>
	    public IList<Tag> GetChildrenForNamespace(string sdlNamespace,
			    bool recursive) {
    		
		    List<Tag> kids = new List<Tag>();
		    foreach (Tag t in children) {
			    if(t.SDLNamespace.Equals(sdlNamespace))
				    kids.Add(t);
    			
			    if(recursive)
                    kids.AddRange(t.GetChildrenForNamespace(sdlNamespace,
                        true));
		    }
    		
		    return kids;
	    }

        /// <summary>
        /// Returns all attributes in the given namespace.
        /// </summary>
        /// <param name="sdlNamespace">The namespace to search</param>
        /// <returns>All attributes in the given namespace</returns>
        public IDictionary<string, object> GetAttributesForNamespace(
                string sdlNamespace) {

            SortedDictionary<string, object> atts =
                new SortedDictionary<string, object>();

            foreach (string key in attributeToNamespace.Keys) {
                if (attributeToNamespace[key].Equals(sdlNamespace))
                    atts[key] = attributes[key];
            }

            return atts;
        }

        // Methods for reading in SDL input ////////////////////////////////////

        /// <summary>
        /// Add all the tags specified in the file at the given URL to this Tag.
        /// </summary>
        /// <param name="url">url A UTF8 encoded .sdl file</param>
        /// <returns>This tag after adding all the children read from the reader
        /// </returns>
        /// <exception cref="System.IO.IOException">If there is an IO problem
        /// while reading the source</exception>
        /// <exception cref="SDLParseException">If the SDL input is malformed
        /// </exception>
	    public Tag ReadURL(String url) {
            WebRequest request = WebRequest.Create(url);
            Stream input = request.GetResponse().GetResponseStream();

            return Read(new StreamReader(input, System.Text.Encoding.UTF8));
	    }
	
        /// <summary>
        /// Add all the tags specified in the given file to this Tag.
        /// </summary>
        /// <param name="file">A UTF8 encoded .sdl file</param>
        /// <returns>This tag after adding all the children read from the reader
        /// </returns>
        /// <exception cref="System.IO.IOException">If there is an IO problem
        /// while reading the source</exception>
        /// <exception cref="SDLParseException">If the SDL input is malformed
        /// </exception>
        public Tag ReadFile(String file) {
            return Read(new StreamReader(file, System.Text.Encoding.UTF8));
        }

        /// <summary>
        /// Add all the tags specified in the given String to this Tag.
        /// </summary>
        /// <param name="text">An SDL string</param>
        /// <returns>This tag after adding all the children read from the reader
        /// </returns>
        /// <exception cref="SDLParseException">If the SDL input is malformed
        /// </exception>
        public Tag ReadString(String text) {
	        return Read(new StringReader(text));
	    }

        /// <summary>
        /// Add all the tags specified in the given Reader to this Tag.
        /// </summary>
        /// <param name="reader">A reader containing SDL source</param>
        /// <returns>This tag after adding all the children read from the reader
        /// </returns>
        /// <exception cref="System.IO.IOException">If there is an IO problem
        /// while reading the source</exception>
        /// <exception cref="SDLParseException">If the SDL input is malformed
        /// </exception>
        public Tag Read(TextReader reader) {
		    IList<Tag> tags = new Parser(reader).Parse();
		    foreach(Tag t in tags)
			    AddChild(t);
		    return this;
	    }

        // Write methods ///////////////////////////////////////////////////////

        /// <summary>
        /// Write this tag out to the given file.
        /// </summary>
        /// <param name="file">The file path to which we will write the children 
        /// of this tag.</param>
        /// <exception cref="IOException">If there is an IO problem during the
        /// write operation</exception>
        public void WriteFile(string file) {
            WriteFile(file, false);
        }

        /// <summary>
        /// Write this tag out to the given file (optionally clipping the root.)
        /// </summary>
        /// <param name="file">The file path to which we will write this tag
        /// </param>
        /// <param name="includeRoot">If true this tag will be included in the
        /// file as the root element, if false only the children will be written
        /// </param>
        /// <exception cref="IOException">If there is an IO problem during the
        /// write operation</exception>
        public void WriteFile(string file, bool includeRoot) {
            Write(new StreamWriter(file, false, System.Text.Encoding.UTF8),
                includeRoot);

        }

        /// <summary>
        /// Write this tag out to the given writer (optionally clipping the
        /// root.)
        /// </summary>
        /// <param name="writer">The writer to which we will write this tag
        /// </param>
        /// <param name="includeRoot">If true this tag will be written out as
        /// the root element, if false only the children will be written</param>
        /// <exception cref="IOException">If there is an IO problem during the
        /// write operation</exception>
        public void Write(TextWriter writer, bool includeRoot) {

            string newLine = "\r\n";

            if (includeRoot) {
                writer.Write(ToString());
            } else {
                for(int i=0; i<children.Count; i++) {
                    writer.Write(children[i].ToString());
                    if (i < children.Count - 1)
                        writer.Write(newLine);
                }
            }

            writer.Close();
        }

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Produces a full SDL "dump" of this tag.  The output is valid SDL.
        /// </summary>
        /// <returns>SDL code describing this tag</returns>
        public override string ToString() {
            return ToString("");
        }

        /// <summary>
        /// Produces a full SDL "dump" of this tag using the given prefix before
        /// each line.
        /// </summary>
        /// <param name="linePrefix">Text to be prefixed to each line</param>
        /// <returns>SDL code describing this tag</returns>
        string ToString(String linePrefix) {
            StringBuilder sb = new StringBuilder();
            sb.Append(linePrefix);

            bool skipValueSpace = false;
            if(sdlNamespace.Length==0 && name.Equals("content")) {
                skipValueSpace = true;
            } else {
                if (sdlNamespace.Length != 0)
                    sb.Append(sdlNamespace).Append(':');
                sb.Append(name);
            }

		    // output values
		    if(values.Count!=0) {

                if (skipValueSpace == true)
                    skipValueSpace = false;
                else
                    sb.Append(" ");
    			
                int size = values.Count;
                for(int i=0;i<size;i++) {
                    sb.Append(SDLUtil.Format(values[i]));
                    if(i<size-1)
                        sb.Append(" ");
                }
		    }
    		
		    // output attributes
		    if(attributes.Count!=0) {	
                foreach(string key in attributes.Keys) {
                    sb.Append(" ");

                    string attNamespace = AttributeToNamespace[key];
                    if(!attNamespace.Equals(""))
                        sb.Append(attNamespace + ":");
                    sb.Append(key + "=");
                    sb.Append(SDLUtil.Format(attributes[key]));
                }
		    }

            // output children
		    if(children.Count!=0) {
                sb.Append(" {\r\n");
			    foreach(Tag t in children) {
                    sb.Append(t.ToString(linePrefix + "    ") + "\r\n");
			    }
                sb.Append(linePrefix + "}");
		    }            

            return sb.ToString();
        }

        /// <summary>
        /// Returns a string containing an XML representation of this tag.
        /// Values will be represented using _val0, _val1, etc.
        /// </summary>
        /// <returns>An XML String describing this Tag</returns>
	    public string ToXMLString() {
		    return ToXMLString("");
	    }

        /// <summary>
        /// Returns a string containing an XML representation of this tag.
        ///  Values will be represented using _val0, _val1, etc.
        /// </summary>
        /// <param name="linePrefix">A prefix to insert before every line.
        /// </param>
        /// <returns>An XML String describing this Tag</returns>
	    String ToXMLString(string linePrefix) {
		    String newLine = "\r\n";
    		
		    if(linePrefix==null)
			    linePrefix="";
    		
		    StringBuilder builder = new StringBuilder(linePrefix + "<");
		    if(!sdlNamespace.Equals(""))
			    builder.Append(sdlNamespace + ":");
		    builder.Append(name);
    		
		    // output values
		    if(values.Count!=0) {
                int i=0;
                foreach (object val in values) {
                    builder.Append(" ");
                    builder.Append("_val" + i + "=\"" + SDLUtil.Format(val,
						    false) + "\"");

                    i++;
                }
		    }
    		
		    // output attributes
		    if(attributes.Count!=0) {
			    foreach (string key in attributes.Keys) {
                    builder.Append(" ");
				    String attNamespace = attributeToNamespace[key];
    			
				    if(!attNamespace.Equals(""))
					    builder.Append(attNamespace + ":");
				    builder.Append(key + "=");
				    builder.Append("\"" + SDLUtil.Format(attributes[key], false)
                        + "\"");				
			    }
		    }		

		    if(children.Count!=0) {
			    builder.Append(">" + newLine);
			    foreach(Tag t in children) {
				    builder.Append(t.ToXMLString(linePrefix + "    ") + newLine);
			    }
    			
			    builder.Append(linePrefix + "</");
			    if(!sdlNamespace.Equals(""))
                    builder.Append(sdlNamespace + ":");
			    builder.Append(name + ">");
		    } else {
			    builder.Append("/>");
		    }

		    return builder.ToString();
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
        /// <returns>A unique hashcode for this tag</returns>
        public override int GetHashCode() {
            return ToString().GetHashCode();
        }
    }
}
