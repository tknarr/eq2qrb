using System;
using System.Diagnostics;
using System.Text;

//  ProfitUI Quick Raid Buttons tool
//  Copyright (C) 2013 Todd T Knarr <tknarr@silverglass.org> <tknarr@cox.net> <todd.knarr@gmail.com>

//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.

//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace QuickRaidButtons
{
    // Base class for UI code writers.
    // Right now it's just ProfitUI, but I decided to separate the UI code file handling from the QRB storage to make
    // it easier to support others in the future.
    public abstract class UICode
    {
        // Common properties for all UIs
        // We the the UIPath and UIFilename from the application settings
        public string UIPath { get; set; }
        public string UIFilename { get; set; }
        // Button count will be passed to the constructor, probably by the default constructor for a UI-specific subclass
        public int ButtonCount { get; protected set; }

        public UICode( int bc )
        {
            UIPath = "";
            UIFilename = "";
            ButtonCount = bc;

            if ( !String.IsNullOrEmpty( Properties.Settings.Default.UIFileFolder ) )
                UIPath = Properties.Settings.Default.UIFileFolder;
            else
                UIPath = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
            UIFilename = Properties.Settings.Default.UIFilename;
            Debug.WriteLine( "UI file path: " + UIPath );
            Debug.WriteLine( "UI file name: " + UIFilename );
        }

        // Overridden method to actually generate the UI file for a given UI
        public abstract void saveUICode( QRBStorage qrb );

        // EQ2 UI XML substitutions. We allow %T or %t to be used to indicate the target, and
        // %I or %i for the implied target. We also need to encode quote marks and apostrophes
        // in attribute strings so they don't get interpreted as string delimiters.
        protected string doUISubstitutions( string s, bool do_pct )
        {
            // Trim leading and trailing whitespace and do percent-variable substitution
            // %T means target, %I means target's target (implied target)
            string ours = s.Trim();
            if ( do_pct )
            {
                ours = ours.Replace( "%T", "Parent.Target" );
                ours = ours.Replace( "%t", "Parent.Target" );
                ours = ours.Replace( "%I", "Parent.Target.Target" );
                ours = ours.Replace( "%i", "Parent.Target.Target" );
            }

            // Encode special characters that need it
            // Currently this is just single- and double-quote characters
            StringBuilder sb = new StringBuilder();
            foreach ( char c in ours )
            {
                if ( c == '\"' )
                    sb.Append( "&quot;" );
                else if ( c == '\'' )
                    sb.Append( "&apos;" );
                else
                    sb.Append( c );
            }
            return sb.ToString();
        }

    }
}
