using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    public class ProfitUICode : UICode
    {
        // ProfitUI has 5 QRBs
        public ProfitUICode() : base( 5 ) { }

        // This is where the work of writing out the UI code file happens. Profit's file is XML, but it's easier
        // to just write text than to make the XML writer handle some of the oddities of EQ2 UI XML.
        public override void saveUICode( QRBStorage qrb )
        {
            string fn = Path.Combine( UIPath, UIFilename );
            Debug.WriteLine( "QRB saving UI code to " + fn );

            using ( StreamWriter writer = new StreamWriter( fn ) )
            {
                // Output header
                writer.WriteLine( "<?xml version=\"1.0\" encoding=\"utf-8\"?>" );
                writer.WriteLine( "<Page ButtonOpacityLevel=\"0.500\" Name=\"ProfitUI_QuickRaidButtons\">" );
                writer.WriteLine( "<Text DynamicData=\"/GameData.Self.ActualLevel\" Name=\"ActualLevel\">80</Text>" );
                writer.WriteLine( "" );

                // We're going to output one Page element per class. In each block we'll output one Data element per button,
                // with most of the work for each button being generating the Macro attribute string. There we start with
                // the level-0 literal commands and output them (so things like cancel_spellcast work correctly), then
                // look at the non-zero-level actual spells in reverse order. The first spell we assign directly to the
                // macro variable, spells after that we generate a COND testing the spell level's against the character's
                // current level. As long as the character's level is less than the previous spell's level, we replace the
                // macro variable with the current spell. The bHaveCommands and bHaveSpells flags let us handle the formatting
                // differences between the very first line in the macro and subsequent lines. We need line breaks between lines
                // within the macro, but not at the very start. Last we output the useability command to fire off the spell only
                // if we actually had any spells (ie. not when all the button does is literal commands).
                foreach ( KeyValuePair<string, QRBButtonAssignment> kv in qrb.ButtonAssignments.OrderBy( x => x.Key ) )
                {
                    QRBButtonAssignment ba = kv.Value;
                    QRBSpellClass sc = null;
                    if ( qrb.SpellClasses.ContainsKey( ba.Classname ) )
                        sc = qrb.SpellClasses[ba.Classname];
                    if ( sc == null )
                        continue;
                    sc.resequenceSpellLines();

                    // Output class Page
                    Debug.WriteLine( "QRB class block for " + ba.Classname );
                    writer.WriteLine( "<Page Name=\"" + ba.Classname + "\">" );

                    for ( int i = 1; i <= ButtonCount; i++ )
                    {
                        string sln = ba.getButton( i );
                        QRBSpellLine sl = null;
                        if ( !String.IsNullOrEmpty( sln ) )
                            sl = sc.findSpellLine( sln );
                        if ( sl != null )
                        {
                            sl.resequenceSpells();


                            Debug.WriteLine( "QRB button " + i.ToString() );
                            writer.Write( "<Data Name=\"Button" + i.ToString() + "\" Macro=\"" );
                            // Output level 0 literal commands first
                            bool haveCommands = false;
                            foreach ( QRBSpell s in sl.Spells )
                            {
                                if ( s.Level == 0 )
                                {
                                    if ( haveCommands )
                                        writer.WriteLine();
                                    string o = doUISubstitutions( s.Text, true ).TrimStart( '/' ); // Remove leading slashes from commands if they included them
                                    writer.Write( o );
                                    haveCommands = true;
                                }
                            }
                            // Then output spells in reverse order by level
                            List<QRBSpell> r = new List<QRBSpell>( sl.Spells );
                            r.Reverse();
                            bool haveSpells = false;
                            int iPrevLevel = 200; // Greater than max level for the forseeable future
                            foreach ( QRBSpell s in r )
                            {
                                if ( s.Level > 0 )
                                {
                                    string o = "";
                                    string quot = "'";
                                    // If we have a spell ID, use that number. Otherwise, use the spell text
                                    if ( s.SpellID > 0 )
                                        o = quot + s.SpellID.ToString() + quot;
                                    else
                                        o = quot + doUISubstitutions( s.Text, true ) + quot;
                                    if ( !haveSpells )
                                    {
                                        if ( haveCommands )
                                            writer.WriteLine();
                                        // First spell is the highest level and is unconditional
                                        writer.WriteLine( "SpellForLevel=" + o );
                                    }
                                    else
                                    {
                                        // After the first spell we output a conditional checking if our level is less
                                        // than the previous level and replacing the spell name if it is
                                        writer.WriteLine( "COND=(Parent.Parent.Parent.Parent.Custom.ProfitUI_QuickRaidButtons.ActualLevel.Text < " + iPrevLevel.ToString() + ")" );
                                        writer.WriteLine( "SpellForLevel=COND ? " + o + " : SpellForLevel" );
                                    }
                                    iPrevLevel = s.Level;
                                    haveSpells = true;
                                }
                            }
                            if ( haveSpells )
                                writer.Write( "useabilityonplayer Parent.Target SpellForLevel" ); // TODO Figure out how to use group/raid slot numbers instead of character names
                            if ( !String.IsNullOrEmpty( sl.Tooltip ) )
                            {
                                writer.WriteLine( "\" Tooltip=\"" + doUISubstitutions( sl.Tooltip, false ) + "\"/>" );
                            }
                        }
                        else
                        {
                            Debug.WriteLine( "QRB empty button " + i.ToString() );
                            writer.WriteLine( "<Data Name=\"Button" + i.ToString() + "\" Macro=\"NONE\" Tooltip=\"NO MACRO CONFIGURED\"/>" );
                        }
                    }

                    // Close class Page
                    writer.WriteLine( "</Page>" );
                    writer.WriteLine( "" );
                }

                // Output trailer
                writer.WriteLine( "</Page>" );
            }
        }
    }
}
