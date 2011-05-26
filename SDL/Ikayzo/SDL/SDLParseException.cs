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
    /// An exception describing a problem with an SDL document's structure.
    /// </summary>
    public class SDLParseException : FormatException {

	    private int line;
	    private int position;

        /// <summary>
        /// Note: Line and positioning numbering start with 1 rather than 0 to
        /// be consistent with most editors.
        /// </summary>
        /// <param name="description">A description of the problem.</param>
        /// <param name="line">The line on which the error occured or -1 for
        /// unknown</param>
        /// <param name="position">The position (within the line) where the
        /// error occured or -1 for unknown</param>
	    public SDLParseException(string description, int line, int position)
            : base(description + " Line " + ((line==-1)
                ? "unknown" :
                (""+line)) + ", Position " + ((position==-1)
                ? "unknown" :
                (""+position))) {

		    this.line = line;
		    this.position = position;
	    }

        /// <summary>
        /// Get the line on which the error occured
        /// </summary>
        public int Line {
            get { return line; }
        }

        /// <summary>
        /// Get the character position within the line where the error occured
        /// </summary>
        public int Position {
            get { return position; }
        }
    	    	
        /// <returns>The message</returns>
	    public override string ToString() {
            return this.Message;
	    }
    }
}
