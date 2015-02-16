using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

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
    // Implementes the edit spell lines window. Opened via the Edit Spell Lines button on the button assignments window.
    // It only edits the spell lines in the permanent storage, it doesn't touch the button assignments.
    public partial class SpellLines : Form
    {
        private int spellNameCount = 16; // Recalculated in the constructor based on the number of spell name textboxes
        private string HelpFile = "";

        // Current class and spell line names
        private string currentClassName = "";
        private string currentSpellLine = "";

        // Reference to our permanent storage, and a collection for our current state
        private QRBStorage qrb = null;
        private Dictionary<string, QRBSpellClass> spellClasses = null;
        private Dictionary<long, QRBSpellID> spellIDs = null;
        private Dictionary<string, QRBSpellID> spellIDsName = null;

        // Save a reference to permanent storage, and make a copy of the spell classes for our current state.
        // We also recalculate the number of spells in a spell line based on the textboxes in our group box. That
        // accomodates changes to the number of textboxes. When done we populate the UI elements from our current
        // state.
        public SpellLines( QRBStorage qrbs, string className )
        {
            InitializeComponent();

            // Conditional check. In development the VS project has the help file one level down in the Help Files folder. In the installer and
            // zipfile versions, the help file only exists in the directory the executable's in. So in debug builds we check for the project folder
            // first, then the executable folder. In release builds we assume we aren't in the VS project environment and only check the executable
            // folder.
#if DEBUG
            HelpFile = Path.GetDirectoryName( Application.ExecutablePath ) + Path.DirectorySeparatorChar + "Help Files" + Path.DirectorySeparatorChar + "QuickRaidButtons.chm";
            if ( !File.Exists( HelpFile ) )
                HelpFile = Path.GetDirectoryName( Application.ExecutablePath ) + Path.DirectorySeparatorChar + "QuickRaidButtons.chm";
#else
            HelpFile = Path.GetDirectoryName( Application.ExecutablePath ) + Path.DirectorySeparatorChar + "QuickRaidButtons.chm";
#endif

            qrb = qrbs;
            spellClasses = qrb.CloneSpellClasses();
            spellIDs = qrb.CloneSpellIDs();
            spellIDsName = qrb.CloneSpellIDsName();
            currentClassName = className;
            currentSpellLine = "";

            // Scan the controls array for our spell textboxes, and determine the maximum number of them
            int maxtb = 0;
            Control[] gbc = Controls.Find( "spells_groupBox", true );
            foreach ( Control ctrl in gbc[0].Controls )
            {
                if ( ( ctrl.Name.Length > 13 ) && ctrl.Name.StartsWith( "spell_textBox" ) )
                {
                    string num = ctrl.Name.Remove( 0, 13 );
                    int n = Int32.Parse( num ); // If this throws an exception, someone's messed up the source code
                    if ( n > maxtb )
                        maxtb = n;
                }
            }
            if ( maxtb > 0 )
                spellNameCount = maxtb;

            populateSpellLineFields();
        }

        // Update the spell ID dictionary, either by adding a new spell ID or by updating an existing one.
        // Duplicate spell names with different IDs are cleaned up during the update process.
        private void updateSpellID( long spell_id, string spell_name )
        {
            QRBSpellID new_sid = new QRBSpellID( spell_id, spell_name );

            if ( spellIDs.ContainsKey( spell_id ) )
                spellIDs[spell_id].Name = spell_name;
            else
                spellIDs.Add( spell_id, new_sid );

            if ( spellIDsName.ContainsKey( spell_name ) )
                spellIDsName[spell_name].ID = spell_id;
            else
                spellIDsName.Add( spell_name, new_sid );
        }

        // Reads back the information from the UI fields and updates our current state. Does nothing if we don't have
        // a current class or spell line, that should never happen. We get our current QRBSpellClass and QRBSpellLine objects
        // from the current state based on our class and spell line name. We'll always have those because when we populated
        // the UI fields we filled those drop-down lists in and selected items in them and they don't have empty elements.
        // Note that we don't update the current class and spell line names from the form, that's handled directly in the
        // event routines when those drop-downs change value. That's because this routine is called when those drop-downs change,
        // and we want to update the current state for the class and spell line we're leaving, not the one the user's changing
        // to. At the end we sort the spells in the line into order, so things end up in the proper order even if the user
        // didn't enter them that way. They'll likely have to do things like add level 0 commands at the end and may enter
        // spells high-to-low when we want them low-to-high, and we need to clean that up.
        private void updateSpellLineData()
        {
            Debug.WriteLine( "Updating spell line data" );
            Debug.WriteLine( "Class: " + currentClassName );
            Debug.WriteLine( "Spell line: " + currentSpellLine );

            if ( String.IsNullOrEmpty( currentClassName ) || String.IsNullOrEmpty( currentSpellLine ) )
                return;

            // Locate our spell class object containing our spell lines, or create a new one
            // if one didn't already exist
            QRBSpellClass spellClass = null;
            if ( spellClasses.ContainsKey( currentClassName ) )
                spellClass = spellClasses[currentClassName];
            if ( spellClass == null )
            {
                Debug.WriteLine( "USL: created new spell class object " + currentClassName );
                spellClass = new QRBSpellClass( currentClassName );
                spellClasses[currentClassName] = spellClass;
            }

            // Locate the spell line we're editing. Create a new one if it doesn't
            // exist for this class
            QRBSpellLine spellLine = spellClass.findSpellLine( currentSpellLine );
            if ( spellLine == null )
            {
                Debug.WriteLine( "USL: created new spell line object " + currentSpellLine );
                spellLine = new QRBSpellLine( currentSpellLine, currentSpellLine );
                spellClass.SpellLines.Add( spellLine );
            }

            // Update the internal spell line object from the form fields
            spellLine.Tooltip = tooltip.Text;

            // Get spell line data from form fields
            spellLine.Spells.Clear();
            for ( int i = 1; i <= spellNameCount; i++ )
            {
                Control[] a_lvl = Controls.Find( "spell_numericUpDown" + i.ToString(), true );
                Control[] a_txt = Controls.Find( "spell_textBox" + i.ToString(), true );
                Control[] a_sid = Controls.Find( "spell_idBox" + i.ToString(), true );

                int lvl = 0;
                string txt = "";
                long id = 0;
                if ( a_txt.Length > 0 )
                {
                    TextBox c_txt = a_txt[0] as TextBox;
                    if ( c_txt != null )
                        txt = c_txt.Text;
                    if ( !String.IsNullOrEmpty( txt ) )
                    {
                        if ( a_lvl.Length > 0 )
                        {
                            NumericUpDown c_lvl = a_lvl[0] as NumericUpDown;
                            if ( c_lvl != null )
                            {
                                String v = c_lvl.Value.ToString();
                                if ( !String.IsNullOrEmpty( v ) )
                                    int.TryParse( v, out lvl );
                                else
                                    lvl = 0;
                            }
                            TextBox c_sid = a_sid[0] as TextBox;
                            if ( c_sid != null )
                            {
                                String v = c_sid.Text;
                                if ( !String.IsNullOrEmpty( v ) )
                                    long.TryParse( v, out id );
                                else
                                    id = 0;
                            }
                        }
                        bool update_spell_id = false;
                        if ( lvl > 0 )
                        {
                            if ( id == 0 && !String.IsNullOrEmpty( txt ) )
                            {
                                if ( spellIDsName.ContainsKey( txt ) )
                                    id = spellIDsName[txt].ID;
                                else
                                    update_spell_id = true;
                            }
                            else if ( id != 0 && String.IsNullOrEmpty( txt ) )
                            {
                                if ( spellIDs.ContainsKey( id ) )
                                    txt = spellIDs[id].Name;
                                else
                                    update_spell_id = true;
                            }
                            else if ( id != 0 && !String.IsNullOrEmpty( txt ) )
                            {
                                if ( !spellIDs.ContainsKey( id ) || !spellIDsName.ContainsKey( txt ) )
                                    update_spell_id = true;
                            }
                        }
                        Debug.WriteLine( "USL: updating spell: " + lvl.ToString() + ": " + txt + ( ( id != 0 ) ? ( " (#" + id.ToString() + ")" ) : "" ) );
                        spellLine.addSpell( lvl, txt, id );
                        if ( update_spell_id && ( lvl > 0 ) && ( id != 0 ) && !String.IsNullOrEmpty( txt ) )
                            updateSpellID( id, txt );
                    }
                }
            }
            // Sort the spells in the spell line into the correct order
            Debug.WriteLine( "USL: resequencing spells" );
            spellLine.resequenceSpells();
        }

        // Take our current class and spell line from the current state and fill in the form fields based on the
        // contents. We always have a class, and if we don't have a spell line we start with the first line for the
        // class. We do accomodate the case of a new class with no spell lines by using a blank form with no items
        // in the spell line drop-down list.
        private void populateSpellLineFields()
        {
            Debug.WriteLine( "Populating spell line fields" );
            Debug.WriteLine( "Class: " + currentClassName );
            Debug.WriteLine( "Spell line: " + currentSpellLine );

            // Locate our spell class object containing our spell lines, or create a new one
            // if one didn't already exist
            QRBSpellClass spellClass = null;
            if ( spellClasses.ContainsKey( currentClassName ) )
                spellClass = spellClasses[currentClassName];
            if ( spellClass == null )
            {
                Debug.WriteLine( "PSL: created new spell class object " + currentClassName );
                spellClass = new QRBSpellClass( currentClassName );
                spellClasses[currentClassName] = spellClass;
            }

            // If we don't have a current spell line name, use the name of the first spell line
            // for the class.
            if ( string.IsNullOrEmpty( currentSpellLine ) && ( spellClass.SpellLines.Count > 0 ) && ( spellClass.SpellLines[0] != null ) )
                currentSpellLine = spellClass.SpellLines[0].Linename;

            // Reset the spell line form fields
            className.Text = currentClassName;
            spellLineName.ResetText();
            spellLineName.Items.Clear();
            tooltip.ResetText();
            Control[] gbc = Controls.Find( "spells_groupBox", true );
            foreach ( Control c in gbc[0].Controls )
            {
                if ( c.Name.StartsWith( "spell_textBox" ) )
                {
                    TextBox tb = c as TextBox;
                    tb.ResetText();
                }
                else if ( c.Name.StartsWith( "spell_numericUpDown" ) )
                {
                    NumericUpDown n = c as NumericUpDown;
                    n.ResetText();
                    n.Text = "0";
                    n.Value = 0;
                }
                else if ( c.Name.StartsWith( "spell_idBox" ) )
                {
                    TextBox tb = c as TextBox;
                    tb.ResetText();
                }
            }

            // If we don't have a spell line name, just use the blank form
            if ( string.IsNullOrEmpty( currentSpellLine ) )
                return;

            // Find our spell line object. If we can't find one, create a new empty one using
            // the current spell line name
            QRBSpellLine spellLine = spellClass.findSpellLine( currentSpellLine );
            if ( spellLine == null )
            {
                Debug.WriteLine( "PSL: created new spell line object " + currentSpellLine );
                spellLine = new QRBSpellLine( currentSpellLine, currentSpellLine );
                spellClass.SpellLines.Add( spellLine );
            }

            // Populate the controls based on the spell line object selected
            foreach ( QRBSpellLine sl in spellClass.SpellLines )
                spellLineName.Items.Add( sl.Linename );
            spellLineName.Text = spellLine.Linename;
            tooltip.Text = spellLine.Tooltip;
            int i = 1;
            foreach ( QRBSpell sp in spellLine.Spells )
            {
                string spinner_name = "spell_numericUpDown" + i.ToString();
                string tb_name = "spell_textBox" + i.ToString();
                string id_name = "spell_idBox" + i.ToString();

                Control[] spinners = Controls.Find( spinner_name, true );
                Control[] tbs = Controls.Find( tb_name, true );
                Control[] ids = Controls.Find( id_name, true );

                Debug.WriteLine( "PSL: updating spell " + i.ToString() + ": " + sp.Level.ToString() + ": " + sp.Text + ( ( sp.SpellID != 0 ) ? ( " (#" + sp.SpellID.ToString() + ")" ) : "" ) );
                if ( spinners.Length > 0 )
                {
                    NumericUpDown ud = spinners[0] as NumericUpDown;
                    ud.Text = sp.Level.ToString();
                    ud.Value = sp.Level;
                }
                if ( tbs.Length > 0 )
                    tbs[0].Text = sp.Text;
                if ( ids.Length > 0 )
                {
                    if ( sp.SpellID != 0 )
                        ids[0].Text = sp.SpellID.ToString();
                    else
                        ids[0].Text = "";
                }

                i++;
                if ( i > spellNameCount )
                    break;
            }
        }

        // Done and Cancel return control to the caller. Done updates the current state from the UI form fields and
        // saves the current state into the permanent storage, Cancel abandons without any changes.
        private void doneButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Done button clicked" );
            updateSpellLineData();
            qrb.SpellClasses = spellClasses;
            qrb.SpellIDs = spellIDs;
            qrb.SpellIDsName = spellIDsName;
        }
        private void cancelButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Cancel button clicked" );
        }

        // Save updates the current state from the UI form fields and saves the current state into permanent storage.
        // Revert reverts the current state to what's in permanent storage and repopulates the form fields.
        private void saveButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Save button clicked" );
            updateSpellLineData();
            qrb.SpellClasses = spellClasses;
            qrb.SpellIDs = spellIDs;
            qrb.SpellIDsName = spellIDsName;
        }
        private void revertButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Revert button clicked" );
            spellClasses = qrb.CloneSpellClasses();
            spellIDs = qrb.CloneSpellIDs();
            spellIDsName = qrb.CloneSpellIDsName();
            populateSpellLineFields();
        }

        // When the class name drop-down list is changed, we update the current state from the form fields, then reset the
        // class name, empty the spell line name (so we'll use the first one for the new class) and repopulate the form fields
        // with the new class and line's information.
        private void className_SelectionChangeCommitted( object sender, EventArgs e )
        {
            Debug.WriteLine( "Class name selection changed" );
            ComboBox className = (ComboBox) sender;

            updateSpellLineData();
            currentClassName = className.Text;
            currentSpellLine = "";
            Debug.WriteLine( "New spell class selected: " + currentClassName );
            populateSpellLineFields();
        }

        // Works the same as the class name selection change event, except we leave the class name alone and
        // change the current spell line name so we'll repopulate it's data.
        private void spellLineName_SelectionChangeCommitted( object sender, EventArgs e )
        {
            Debug.WriteLine( "Spell line name selection changed" );
            ComboBox spellLineName = (ComboBox) sender;

            updateSpellLineData();
            currentSpellLine = spellLineName.Text;
            Debug.WriteLine( "New spell line selected: " + currentSpellLine );
            populateSpellLineFields();
        }

        // When the user clears a specific line, find out which one based on the button's name (it ends with the same number
        // as the spinner and textbox will), clear the textbox and zero the spinner.
        private void clearButton_Click( object sender, EventArgs e )
        {
            Button btn = (Button) sender;
            Debug.WriteLine( "Clearing spell line fields - " + btn.Name );

            string levelSpinnerName = "spell_numericUpDown" + btn.Name.Remove( 0, 12 );
            string textBoxName = "spell_textBox" + btn.Name.Remove( 0, 12 );
            string idBoxName = "spell_idBox" + btn.Name.Remove( 0, 12 );

            Control[] spinners = Controls.Find( levelSpinnerName, true );
            if ( spinners.Length > 0 )
            {
                NumericUpDown ud = spinners[0] as NumericUpDown;
                ud.ResetText();
                ud.Text = "0";
                ud.Value = 0;
            }
            Control[] tbs = Controls.Find( textBoxName, true );
            if ( tbs.Length > 0 )
                tbs[0].ResetText();
            Control[] ids = Controls.Find( idBoxName, true );
            if ( ids.Length > 0 )
                ids[0].ResetText();
        }

        // If the user wants things sorted, update the current state based on the form data and repopulate the
        // form fields. The last thing done when updating the current state is to sort the spells into order,
        // which accomplishes what we want.
        private void sortButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Sort spells button clicked" );

            updateSpellLineData();
            populateSpellLineFields();
        }

        // Auto-fill spell names. We take the last filled-in spell with a non-zero level, and strip any
        // Roman numeral suffix off it, adding 1 to the suffix and 10 to the level. If we can't find a suffix, assume
        // 1 for the suffix. If we can't find a spell, take the spell line name as the base spell name and 1 as the start
        // of numbering and initial level. Then add spells until we completely populate the form. The spell text will be
        // the base name plus the Roman numeral suffix. Increment the suffix by 1 and the level by 10 each time.
        private void autoFillButton_Click( object sender, EventArgs e )
        {
            if ( String.IsNullOrEmpty( currentClassName ) || String.IsNullOrEmpty( currentSpellLine ) )
                return;

            Debug.WriteLine( "Autofilling " + currentClassName + ": " + currentSpellLine );

            int suffix = 0;
            string base_name = "";
            int level = 0;

            // Search our textboxes and spinners for the highest non-zero-level spell present
            // and record it.
            for ( int i = 1; i <= spellNameCount; i++ )
            {
                Control[] a_lvl = Controls.Find( "spell_numericUpDown" + i.ToString(), true );
                Control[] a_txt = Controls.Find( "spell_textBox" + i.ToString(), true );

                int lvl = 0;
                string txt = "";
                if ( a_txt.Length > 0 )
                {
                    TextBox c_txt = a_txt[0] as TextBox;
                    if ( c_txt != null )
                        txt = c_txt.Text;
                    if ( !String.IsNullOrEmpty( txt ) )
                    {
                        if ( a_lvl.Length > 0 )
                        {
                            NumericUpDown c_lvl = a_lvl[0] as NumericUpDown;
                            if ( c_lvl != null )
                            {
                                String v = c_lvl.Value.ToString();
                                if ( !String.IsNullOrEmpty( v ) )
                                    int.TryParse( v, out lvl );
                                else
                                    lvl = 0;
                            }
                        }
                    }
                }
                if ( !String.IsNullOrEmpty( txt ) && ( lvl > 0 ) && ( lvl > level ) )
                {
                    base_name = txt;
                    level = lvl;
                }
            }
            if ( !String.IsNullOrEmpty( base_name ) )
            {
                Debug.WriteLine( "Found highest spell " + level.ToString() + ": " + base_name );

                // Tokenize the spell name, find the last part that matches a Roman numeral. If we found one,
                // remember it's value and rebuild the base name minus the Roman numeral suffix
                string[] tokens = base_name.Split();
                for ( int i = tokens.Length - 1; i >= 0; i-- )
                {
                    if ( !String.IsNullOrEmpty( tokens[i] ) )
                    {
                        int j = fromRoman( tokens[i] );
                        if ( j > 0 )
                        {
                            suffix = j;
                            base_name = "";
                            for ( j = 0; j < i; j++ )
                            {
                                if ( !String.IsNullOrEmpty( tokens[j] ) )
                                    base_name += " " + tokens[j];
                            }
                            break;
                        }
                    }
                }
                suffix += 1;
                level += 10;
            }
            else
            {
                Debug.WriteLine( "No spell found, using line name" );

                // Use the spell line name
                base_name = currentSpellLine;
                suffix = 1;
                level = 1;
            }
            Debug.WriteLine( "Beginning auto-fill from level " + level.ToString() + ", suffix " + suffix.ToString() + ", base name " + base_name );

            // Go through our textboxes and spinners filling blank ones in starting with the level
            // and suffix numbering we established earlier.
            for ( int i = 1; i <= spellNameCount; i++ )
            {
                Control[] a_lvl = Controls.Find( "spell_numericUpDown" + i.ToString(), true );
                Control[] a_txt = Controls.Find( "spell_textBox" + i.ToString(), true );
                Control[] a_sid = Controls.Find( "spell_idBox" + i.ToString(), true );
                if ( ( a_lvl.Length > 0 ) && ( a_txt.Length > 0 ) )
                {
                    NumericUpDown c_lvl = a_lvl[0] as NumericUpDown;
                    TextBox c_txt = a_txt[0] as TextBox;
                    TextBox c_sid = a_sid[0] as TextBox;
                    if ( level > c_lvl.Maximum ) // Check and terminate when we run past our maximum level
                        break;
                    if ( String.IsNullOrEmpty( c_txt.Text ) && ( String.IsNullOrEmpty( c_lvl.Text ) || ( c_lvl.Value == 0 ) ) )
                    {
                        string sn = base_name + " " + toRoman( suffix );
                        Debug.WriteLine( "Auto-fill " + i.ToString() + " level " + level.ToString() + ": " + sn );
                        c_lvl.Text = level.ToString();
                        c_lvl.Value = level;
                        c_txt.Text = sn;
                        // Locate spell ID if possible
                        if ( spellIDsName.ContainsKey( sn ) )
                            c_sid.Text = spellIDsName[sn].ID.ToString();
                        suffix++;
                        // Next level calculation. Initially spells get replaced every 7-8 levels. Once they hit
                        // the teens, replacement goes on a 14-level interval. At 70 the interval changes to
                        // every 10 levels. This isn't perfect, there's a lot of weirdness in T1 and T2. Once
                        // you've got the spell in T2, though, it seems pretty solid after that, and this seems
                        // good enough to be useful. It's sure better than filling things in by hand.
                        if ( level >= 70 )
                            level += 10;
                        else if ( level >= 10 )
                            level += 14;
                        else
                            level += 8;
                    }
                }
            }
        }

        private static string[] roman_numbers_high = { "", "X", "XX", "XXX", "XL", "L", "LX", "LXX", "LXXX", "XC" };
        private static string[] roman_numbers_low = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };

        /// <summary>
        ///  Convert an integer into a Roman numeric string
        ///  Note: this function only works on values in the 1-99 range
        /// </summary>
        /// <param name="i">Number to convert</param>
        /// <returns>Roman numeric form</returns>
        private string toRoman( int i )
        {
            int il = Math.Abs( i ) % 10;
            int ih = ( Math.Abs( i ) / 10 ) % 10;
            return roman_numbers_high[ih] + roman_numbers_low[il];
        }

        /// <summary>
        /// Convert a Roman numeric string to an int. It does this by scaning first the high-digit array and then
        /// the low-digit array looking for matches. If it matches the high-digit array it removes the match from
        /// the string and continues with the low digit. This works because no string for a number 1-9 can possibly
        /// match an initial tens-digit string for 10-99.
        /// </summary>
        /// <param name="s">String to convert</param>
        /// <returns>Integer value</returns>
        private int fromRoman( string s )
        {
            string low_s = s;
            int il = 0;
            int ih = 0;
            for ( ih = 9; ih > 0; ih-- )
            {
                if ( s.StartsWith( roman_numbers_high[ih] ) )
                {
                    low_s = s.Remove( 0, roman_numbers_high[ih].Length );
                    break;
                }
            }
            for ( il = 9; il > 0; il-- )
            {
                if ( String.Equals( low_s, roman_numbers_low[il] ) )
                    break;
            }
            return ( ih * 10 ) + il;
        }

        // Delete a spell line. If we have a spell line for this class we remove it
        // from the QRBSpellClass object for our current class, reset to an empty
        // spell line string and repopulate the form fields. This'll reset things
        // including handling the case where we deleted the last spell line.
        private void deleteLineButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Clear entire line button clicked" );
            Debug.WriteLine( "Class: " + currentClassName );
            Debug.WriteLine( "Spell line: " + currentSpellLine );

            QRBSpellClass spellClass = null;
            if ( spellClasses.ContainsKey( currentClassName ) )
                spellClass = spellClasses[currentClassName];
            if ( spellClass != null )
                spellClass.removeSpellLine( currentSpellLine );
            currentSpellLine = "";
            populateSpellLineFields();
            return;
        }

        // Create a new spell line. We open the new spell line dialog and let it do it's work, then
        // check it's return status. If the user ended it by clicking OK, we retrieve the spell line
        // name and tooltip text from the dialog box object. If the spell line doesn't already exist
        // we add the spell line with the tooltip text to the spell lines for the current class. Then
        // we resort the spell lines into alphabetical order and repopulate the UI form fields. We set
        // the newly-created spell line name as the current spell line to be edited, that's probably
        // what the user wants to do.
        private void newLineButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "New spell line button clicked" );
            Debug.WriteLine( "Class: " + currentClassName );

            updateSpellLineData();
            using ( NewSpellLine nsl = new NewSpellLine() )
            {
                DialogResult result = nsl.ShowDialog();
                if ( result == DialogResult.OK )
                {
                    string newSpellLine = nsl.SpellLineName;
                    string newSpellTooltip = nsl.SpellLineDescription;
                    if ( String.IsNullOrEmpty( newSpellTooltip ) )
                        newSpellTooltip = newSpellLine;
                    QRBSpellClass spellClass = null;
                    if ( spellClasses.ContainsKey( currentClassName ) )
                        spellClass = spellClasses[currentClassName];
                    if ( spellClass != null )
                    {
                        QRBSpellLine sl = spellClass.findSpellLine( newSpellLine );
                        if ( sl == null )
                        {
                            Debug.WriteLine( "Adding new spell line: " + newSpellLine );
                            spellClass.addSpellLine( newSpellLine, newSpellTooltip );
                        }
                        currentSpellLine = newSpellLine;
                    }
                    spellClass.resequenceSpellLines();
                    populateSpellLineFields();
                }
            }
        }

        // When entering a textbox, select all text so that typing replaces the current contents
        private void textbox_EnterSelect( object sender, EventArgs e )
        {
            TextBox tb = sender as TextBox;
            if ( tb != null )
            {
                tb.SelectAll();
            }
        }

        // When leaving a textbox, if it's a spell name or ID box check and update the corresponding other
        // box's text if it's empty and we can find information in the spell IDs dictionary
        private void textbox_LeaveUpdate( object sender, EventArgs e )
        {
            TextBox tb = sender as TextBox;
            TextBox spell_box = null;
            TextBox id_box = null;
            bool is_id = false;
            if ( tb != null )
            {
                if ( tb.Name.StartsWith( "spell_textBox" ) )
                {
                    spell_box = tb;
                    string idBoxName = "spell_idBox" + tb.Name.Remove( 0, 13 );
                    Control[] a_id = Controls.Find( idBoxName, true );
                    if ( a_id.Length > 0 )
                        id_box = a_id[0] as TextBox;
                }
                else if ( tb.Name.StartsWith( "spell_idBox" ) )
                {
                    id_box = tb;
                    is_id = true;
                    string textBoxName = "spell_textBox" + tb.Name.Remove( 0, 11 );
                    Control[] a_txt = Controls.Find( textBoxName, true );
                    if ( a_txt.Length > 0 )
                        spell_box = a_txt[0] as TextBox;
                }
            }
            if ( spell_box != null && id_box != null )
            {
                long spell_id = 0;
                string spell_name = "";
                if ( !long.TryParse( id_box.Text, out spell_id ) )
                    spell_id = 0;
                spell_name = spell_box.Text;

                if ( is_id && spell_id != 0 && String.IsNullOrEmpty( spell_name ) )
                {
                    if ( spellIDs.ContainsKey( spell_id ) )
                    {
                        spell_name = spellIDs[spell_id].Name;
                        spell_box.Text = spell_name;
                    }
                }
                else if ( !is_id && spell_id == 0 && !String.IsNullOrEmpty( spell_name ) )
                {
                    if ( spellIDsName.ContainsKey( spell_name ) )
                    {
                        spell_id = spellIDsName[spell_name].ID;
                        id_box.Text = spell_id.ToString();
                    }
                }
            }
        }

        // When entering a numeric up/down spinner, select all text so that typing replaces the current contents
        private void numericUpDown_EnterSelect( object sender, EventArgs e )
        {
            NumericUpDown ud = sender as NumericUpDown;
            if ( ud != null )
            {
                if ( String.IsNullOrEmpty( ud.Text ) )
                {
                    ud.ResetText();
                    ud.Text = "0";
                    ud.Value = 0;
                }
                ud.Select( 0, ud.Text.Length );
            }
        }

        private void helpButton_Click( object sender, EventArgs e )
        {
            Button bt = sender as Button;
            // Check QuickRaidButtons.hhp in the Help Files folder for the topic IDs. You'll find what you need in the ALIAS and MAP sections
            Help.ShowHelp( bt, "file:" + HelpFile, HelpNavigator.TopicId, "1002" );
        }
    }
}
