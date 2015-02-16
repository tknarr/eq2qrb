using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
    // The button assignment window form. This is the top-level window for the application. We create it passing in a
    // reference to a UICode object to be used for generating the UI code and to set things like how many buttons
    // there are. Right now that's a formality since the window is hard-coded for 5 button fields, the number ProfitUI
    // supports. A better way would be to have the constructor create the UI fields for button assignments based on
    // the number of buttons and adjust the window size and UI element positioning to accomodate the button assignement
    // elements. That, though, is for another day.
    public partial class ButtonAssignment : Form
    {
        private UICode uicode = null; // Reference to the object to use to format/write the UI code file
        private QRBStorage qrb = null; // Reference to our persistent storage object
        private int button_count = 5; // Default number of quick-raid buttons
        private int recent_files_count = 5; // Default number of recent files
        private string HelpFile = "";

        private string currentButtonClass = ""; // The current class radio button selection
        private Dictionary<string, QRBButtonAssignment> buttonAssignments = null; // Our current button assignment state reflecting the GUI state

        // Maintain the recent-files list
        // Note that the recent-files list is stored in reverse, with the most-recent file at the end and
        // the oldest file at the start.
        private void recentFile( string filename )
        {
            if ( Properties.Settings.Default.RecentFiles == null )
                Properties.Settings.Default.RecentFiles = new System.Collections.Specialized.StringCollection();
            if ( Properties.Settings.Default.RecentFiles.Contains( filename ) )
                Properties.Settings.Default.RecentFiles.Remove( filename );
            Properties.Settings.Default.RecentFiles.Add( filename );
            while ( Properties.Settings.Default.RecentFiles.Count > recent_files_count )
                Properties.Settings.Default.RecentFiles.RemoveAt( 0 );
        }

        private void createMissingQRBClasses( QRBStorage q )
        {
            // Insure all classes listed in our class panel actually exist
            Control[] a_panel = Controls.Find( "classPanel", true );
            if ( a_panel.Length > 0 )
            {
                Panel class_panel = a_panel[0] as Panel;
                foreach ( Control c in class_panel.Controls )
                {
                    if ( c.Name.StartsWith( "radioButton" ) )
                    {
                        RadioButton rb = c as RadioButton;
                        if ( rb != null )
                        {
                            if ( !q.ButtonAssignments.ContainsKey( rb.Text ) )
                            {
                                Debug.WriteLine( "Creating missing button assignments for " + rb.Text + ", button count " + q.ButtonCount.ToString() );
                                QRBButtonAssignment ba = new QRBButtonAssignment( rb.Text, q.ButtonCount );
                                q.ButtonAssignments.Add( ba.Classname, ba );
                            }
                            if ( !q.SpellClasses.ContainsKey( rb.Text ) )
                            {
                                Debug.WriteLine( "Creating missing spell class for " + rb.Text );
                                QRBSpellClass sc = new QRBSpellClass( rb.Text );
                                q.SpellClasses.Add( sc.Classname, sc );
                            }
                        }
                    }
                }
            }
            else
                Debug.WriteLine( "Can't find classPanel to check class names" );
        }

        // Retrieve environment and application settings information and create our QRBStorage object. It'll
        // load itself from the storage file as it's created.
        private QRBStorage createQRBStorage( bool existing_file )
        {
            // We'll have set up the storage filename property back in Program::Main()
            string StorageFilename = Properties.Settings.Default.StorageFilename;
            // We always use the schema from our application installation folder
            string SchemaFilename = Path.GetDirectoryName( Application.ExecutablePath ) + Path.DirectorySeparatorChar + "QRBStorage.xsd";
            Debug.WriteLine( "Storage file name: " + StorageFilename );
            Debug.WriteLine( "Schema file name: " + SchemaFilename );
            QRBStorage q = new QRBStorage( StorageFilename, SchemaFilename, button_count, existing_file );
            createMissingQRBClasses( q );
            recentFile( StorageFilename );
            return q;
        }

        // Populate the radio buttons. We need to do this when we start the program, at this point none of the
        // buttons will be selected.
        private void populateRadioButtons()
        {
            Control[] panel = Controls.Find( "panel1", true );
            if ( panel.Length != 0 )
            {
                foreach ( Control c in panel[0].Controls )
                {
                    if ( c.Name.StartsWith( "radioButton" ) )
                    {
                        RadioButton rb = c as RadioButton;
                        if ( rb != null )
                        {
                            if ( String.Equals( rb.Text, currentButtonClass ) )
                                rb.Checked = true;
                            else
                                rb.Checked = false;
                        }
                    }
                }
            }
        }

        // Populate the UI form fields based on the current state. We locate the current class's button assignments, creating a
        // blank object and adding it to the collection if we couldn't find one, and the current class's spell lines. For each
        // button assignment combo box, we populate it in with a blank item and then one item for each spell line for the class
        // (they should be in alphabetical order already). If the item we're adding matches the current spell line assigned to
        // this button, we remember the item's index so we can set that as the initial selection for the combo box. If we don't
        // find a match we'll set the selection to the blank first item (nothing assigned to this button).
        private void populateButtonAssignments()
        {
            // Locate the button assignments for the current class
            QRBButtonAssignment currentAssignments = null;
            if ( buttonAssignments.ContainsKey( currentButtonClass ) )
                currentAssignments = buttonAssignments[currentButtonClass];
            if ( currentAssignments == null )
            {
                currentAssignments = new QRBButtonAssignment( currentButtonClass, button_count );
                buttonAssignments[currentButtonClass] = currentAssignments;
            }

            // Locate the spell lines for the current class
            QRBSpellClass spellLines = null;
            if ( qrb.SpellClasses.ContainsKey( currentButtonClass ) )
                spellLines = qrb.SpellClasses[currentButtonClass];

            // Populate the pull-down boxes for the 5 buttons we support
            int buttonNumber = 0;
            ComboBox[] cbs = new ComboBox[] { comboBox1, comboBox2, comboBox3, comboBox4, comboBox5 };
            foreach ( ComboBox cb in cbs )
            {
                buttonNumber++;
                Debug.WriteLine( "Button number " + buttonNumber.ToString() );
                int buttonIndex = 0;
                string currentButton = currentAssignments.getButton( buttonNumber );
                cb.Items.Clear();
                // We always have a blank no-assignment possibility
                cb.Items.Add( "" );
                if ( spellLines != null )
                {
                    int i = 1; // Remember that we have that initial blank entry in the combobox
                    foreach ( QRBSpellLine sl in spellLines.SpellLines )
                    {
                        Debug.WriteLine( "Spell line " + i.ToString() + ": " + sl.Linename );
                        cb.Items.Add( sl.Linename );
                        // If the entry we just populated matches the current button assignment, remember
                        // it's index so we can set the drop-down's initial value
                        if ( sl.Linename == currentButton )
                            buttonIndex = i;
                        i++;
                    }
                }

                Debug.WriteLine( "Active spell line: " + buttonIndex.ToString() );
                // Set the initial item in the drop-down to the current selection, or to the
                // blank line if we didn't have a selection.
                cb.SelectedIndex = buttonIndex;
                //cb.SelectedText = currentButton;
            }
        }

        private void updateStatusText( string filename )
        {
            toolStripStatusLabelFilename.Text = filename;
        }

        // Our primary constructor. A UICode object is required. We create our QRBStorage object and
        // set the initial class based on the saved application setting for it. If we don't have an
        // initial class, start with Guardian. Once we're done with all that, we create our current
        // button assignment state using a cloned copy of the permanent state and populate our UI
        // fields with the data from the current state.
        public ButtonAssignment( UICode uic )
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

            uicode = uic;
            button_count = uicode.ButtonCount;

            qrb = createQRBStorage( true );
            buttonAssignments = qrb.CloneButtonAssignments();
            if ( !String.IsNullOrEmpty( Properties.Settings.Default.InitialClass ) )
                currentButtonClass = Properties.Settings.Default.InitialClass;
            else
                currentButtonClass = "Guardian";
            Debug.WriteLine( "Initial class selected: " + currentButtonClass );

            populateRadioButtons();
            populateButtonAssignments();

            updateStatusText( qrb.StorageFilename );
        }

        // The form closing event gets fired when we're exiting the program. We save our current state
        // into the permanent state and write the permanent state to the storage XML file. We update our
        // program settings (UI file directory and filename, initial class) and save them before we exit.
        private void ButtonAssignment_FormClosing( object sender, FormClosingEventArgs e )
        {
            Debug.WriteLine( "Button assignment form closing" );
            // Save as part of closing
            qrb.ButtonAssignments = buttonAssignments;
            qrb.saveDocument();
            Properties.Settings.Default.InitialClass = currentButtonClass;
            Properties.Settings.Default.Save();
        }

        // The exit button was clicked, so we exit the application.
        private void exitButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Exit button clicked" );
            // Save as part of closing
            qrb.ButtonAssignments = buttonAssignments;
            qrb.saveDocument();
            Properties.Settings.Default.InitialClass = currentButtonClass;
            Properties.Settings.Default.Save();
            Application.Exit();
        }

        // Fires when the Generate UI File button was clicked. We save the current state into the permanent state
        // and open a standard file save dialog, setting it to default to the last directory and filename the user
        // generated UI code into. If they click OK in the file save dialog, we'll use the UI code object to generate
        // UI code from the QRBStorage object.
        private void generateButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Generate UI code button clicked" );
            qrb.ButtonAssignments = buttonAssignments;
            buttonAssignments = qrb.CloneButtonAssignments();
            qrb.saveDocument();

            // Open a file save dialog, setting the path and filename based on the UIPath and UIFilename in the qrb object
            using ( SaveFileDialog dlg = new SaveFileDialog() )
            {
                dlg.InitialDirectory = uicode.UIPath;
                dlg.FileName = uicode.UIFilename;
                dlg.DefaultExt = ".txt";
                dlg.AddExtension = true;
                dlg.CheckPathExists = true;
                dlg.OverwritePrompt = true;
                dlg.ValidateNames = true;
                dlg.RestoreDirectory = true;
                dlg.Filter = "Text files|*.txt|XML files|*.xml|All files|*.*";
                DialogResult result = dlg.ShowDialog( this );
                if ( result == DialogResult.OK )
                {
                    string fp = dlg.FileName;
                    Debug.Write( "UI code selected file: " + fp );
                    uicode.UIPath = Path.GetDirectoryName( fp );
                    Debug.Write( "UI code selected directory: " + uicode.UIPath );
                    uicode.UIFilename = Path.GetFileName( fp );
                    Debug.Write( "UI code selected filename: " + uicode.UIFilename );
                    uicode.saveUICode( qrb );
                    Properties.Settings.Default.UIFileFolder = uicode.UIPath;
                    Properties.Settings.Default.UIFilename = uicode.UIFilename;
                }
            }
        }

        private void openDataFile( bool existing_file )
        {
            qrb.ButtonAssignments = buttonAssignments;
            qrb.saveDocument();
            Properties.Settings.Default.InitialClass = currentButtonClass;
            Properties.Settings.Default.Save();

            // Open a file open dialog to select our CSV file
            using ( OpenFileDialog dlg = new OpenFileDialog() )
            {
                string initial_folder = Path.GetDirectoryName( Properties.Settings.Default.StorageFilename );
                dlg.InitialDirectory = initial_folder;
                dlg.DefaultExt = ".xml";
                dlg.AddExtension = true;
                dlg.CheckPathExists = true;
                dlg.CheckFileExists = existing_file;
                dlg.ValidateNames = true;
                dlg.RestoreDirectory = true;
                dlg.Filter = "XML files|*.xml|All files|*.*";
                DialogResult result = dlg.ShowDialog( this );
                if ( result == DialogResult.OK )
                {
                    string fp = dlg.FileName;
                    Debug.WriteLine( ( existing_file ? "Loading" : "Creating" ) + " new QRB database " + fp );
                    Properties.Settings.Default.StorageFilename = fp;

                    qrb = createQRBStorage( existing_file );
                    buttonAssignments = qrb.CloneButtonAssignments();

                    populateRadioButtons();
                    populateButtonAssignments();

                    updateStatusText( qrb.StorageFilename );
                }
            }

        }

        // Create a new QRB storage file, saving modified data first
        private void newMenuItem_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "New menu item clicked" );
            openDataFile( false );
        }

        // Open a new QRB storage file, saving modified data first
        private void openMenuItem_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Open menu item clicked" );
            openDataFile( true );
        }

        // Save our current state into the permanent state and save to disk, and create a new
        // current state cloned from our permanent state so future changes don't affect the
        // permanent state until the user saves it again.
        private void saveButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Save button clicked" );
            qrb.ButtonAssignments = buttonAssignments;
            buttonAssignments = qrb.CloneButtonAssignments();
            qrb.saveDocument();
            Properties.Settings.Default.InitialClass = currentButtonClass;
            Properties.Settings.Default.Save();
        }

        private void saveAsMenuItem_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Save As menu item clicked" );

            // Open a file save dialog
            using ( SaveFileDialog dlg = new SaveFileDialog() )
            {
                string initial_folder = Path.GetDirectoryName( qrb.StorageFilename );
                string initial_file = Path.GetFileName( qrb.StorageFilename );
                dlg.InitialDirectory = initial_folder;
                dlg.FileName = initial_file;
                dlg.DefaultExt = ".xml";
                dlg.AddExtension = true;
                dlg.CheckPathExists = true;
                dlg.OverwritePrompt = true;
                dlg.ValidateNames = true;
                dlg.RestoreDirectory = true;
                dlg.Filter = "XML files|*.xml|All files|*.*";
                DialogResult result = dlg.ShowDialog( this );
                if ( result == DialogResult.OK )
                {
                    string fp = dlg.FileName;
                    Debug.WriteLine( "Saving data to " + fp );
                    Properties.Settings.Default.StorageFilename = fp;
                    qrb.ButtonAssignments = buttonAssignments;
                    buttonAssignments = qrb.CloneButtonAssignments();
                    qrb.saveDocument( fp );
                    Properties.Settings.Default.InitialClass = currentButtonClass;
                    Properties.Settings.Default.Save();
                }
            }
        }

        // Revert changes. Throw away the current state, clone the permanent state and repopulate the
        // UI form elements.
        private void revertButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Revert button clicked" );
            buttonAssignments = qrb.CloneButtonAssignments();
            populateButtonAssignments();
        }

        // When the Edit Spell Lines button is clicked, bring up the spell lines window handing it the QRBStorage
        // object and the current class so it can set itself to the class we're currently looking at. When it returns
        // control to us, repopulate the UI fields. The window code will have updated the QRBStorage object already.
        // The new spell lines window doesn't alter button assignments, so we don't mess with our current button
        // assignment state. We will, during repopulation, clean things up if the user deleted a currently-assigned
        // spell line.
        private void editSpellLinesButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Edit spell lines button clicked" );
            using ( SpellLines sl_window = new SpellLines( qrb, currentButtonClass ) )
            {
                sl_window.ShowDialog( this );
                populateButtonAssignments();
            }
        }

        // When one of the class radio buttons is clicked, set our current class as it's name (the button names
        // match the classes) and repopulate the UI fields.
        private void classSelection_Click( object sender, EventArgs e )
        {
            RadioButton btn = sender as RadioButton;
            currentButtonClass = btn.Text;
            Debug.WriteLine( "New class selected: " + currentButtonClass );
            populateButtonAssignments();
        }

        // When the user selects a new value from one of the pull-down lists for button assignments, update
        // the current state for that button for our current class with the text of the combo box. We use the
        // Tag attribute of the combo boxes to map them to button numbers.
        private void buttonSpellLine_SelectionChanged( object sender, EventArgs e )
        {
            ComboBox spellLineName = sender as ComboBox;
            int button_number = int.Parse( spellLineName.Tag.ToString() );
            string line_name = spellLineName.Text;

            if ( buttonAssignments.ContainsKey( currentButtonClass ) )
                buttonAssignments[currentButtonClass].setButton( button_number, line_name );
        }

        // Show our About box when the user clicks the About button.
        private void aboutButton_Click( object sender, EventArgs e )
        {
            using ( AboutBox a = new AboutBox() )
            {
                a.ShowDialog( this );
            }
        }

        private void helpButton_Click( object sender, EventArgs e )
        {
            // Check QuickRaidButtons.hhp in the Hel Files folder for the topic IDs. You'll find what you need in the ALIAS and MAP sections
            Help.ShowHelp( this, "file:" + HelpFile, HelpNavigator.TopicId, "1001" );
        }

        // Update the spell ID dictionary, either by adding a new spell ID or by updating an existing one.
        // Duplicate spell names with different IDs are cleaned up during the update process.
        private void updateSpellID( long spell_id, string spell_name )
        {
            QRBSpellID new_sid = new QRBSpellID( spell_id, spell_name );

            if ( qrb.SpellIDs.ContainsKey( spell_id ) )
                qrb.SpellIDs[spell_id].Name = spell_name;
            else
                qrb.SpellIDs.Add( spell_id, new_sid );

            if ( qrb.SpellIDsName.ContainsKey( spell_name ) )
                qrb.SpellIDsName[spell_name].ID = spell_id;
            else
                qrb.SpellIDsName.Add( spell_name, new_sid );
        }

        private void updateSpellsWithID()
        {
            foreach ( KeyValuePair<string, QRBSpellClass> kvp1 in qrb.SpellClasses )
            {
                QRBSpellClass sc = kvp1.Value;
                foreach ( QRBSpellLine sl in sc.SpellLines )
                {
                    foreach ( QRBSpell sp in sl.Spells )
                    {
                        if ( ( sp.Level > 0 ) && ( sp.SpellID == 0 ) && !String.IsNullOrEmpty( sp.Text ) )
                        {
                            if ( qrb.SpellIDsName.ContainsKey( sp.Text ) )
                                sp.SpellID = qrb.SpellIDsName[sp.Text].ID;
                        }
                        else if ( ( sp.Level > 0 ) && String.IsNullOrEmpty( sp.Text ) && ( sp.SpellID != 0 ) )
                        {
                            if ( qrb.SpellIDs.ContainsKey( sp.SpellID ) )
                                sp.Text = qrb.SpellIDs[sp.SpellID].Name;
                        }
                    }
                }
            }
        }

        // CSV file format is the spell ID, a comma and the spell name to the end of the line.
        // Spell ID must be non-zero. Spell name must contain text.

        private void importSpellIDButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Import spell IDs button clicked" );

            // Open a file open dialog to select our CSV file
            using ( OpenFileDialog dlg = new OpenFileDialog() )
            {
                string initial_folder = Properties.Settings.Default.ExportFolder;
                if ( String.IsNullOrEmpty( initial_folder ) )
                    initial_folder = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
                dlg.InitialDirectory = initial_folder;
                dlg.DefaultExt = ".csv";
                dlg.AddExtension = true;
                dlg.CheckPathExists = true;
                dlg.CheckFileExists = true;
                dlg.ValidateNames = true;
                dlg.RestoreDirectory = true;
                dlg.Filter = "CSV files|*.csv;*.txt|All files|*.*";
                DialogResult result = dlg.ShowDialog( this );
                if ( result == DialogResult.OK )
                {
                    string fp = dlg.FileName;
                    Properties.Settings.Default.ExportFolder = Path.GetDirectoryName( fp );
                    Debug.WriteLine( "Importing spell IDs from " + fp );
                    using ( StreamReader sr = new StreamReader( fp ) )
                    {
                        string data_line = sr.ReadLine();
                        while ( data_line != null )
                        {
                            int comma = data_line.IndexOf( ',' );
                            if ( comma > 0 )
                            {
                                long sid = 0;
                                string sn = "";
                                if ( ( comma + 1 ) < data_line.Length )
                                {
                                    sn = data_line.Substring( comma + 1 );
                                    if ( !long.TryParse( data_line.Substring( 0, comma ), out sid ) )
                                    {
                                        sn = "";
                                        sid = 0;
                                    }
                                }
                                if ( ( sid != 0 ) && !String.IsNullOrEmpty( sn ) )
                                {
                                    Debug.WriteLine( "{0:D}: {1}", sid, sn );
                                    updateSpellID( sid, sn );
                                }
                            }
                            data_line = sr.ReadLine();
                        }
                    }
                    updateSpellsWithID();
                }
            }
        }

        private void exportSpellIDButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Export spell IDs button clicked" );

            // Open a file save dialog
            using ( SaveFileDialog dlg = new SaveFileDialog() )
            {
                string initial_folder = Properties.Settings.Default.ExportFolder;
                if ( String.IsNullOrEmpty( initial_folder ) )
                    initial_folder = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
                dlg.InitialDirectory = initial_folder;
                dlg.FileName = "_ProfitUI_SpellIDs.csv";
                dlg.DefaultExt = ".csv";
                dlg.AddExtension = true;
                dlg.CheckPathExists = true;
                dlg.OverwritePrompt = true;
                dlg.ValidateNames = true;
                dlg.RestoreDirectory = true;
                dlg.Filter = "CSV files|*.csv|Text files|*.txt|All files|*.*";
                DialogResult result = dlg.ShowDialog( this );
                if ( result == DialogResult.OK )
                {
                    string fp = dlg.FileName;
                    Properties.Settings.Default.ExportFolder = Path.GetDirectoryName( fp );
                    Debug.WriteLine( "Exporting spell IDs to " + fp );
                    using ( StreamWriter sw = new StreamWriter( fp ) )
                    {
                        foreach ( KeyValuePair<long, QRBSpellID> kvp in qrb.SpellIDs.OrderBy( x => x.Value.Name ) )
                        {
                            QRBSpellID sid = kvp.Value;
                            sw.WriteLine( "{0:D},{1}", sid.ID, sid.Name );
                        }
                    }
                }
            }
        }

        // CSV file format is:
        //     class,spell line name,tooltip text,level,spell ID,spell name
        // The class may be * to indicate a line that applies to all classes. These spell lines will be applied
        //     after the rest of the import is done, and will overwrite/replace spell lines of the same name
        //     that may already exist on a class.
        // The class may be + to indicate a line that applies to all classes that don't already have it. These
        //     act like spell lines for class *, except that they won't overwrite/replace existing spell lines
        //     for classes. This is useful for merging new definitions without affecting existing ones.
        // Classes * and + will not be used during export, they exist soley for import.
        // The class and spell line name must be present.
        // The tooltip text may be omitted (field left empty), in which case the spell line name will be used
        //     for the tooltip text.
        // The spell level must be >= 0. A level of zero indicates a generic command in which case the spell ID
        //     field will be ignored. A level >0 indicates an actual spell.
        // The spell ID field must be numeric or left empty.
        // Either the spell ID or the spell name must be present. If either is omitted, the code will try to
        //     populate it using the value of the one that was provided. If both are provided, the list of
        //     spell ID/name mappings will be updated to match.
        // If the spell class a line would go in doesn't already exist, it'll be added. If it does exist, it'll
        //     be updated. New spell lines replace existing ones, or are added if a spell line of the same name
        //     doesn't exist. Changes in the class and spell line fields trigger a change to a new spell line, so
        //     all spells for a single line must be in a single block in the imported file.

        private void importSpellLinesButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Import spell lines button clicked" );

            // Open a file open dialog to select our CSV file
            using ( OpenFileDialog dlg = new OpenFileDialog() )
            {
                string initial_folder = Properties.Settings.Default.ExportFolder;
                if ( String.IsNullOrEmpty( initial_folder ) )
                    initial_folder = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
                dlg.InitialDirectory = initial_folder;
                dlg.DefaultExt = ".csv";
                dlg.AddExtension = true;
                dlg.CheckPathExists = true;
                dlg.CheckFileExists = true;
                dlg.ValidateNames = true;
                dlg.RestoreDirectory = true;
                dlg.Filter = "CSV files|*.csv;*.txt|All files|*.*";
                DialogResult result = dlg.ShowDialog( this );
                if ( result == DialogResult.OK )
                {
                    string currentClass = "";
                    string currentSpellLineName = "";
                    string currentTooltip = "";
                    QRBSpellLine currentSpellLine = null;
                    QRBSpellClass currentSpellClass = null;

                    QRBSpellClass globalSpellClassAdd = new QRBSpellClass( "*" );
                    QRBSpellClass globalSpellClassMerge = new QRBSpellClass( "+" );

                    string fp = dlg.FileName;
                    Properties.Settings.Default.ExportFolder = Path.GetDirectoryName( fp );
                    Debug.WriteLine( "Importing spell lines from " + fp );
                    using ( StreamReader sr = new StreamReader( fp ) )
                    {
                        string data_line = sr.ReadLine();
                        while ( data_line != null )
                        {
                            int comma = data_line.IndexOf( ',' );
                            if ( ( comma > 0 ) && ( comma + 1 < data_line.Length ) )
                            {
                                string field1 = "";
                                string field2 = "";
                                string field3 = "";
                                string field4 = "";
                                string field5 = "";
                                string field6 = "";

                                field1 = data_line.Substring( 0, comma );
                                field2 = data_line.Substring( comma + 1 );
                                comma = field2.IndexOf( ',' );
                                if ( comma >= 0 )
                                {
                                    if ( comma + 1 < field2.Length )
                                        field3 = field2.Substring( comma + 1 );
                                    field2 = field2.Substring( 0, comma );
                                }
                                comma = field3.IndexOf( ',' );
                                if ( comma >= 0 )
                                {
                                    if ( comma + 1 < field3.Length )
                                        field4 = field3.Substring( comma + 1 );
                                    field3 = field3.Substring( 0, comma );
                                }
                                comma = field4.IndexOf( ',' );
                                if ( comma >= 0 )
                                {
                                    if ( comma + 1 < field4.Length )
                                        field5 = field4.Substring( comma + 1 );
                                    field4 = field4.Substring( 0, comma );
                                }
                                comma = field5.IndexOf( ',' );
                                if ( comma >= 0 )
                                {
                                    if ( comma + 1 < field5.Length )
                                        field6 = field5.Substring( comma + 1 );
                                    field5 = field5.Substring( 0, comma );
                                }

                                // We create new classes if they don't exist, and modify existing ones.
                                // We replace existing spell lines with the imported one and add new ones.
                                if ( !field1.Equals( currentClass ) )
                                {
                                    // If we're starting a new class, add the existing spell class and line if any to
                                    // the storage object.
                                    if ( currentSpellClass != null )
                                    {
                                        if ( currentSpellLine != null )
                                        {
                                            currentSpellLine.resequenceSpells();
                                            currentSpellClass.updateSpellLine( currentSpellLine );
                                            currentSpellClass.resequenceSpellLines();
                                        }
                                        if ( !currentSpellClass.Classname.Equals( "*" ) && !currentSpellClass.Classname.Equals( "+" ) )
                                        {
                                            if ( !qrb.SpellClasses.ContainsKey( currentSpellClass.Classname ) )
                                            {
                                                Debug.WriteLine( "Adding new spell class " + currentSpellClass.Classname );
                                                qrb.SpellClasses.Add( currentSpellClass.Classname, currentSpellClass );
                                            }
                                        }
                                    }

                                    currentClass = field1;
                                    currentSpellLineName = "";
                                    currentTooltip = "";
                                    currentSpellLine = null;
                                    if ( currentClass.Equals( "*" ) )
                                        currentSpellClass = globalSpellClassAdd;
                                    else if ( currentClass.Equals( "+" ) )
                                        currentSpellClass = globalSpellClassMerge;
                                    else if ( qrb.SpellClasses.ContainsKey( currentClass ) )
                                        currentSpellClass = qrb.SpellClasses[currentClass];
                                    else
                                        currentSpellClass = new QRBSpellClass( currentClass );
                                    Debug.WriteLine( "Class: " + currentClass );
                                }
                                if ( ( currentSpellLine == null ) || !field2.Equals( currentSpellLine.Linename ) )
                                {
                                    // If we're starting a new spell line, add the existing spell line if any to the
                                    // current spell class
                                    if ( currentSpellClass != null )
                                    {
                                        if ( currentSpellLine != null )
                                        {
                                            currentSpellLine.resequenceSpells();
                                            currentSpellClass.updateSpellLine( currentSpellLine );
                                        }
                                    }

                                    currentSpellLineName = field2;
                                    currentTooltip = field3;
                                    Debug.WriteLine( "Spell line: " + currentSpellLineName );
                                    if ( String.IsNullOrEmpty( currentTooltip ) )
                                        currentTooltip = currentSpellLineName;
                                    else
                                        Debug.WriteLine( "Tooltip text: " + currentTooltip );
                                    currentSpellLine = new QRBSpellLine( currentSpellLineName, currentTooltip );
                                }
                                if ( !String.IsNullOrEmpty( field4 ) && ( !String.IsNullOrEmpty( field5 ) || !String.IsNullOrEmpty( field6 ) ) )
                                {
                                    int lvl = -1;
                                    if ( int.TryParse( field4, out lvl ) && ( lvl >= 0 ) )
                                    {
                                        long spell_id = 0;
                                        if ( !long.TryParse( field5, out spell_id ) )
                                            spell_id = 0;
                                        string spell_name = field6;

                                        if ( spell_id == 0 && !String.IsNullOrEmpty( spell_name ) )
                                        {
                                            // Try to find spell ID for name
                                            if ( qrb.SpellIDsName.ContainsKey( spell_name ) )
                                                spell_id = qrb.SpellIDsName[spell_name].ID;
                                        }
                                        else if ( spell_id != 0 && String.IsNullOrEmpty( spell_name ) )
                                        {
                                            // Try to find spell name for ID
                                            if ( qrb.SpellIDs.ContainsKey( spell_id ) )
                                                spell_name = qrb.SpellIDs[spell_id].Name;
                                        }
                                        else if ( spell_id != 0 && !String.IsNullOrEmpty( spell_name ) )
                                        {
                                            // Update spell ID dictionary based on new information
                                            updateSpellID( spell_id, spell_name );
                                        }
                                        if ( ( currentSpellLine != null ) && ( !String.IsNullOrEmpty( spell_name ) || spell_id != 0 ) )
                                        {
                                            // Update spell lines
                                            Debug.WriteLine( lvl.ToString() + " : " + ( ( spell_id != 0 ) ? ( "#" + spell_id.ToString() + " " ) : "" ) + spell_name );
                                            currentSpellLine.addSpell( lvl, spell_name, spell_id );
                                        }
                                    }
                                }
                            }
                            data_line = sr.ReadLine();
                        }
                        // If we have a pending spell line or class left at the end of the loop, add them to the storage object
                        if ( currentSpellClass != null )
                        {
                            if ( currentSpellLine != null )
                            {
                                currentSpellLine.resequenceSpells();
                                currentSpellClass.updateSpellLine( currentSpellLine );
                                currentSpellClass.resequenceSpellLines();
                            }
                            if ( !currentSpellClass.Classname.Equals( "*" ) && !currentSpellClass.Classname.Equals( "+" ) )
                            {
                                if ( !qrb.SpellClasses.ContainsKey( currentSpellClass.Classname ) )
                                {
                                    Debug.WriteLine( "Adding new spell class " + currentSpellClass.Classname );
                                    qrb.SpellClasses.Add( currentSpellClass.Classname, currentSpellClass );
                                }
                            }
                        }
                    }

                    // Add global spell lines to classes that don't have them
                    if ( globalSpellClassMerge.SpellLines.Count > 0 )
                    {
                        foreach ( KeyValuePair<string, QRBSpellClass> kvp in qrb.SpellClasses )
                        {
                            QRBSpellClass sc = kvp.Value;

                            foreach ( QRBSpellLine sl in globalSpellClassMerge.SpellLines )
                            {
                                QRBSpellLine osl = sc.findSpellLine( sl.Linename );
                                if ( osl == null )
                                {
                                    QRBSpellLine nsl = sl.Clone() as QRBSpellLine;
                                    sc.updateSpellLine( nsl );
                                }
                            }
                            sc.resequenceSpellLines();
                        }
                    }
                    // Fill in global spell lines, overwriting ones already there
                    if ( globalSpellClassAdd.SpellLines.Count > 0 )
                    {
                        foreach ( KeyValuePair<string, QRBSpellClass> kvp in qrb.SpellClasses )
                        {
                            QRBSpellClass sc = kvp.Value;

                            foreach ( QRBSpellLine sl in globalSpellClassAdd.SpellLines )
                            {
                                QRBSpellLine nsl = sl.Clone() as QRBSpellLine;
                                sc.updateSpellLine( nsl );
                            }
                            sc.resequenceSpellLines();
                        }
                    }
                    populateButtonAssignments();
                }
            }
        }

        // Merge new spells into existing spell lines. The file format is the same as for importing spell lines, but normally
        // the file will only contain new spells to be added to existing lines. Each line is treated separately. If a spell already
        // exists in a spell line, that entry will be ignored. If the spell line doesn't exist a new blank one will be created and
        // the entry merged into it. The special "*" and "+" classes are not recognized here.
        private void mergeSpellLinesButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Merge spell lines button clicked" );

            // Open a file open dialog to select our CSV file
            using ( OpenFileDialog dlg = new OpenFileDialog() )
            {
                string initial_folder = Properties.Settings.Default.ExportFolder;
                if ( String.IsNullOrEmpty( initial_folder ) )
                    initial_folder = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
                dlg.InitialDirectory = initial_folder;
                dlg.DefaultExt = ".csv";
                dlg.AddExtension = true;
                dlg.CheckPathExists = true;
                dlg.CheckFileExists = true;
                dlg.ValidateNames = true;
                dlg.RestoreDirectory = true;
                dlg.Filter = "CSV files|*.csv;*.txt|All files|*.*";
                DialogResult result = dlg.ShowDialog( this );
                if ( result == DialogResult.OK )
                {
                    string fp = dlg.FileName;
                    Properties.Settings.Default.ExportFolder = Path.GetDirectoryName( fp );
                    Debug.WriteLine( "Merging spell lines from " + fp );
                    using ( StreamReader sr = new StreamReader( fp ) )
                    {
                        string data_line = sr.ReadLine();
                        while ( data_line != null )
                        {
                            int comma = data_line.IndexOf( ',' );
                            if ( ( comma > 0 ) && ( comma + 1 < data_line.Length ) )
                            {
                                string new_class = "";
                                string new_line_name = "";
                                string new_tooltip = "";
                                string new_level_str = "";
                                string new_spell_id_str = "";
                                string new_spell_name = "";
                                int new_level = -1;
                                long new_spell_id = 0;

                                new_class = data_line.Substring( 0, comma );
                                new_line_name = data_line.Substring( comma + 1 );
                                comma = new_line_name.IndexOf( ',' );
                                if ( comma >= 0 )
                                {
                                    if ( comma + 1 < new_line_name.Length )
                                        new_tooltip = new_line_name.Substring( comma + 1 );
                                    new_line_name = new_line_name.Substring( 0, comma );
                                }
                                comma = new_tooltip.IndexOf( ',' );
                                if ( comma >= 0 )
                                {
                                    if ( comma + 1 < new_tooltip.Length )
                                        new_level_str = new_tooltip.Substring( comma + 1 );
                                    new_tooltip = new_tooltip.Substring( 0, comma );
                                }
                                comma = new_level_str.IndexOf( ',' );
                                if ( comma >= 0 )
                                {
                                    if ( comma + 1 < new_level_str.Length )
                                        new_spell_id_str = new_level_str.Substring( comma + 1 );
                                    new_level_str = new_level_str.Substring( 0, comma );
                                }
                                comma = new_spell_id_str.IndexOf( ',' );
                                if ( comma >= 0 )
                                {
                                    if ( comma + 1 < new_spell_id_str.Length )
                                        new_spell_name = new_spell_id_str.Substring( comma + 1 );
                                    new_spell_id_str = new_spell_id_str.Substring( 0, comma );
                                }

                                if ( !String.IsNullOrEmpty( new_level_str ) )
                                {
                                    if ( !int.TryParse( new_level_str, out new_level ) )
                                        new_level = -1;
                                }
                                if ( !String.IsNullOrEmpty( new_spell_id_str ) )
                                {
                                    if ( !long.TryParse( new_spell_id_str, out new_spell_id ) )
                                        new_spell_id = 0;
                                }

                                if ( !String.IsNullOrEmpty( new_class ) && !String.IsNullOrEmpty( new_line_name ) &&
                                     ( !String.IsNullOrEmpty( new_spell_name ) || ( new_spell_id != 0 ) ) )
                                {
                                    if ( String.IsNullOrEmpty( new_tooltip ) )
                                        new_tooltip = new_line_name;

                                    // Check our spell ID database and try to populate a missing spell name or ID, or update
                                    // it with new information if fully provided.
                                    if ( String.IsNullOrEmpty( new_spell_name ) && ( new_spell_id != 0 ) )
                                    {
                                        if ( qrb.SpellIDs.ContainsKey( new_spell_id ) )
                                            new_spell_name = qrb.SpellIDs[new_spell_id].Name;
                                    }
                                    else if ( !String.IsNullOrEmpty( new_spell_name ) && ( new_spell_id == 0 ) )
                                    {
                                        if ( qrb.SpellIDsName.ContainsKey( new_spell_name ) )
                                            new_spell_id = qrb.SpellIDsName[new_spell_name].ID;
                                    }
                                    else if ( !String.IsNullOrEmpty( new_spell_name ) && ( new_spell_id != 0 ) )
                                    {
                                        updateSpellID( new_spell_id, new_spell_name );
                                    }

                                    // If the spell class and line exists, merge the spell into that line. If the spell line
                                    // doesn't exist, create a new one to merge into.
                                    if ( qrb.SpellClasses.ContainsKey( new_class ) )
                                    {
                                        QRBSpellClass sc = qrb.SpellClasses[new_class];
                                        QRBSpellLine sl = sc.findSpellLine( new_line_name );
                                        if ( sl == null )
                                            sl = new QRBSpellLine( new_line_name, new_tooltip );
                                        sl.addSpell( new_level, new_spell_name, new_spell_id );
                                        sl.resequenceSpells();
                                        sc.updateSpellLine( sl );
                                        sc.resequenceSpellLines();
                                    }
                                }
                            }

                            data_line = sr.ReadLine();
                        }
                    }
                    populateButtonAssignments();
                }
            }
        }

        private void exportSpellLinesButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Export spell lines button clicked" );

            // Open a file save dialog
            using ( SaveFileDialog dlg = new SaveFileDialog() )
            {
                string initial_folder = Properties.Settings.Default.ExportFolder;
                if ( String.IsNullOrEmpty( initial_folder ) )
                    initial_folder = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
                dlg.InitialDirectory = initial_folder;
                dlg.FileName = "_ProfitUI_SpellLines.csv";
                dlg.DefaultExt = ".csv";
                dlg.AddExtension = true;
                dlg.CheckPathExists = true;
                dlg.OverwritePrompt = true;
                dlg.ValidateNames = true;
                dlg.RestoreDirectory = true;
                dlg.Filter = "CSV files|*.csv|Text files|*.txt|All files|*.*";
                DialogResult result = dlg.ShowDialog( this );
                if ( result == DialogResult.OK )
                {
                    string fp = dlg.FileName;
                    Properties.Settings.Default.ExportFolder = Path.GetDirectoryName( fp );
                    Debug.WriteLine( "Exporting spell lines to " + fp );
                    using ( StreamWriter sw = new StreamWriter( fp ) )
                    {
                        foreach ( KeyValuePair<string, QRBSpellClass> kvp_sc in qrb.SpellClasses.OrderBy( x => x.Key ) )
                        {
                            QRBSpellClass sc = kvp_sc.Value;
                            foreach ( QRBSpellLine sl in sc.SpellLines )
                            {
                                string tooltip_text = sl.Tooltip;
                                if ( sl.Linename.Equals( sl.Tooltip ) )
                                    tooltip_text = "";

                                foreach ( QRBSpell spell in sl.Spells )
                                {
                                    string spell_id_text = "";
                                    if ( spell.SpellID > 0 )
                                        spell_id_text = spell.SpellID.ToString();

                                    sw.WriteLine( "{0},{1},{2},{3:D},{4},{5}", sc.Classname, sl.Linename, tooltip_text, spell.Level, spell_id_text, spell.Text );
                                }
                            }
                        }
                    }
                }
            }
        }

        // CSV file format:
        //     class,button number,spell line name
        // The class must be present.
        // The button number must be numeric and in the range of valid button numbers 1-5.
        // The spell line name must be present.

        private void importButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Import button assignments button clicked" );

            // Open a file open dialog to select our CSV file
            using ( OpenFileDialog dlg = new OpenFileDialog() )
            {
                string initial_folder = Properties.Settings.Default.ExportFolder;
                if ( String.IsNullOrEmpty( initial_folder ) )
                    initial_folder = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
                dlg.InitialDirectory = initial_folder;
                dlg.DefaultExt = ".csv";
                dlg.AddExtension = true;
                dlg.CheckPathExists = true;
                dlg.CheckFileExists = true;
                dlg.ValidateNames = true;
                dlg.RestoreDirectory = true;
                dlg.Filter = "CSV files|*.csv;*.txt|All files|*.*";
                DialogResult result = dlg.ShowDialog( this );
                if ( result == DialogResult.OK )
                {
                    string currentClass = "";

                    string fp = dlg.FileName;
                    Properties.Settings.Default.ExportFolder = Path.GetDirectoryName( fp );
                    Debug.WriteLine( "Importing button assignments from " + fp );
                    using ( StreamReader sr = new StreamReader( fp ) )
                    {
                        string data_line = sr.ReadLine();
                        while ( data_line != null )
                        {
                            int comma = data_line.IndexOf( ',' );
                            if ( ( comma > 0 ) && ( comma + 1 < data_line.Length ) )
                            {
                                string field1 = "";
                                string field2 = "";
                                string field3 = "";

                                field1 = data_line.Substring( 0, comma );
                                field2 = data_line.Substring( comma + 1 );
                                comma = field2.IndexOf( ',' );
                                if ( comma >= 0 )
                                {
                                    if ( comma + 1 < field2.Length )
                                        field3 = field2.Substring( comma + 1 );
                                    field2 = field2.Substring( 0, comma );
                                }

                                QRBButtonAssignment ba = null;
                                QRBSpellClass sc = null;
                                // Check whether this class exists in our storage
                                if ( buttonAssignments.ContainsKey( field1 ) )
                                    ba = buttonAssignments[field1];
                                if ( qrb.SpellClasses.ContainsKey( field1 ) )
                                    sc = qrb.SpellClasses[field1];
                                // Fix up partial classes if needed
                                if ( ba == null && sc != null )
                                {
                                    Debug.WriteLine( "Creating missing button assignments for class " + field1 );
                                    ba = new QRBButtonAssignment( field1, qrb.ButtonCount );
                                    buttonAssignments.Add( ba.Classname, ba );
                                }
                                else if ( ba != null && sc == null )
                                {
                                    Debug.WriteLine( "Creating missing spell class for class " + field1 );
                                    sc = new QRBSpellClass( field1 );
                                    qrb.SpellClasses.Add( sc.Classname, sc );
                                }

                                // Only import button assignments for classes that exist
                                if ( ba != null && sc != null )
                                {
                                    // If we're changing class, reset all the button assignments
                                    if ( !currentClass.Equals( field1 ) )
                                    {
                                        Debug.WriteLine( "Resetting button assignments for class " + field1 );
                                        for ( int i = 1; i <= ba.ButtonCount; i++ )
                                            ba.setButton( i, "" );
                                    }

                                    // Parse out and update this button assignment
                                    int bn = 0;
                                    string sln = "";
                                    if ( !Int32.TryParse( field2, out bn ) )
                                        bn = 0;
                                    if ( !String.IsNullOrEmpty( field3 ) )
                                    {
                                        QRBSpellLine sl = sc.findSpellLine( field3 );
                                        if ( sl != null )
                                            sln = sl.Linename;
                                    }
                                    if ( bn > 0 && !String.IsNullOrEmpty( sln ) )
                                    {
                                        Debug.WriteLine( "Setting " + ba.Classname + " button " + bn.ToString() + ": " + field3 );
                                        ba.setButton( bn, field3 );
                                    }
                                }
                                else
                                    Debug.WriteLine( "Class " + field1 + " does not exist" );

                                currentClass = field1;
                            }

                            data_line = sr.ReadLine();
                        }
                    }
                    populateButtonAssignments();
                }
            }
        }

        private void exportButton_Click( object sender, EventArgs e )
        {
            Debug.WriteLine( "Export button assignments button clicked" );

            // Open a file save dialog
            using ( SaveFileDialog dlg = new SaveFileDialog() )
            {
                string initial_folder = Properties.Settings.Default.ExportFolder;
                if ( String.IsNullOrEmpty( initial_folder ) )
                    initial_folder = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
                dlg.InitialDirectory = initial_folder;
                dlg.FileName = "_ProfitUI_ButtonAssignments.csv";
                dlg.DefaultExt = ".csv";
                dlg.AddExtension = true;
                dlg.CheckPathExists = true;
                dlg.OverwritePrompt = true;
                dlg.ValidateNames = true;
                dlg.RestoreDirectory = true;
                dlg.Filter = "CSV files|*.csv|Text files|*.txt|All files|*.*";
                DialogResult result = dlg.ShowDialog( this );
                if ( result == DialogResult.OK )
                {
                    string fp = dlg.FileName;
                    Properties.Settings.Default.ExportFolder = Path.GetDirectoryName( fp );
                    Debug.WriteLine( "Exporting button assignments to " + fp );
                    using ( StreamWriter sw = new StreamWriter( fp ) )
                    {
                        foreach ( KeyValuePair<string, QRBButtonAssignment> kvp in buttonAssignments.OrderBy( x => x.Key ) )
                        {
                            QRBButtonAssignment ba = kvp.Value;

                            for ( int i = 1; i <= ba.ButtonCount; i++ )
                            {
                                string sln = ba.getButton( i );
                                if ( !String.IsNullOrEmpty( sln ) )
                                    sw.WriteLine( "{0},{1:D},{2}", ba.Classname, i, sln );
                            }
                        }
                    }
                }
            }
        }

        private void clearDataMenuItem_Click( object sender, EventArgs e )
        {
            qrb.SpellIDs.Clear();
            qrb.SpellIDsName.Clear();
            qrb.SpellClasses.Clear();
            qrb.ButtonAssignments.Clear();
            createMissingQRBClasses( qrb );
            populateButtonAssignments();
        }

        // Reset settings to their initial defaults
        private void resetDefaultsMenuItem_Click( object sender, EventArgs e )
        {
            qrb.ButtonAssignments = buttonAssignments;
            buttonAssignments = qrb.CloneButtonAssignments();
            qrb.saveDocument();

            Properties.Settings.Default.InitialClass = "Guardian";
            Properties.Settings.Default.UIFileFolder = "";
            Properties.Settings.Default.UIFilename = "_ProfitUI_QuickRaidButtons.txt";
            Properties.Settings.Default.ExportFolder = "";

            // Construct our application data path and default storage file name
            StringBuilder sb = new StringBuilder();
            sb.Append( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ) );
            sb.Append( Path.DirectorySeparatorChar );
            sb.Append( Properties.Settings.Default.DefaultStorageFolder );
            string SchemaPath = sb.ToString();
            if ( !Directory.Exists( SchemaPath ) )
                Directory.CreateDirectory( SchemaPath );
            sb.Append( Path.DirectorySeparatorChar );
            string AppDataPath = sb.ToString();
            Properties.Settings.Default.StorageFilename = AppDataPath + "QRBStorage.xml";

            Properties.Settings.Default.Save();

            qrb = createQRBStorage( true );
            buttonAssignments = qrb.CloneButtonAssignments();
            currentButtonClass = Properties.Settings.Default.InitialClass;

            populateRadioButtons();
            populateButtonAssignments();

            updateStatusText( qrb.StorageFilename );
        }
    }
}
