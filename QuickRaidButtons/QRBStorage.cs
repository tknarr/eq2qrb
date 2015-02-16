using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

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

// These are the basic classes that hold information about spell lines and button assignments.
// The hierarchy goes:
//
// QRBStorage
//     Dictionary of QRBButtonAssignment
//         QRBButtonAssignment
//             Class name
//             Array of button names
//     Dictionary of QRBSpellClass
//         QRBSpellClass
//             Class name
//             List of QRBSpellLine
//                 QRBSpellLine
//                     Spell line name
//                     Tooltip text
//                     List of QRBSpell
//                         QRBSpell
//                             Level
//                             Spell text
//                             Spell ID
//                             Sequence number (for sorting)
//
// The classes all implement ICloneable to make it easy to create physical copies rather than
// just creating new references. That's important because we expect to have to maintain a current
// copy of the state of things and need it to be a different physical object from the permanent
// state. Technically I could've just implemented the Clone() method, but declaring the implementation
// of a standard interface is a better way to go about it.

namespace QuickRaidButtons
{
    public class QRBSpellID : IComparable, ICloneable
    {
        // Data members
        public long ID { get; set; }
        public string Name { get; set; }

        public QRBSpellID()
        {
            ID = 0;
            Name = "";
        }

        public QRBSpellID( long id, string n )
        {
            ID = id;
            Name = n;
        }

        public object Clone()
        {
            return new QRBSpellID( ID, Name );
        }

        public int CompareTo( object obj )
        {
            QRBSpellID right = obj as QRBSpellID;
            if ( right != null )
                return ID.CompareTo( right.ID );
            else
                throw new ArgumentException( "Object is not a QRBSpellID" );
        }
    }

    public class QRBSpell : IComparable, ICloneable
    {
        // Data members
        public int Level { get; set; }
        public int Sequence { get; set; }
        public string Text { get; set; }
        public long SpellID { get; set; }
        public bool DataFeedChecked { get; set; }

        public QRBSpell()
        {
            Level = 0;
            Sequence = 0;
            Text = "";
            SpellID = 0;
            DataFeedChecked = false;
        }

        public QRBSpell( int l, string t, long si = 0, bool df = false )
        {
            Level = l;
            Sequence = 0;
            Text = t;
            SpellID = si;
            DataFeedChecked = df ? df : ( SpellID != 0 );
        }

        public QRBSpell( int l, int s, string t, long si = 0, bool df = false )
        {
            Level = l;
            Sequence = s;
            Text = t;
            SpellID = si;
            DataFeedChecked = df ? df : ( SpellID != 0 );
        }

        public object Clone()
        {
            QRBSpell r = new QRBSpell( Level, Sequence, Text, SpellID );
            r.DataFeedChecked = DataFeedChecked;
            return r;
        }

        public int CompareTo( object obj )
        {
            QRBSpell right = obj as QRBSpell;
            if ( right != null )
            {
                int result = Level.CompareTo( right.Level );
                if ( result == 0 )
                    return Sequence.CompareTo( right.Sequence );
                else
                    return result;
            }
            else
                throw new ArgumentException( "Object is not a QRBSpell" );
        }
    }

    public class QRBSpellLine : IComparable, ICloneable
    {
        // Data members
        public string Linename { get; set; }
        public string Tooltip { get; set; }
        public List<QRBSpell> Spells { get; set; }

        public QRBSpellLine()
        {
            Linename = "";
            Tooltip = "";
            Spells = new List<QRBSpell>();
        }

        public QRBSpellLine( string ln, string tt )
        {
            Linename = ln;
            Tooltip = tt;
            Spells = new List<QRBSpell>();
        }

        // Check whether this spell line has any non-blank spells in it
        // We use this to decide whether or not to output a spell line
        public bool hasSpells()
        {
            foreach ( QRBSpell s in Spells )
                if ( s.Text != "" )
                    return true;
            return false;
        }

        // Convenience methods for adding a spell to a spell line
        public void addSpell( QRBSpell s )
        {
            if ( Spells.Count > 0 )
                s.Sequence = Spells[Spells.Count - 1].Sequence + 1;
            else
                s.Sequence = 1;
            Spells.Add( s );
            return;
        }
        public void addSpell( int l, string t )
        {
            addSpell( new QRBSpell( l, t ) );
            return;
        }
        public void addSpell( int l, string t, long sid )
        {
            addSpell( new QRBSpell( l, t, sid ) );
            return;
        }

        // Resort the spells in a line into order (ascending by level and sequence) and then
        // reset the sequence numbers starting from 1. We use this to keep spells in the
        // correct display order.
        public void resequenceSpells()
        {
            Spells.Sort();
            int sequence = 1;
            foreach ( QRBSpell s in Spells )
            {
                s.Sequence = sequence;
                sequence++;
            }
            return;
        }

        public object Clone()
        {
            QRBSpellLine result = new QRBSpellLine( Linename, Tooltip );
            foreach ( QRBSpell s in Spells )
                result.Spells.Add( s.Clone() as QRBSpell );
            return result;
        }

        public int CompareTo( object obj )
        {
            QRBSpellLine right = obj as QRBSpellLine;
            if ( right != null )
                return Linename.CompareTo( right.Linename );
            else
                throw new ArgumentException( "Object is not a QRBSpellLine" );
        }
    }

    public class QRBSpellClass : IComparable, ICloneable
    {
        // Data members
        public string Classname { get; set; }
        public List<QRBSpellLine> SpellLines { get; set; }

        public QRBSpellClass()
        {
            Classname = "";
            SpellLines = new List<QRBSpellLine>();
        }

        public QRBSpellClass( string cn )
        {
            Classname = cn;
            SpellLines = new List<QRBSpellLine>();
        }

        // Check whether the class has any spell lines with spells in them. Used to
        // check whether we need to output a class or not.
        public bool hasSpellLines()
        {
            foreach ( QRBSpellLine sl in SpellLines )
                if ( sl.hasSpells() )
                    return true;
            return false;
        }

        // Given a spell line name, return a reference to the spell line or a null
        // reference if the spell line doesn't exist.
        public QRBSpellLine findSpellLine( string s )
        {
            foreach ( QRBSpellLine sl in SpellLines )
            {
                if ( sl.Linename.Equals( s ) )
                    return sl;
            }
            return null;
        }

        // Convenience method for adding a new spell line. If the line exists it returns a reference
        // to the existing object, otherwise it creates a new object and adds it to the list.
        public QRBSpellLine addSpellLine( string s, string t = "" )
        {
            foreach ( QRBSpellLine sl in SpellLines )
            {
                if ( sl.Linename.Equals( s ) )
                    return sl;
            }
            QRBSpellLine n = new QRBSpellLine( s, t );
            SpellLines.Add( n );
            return n;
        }
        public QRBSpellLine updateSpellLine( QRBSpellLine nsl )
        {
            removeSpellLine( nsl.Linename );
            SpellLines.Add( nsl );
            return nsl;
        }

        // Convenience method for removing a spell line from the list. Accounts for
        // the possibility of there being more than one instance of the line. That shouldn't
        // happen, but we clean things up just to be sure.
        public void removeSpellLine( string s )
        {
            for ( int i = 0; i < SpellLines.Count; i++ )
            {
                if ( SpellLines[i].Linename.Equals( s ) )
                {
                    SpellLines.RemoveAt( i );
                    i--;
                }
            }
            return;
        }

        // Resort the spell lines in the list into alphabetical order.
        public void resequenceSpellLines()
        {
            SpellLines.Sort();
        }

        public object Clone()
        {
            QRBSpellClass result = new QRBSpellClass( Classname );
            foreach ( QRBSpellLine sl in SpellLines )
                result.SpellLines.Add( sl.Clone() as QRBSpellLine );
            return result;
        }

        public int CompareTo( object obj )
        {
            QRBSpellClass right = obj as QRBSpellClass;
            if ( right != null )
                return Classname.CompareTo( right.Classname );
            else
                throw new ArgumentException( "Object is not a QRBSpellClass" );
        }
    }

    public class QRBButtonAssignment : IComparable, ICloneable
    {
        // Data members
        public string Classname { get; set; }
        private string[] button;

        public int ButtonCount { get; private set; }

        public QRBButtonAssignment( int bc )
        {
            Classname = "";
            ButtonCount = bc;
            button = new string[ButtonCount];
        }

        public QRBButtonAssignment( string cn, int bc )
        {
            Classname = cn;
            ButtonCount = bc;
            button = new string[ButtonCount];
        }

        // Return a reference to the button assignment object for button n, or a null reference
        // if n is out of range. n is 1-based, running from 1 to the button count.
        public string getButton( int n )
        {
            if ( ( n >= 1 ) && ( n <= ButtonCount ) )
                return button[n - 1];
            else
                return "";
        }

        // Set a button assignment. Assigns line name ln to button n. Checks for n being in range.
        public void setButton( int n, string ln )
        {
            if ( ( n >= 1 ) && ( n <= ButtonCount ) )
                button[n - 1] = ln;
            return;
        }

        // Returns the number of buttons with active assignments.
        public int buttonCount()
        {
            int result = 0;
            for ( int i = 0; i < ButtonCount; i++ )
                if ( !string.IsNullOrEmpty( button[i] ) )
                    result++;
            return result;
        }

        public object Clone()
        {
            QRBButtonAssignment result = new QRBButtonAssignment( Classname, 5 );
            for ( int i = 0; i < ButtonCount; i++ )
                result.button[i] = button[i];
            return result;
        }

        public int CompareTo( object obj )
        {
            QRBButtonAssignment right = obj as QRBButtonAssignment;
            if ( right != null )
                return Classname.CompareTo( right.Classname );
            else
                throw new ArgumentException( "Object is not a QRBButtonAssignment" );
        }
    }

    public class QRBStorage : ICloneable
    {
        // Data members
        public string StorageFilename { get; private set; }
        public string SchemaFilename { get; private set; }
        public int ButtonCount { get; private set; }

        // Collections, our spell classes and button assignments
        public Dictionary<string, QRBSpellClass> SpellClasses { get; set; }
        public Dictionary<string, QRBButtonAssignment> ButtonAssignments { get; set; }
        public Dictionary<long, QRBSpellID> SpellIDs { get; set; }
        public Dictionary<string, QRBSpellID> SpellIDsName { get; set; }

        public QRBStorage( int button_count )
        {
            SpellClasses = new Dictionary<string, QRBSpellClass>();
            ButtonAssignments = new Dictionary<string, QRBButtonAssignment>();
            SpellIDs = new Dictionary<long, QRBSpellID>();
            SpellIDsName = new Dictionary<string, QRBSpellID>();
            StorageFilename = "";
            SchemaFilename = "";
            ButtonCount = button_count;
        }

        public QRBStorage( string fileName, string schemaFileName, int button_count, bool read_file = true )
        {
            SpellClasses = new Dictionary<string, QRBSpellClass>();
            ButtonAssignments = new Dictionary<string, QRBButtonAssignment>();
            SpellIDs = new Dictionary<long, QRBSpellID>();
            SpellIDsName = new Dictionary<string, QRBSpellID>();
            StorageFilename = fileName;
            SchemaFilename = schemaFileName;
            ButtonCount = button_count;

            if ( read_file )
                loadDocument();
        }

        public object Clone()
        {
            QRBStorage result = new QRBStorage( ButtonCount );
            result.StorageFilename = StorageFilename;
            result.SchemaFilename = SchemaFilename;
            result.ButtonCount = ButtonCount;
            result.SpellClasses = CloneSpellClasses();
            result.ButtonAssignments = CloneButtonAssignments();
            result.SpellIDs = CloneSpellIDs();
            result.SpellIDsName = CloneSpellIDsName();
            return result;
        }

        // Load the spell class collection from an XML element tree. This would be the tree
        // rooted at the Spells element in the QRBStorage XML schema. The code reflects the
        // structure of the XML, looping through the list of child elements of each element
        // creating child objects and adding them to the collection as we go. At the innermost
        // loop we convert Spell elements into QRBSpell objects, above that we convert SpellLine
        // elements into QRBSpellLine objects, and above that we convert SpellClass elements into
        // QRBSpellClass objects.
        private void loadSpells( XmlElement root )
        {
            foreach ( XmlNode node in root.ChildNodes )
            {
                if ( node.LocalName.Equals( "SpellClass" ) )
                {
                    XmlElement elem = (XmlElement) node;
                    string classname = elem.GetAttribute( "class" );
                    if ( !SpellClasses.ContainsKey( classname ) ) // Take only the first occurrence, ignore subsequence duplicates
                    {
                        QRBSpellClass sc = new QRBSpellClass( classname );

                        foreach ( XmlNode node2 in elem.ChildNodes )
                        {
                            if ( node2.LocalName.Equals( "SpellLine" ) )
                            {
                                // We need to maintain a sequence number for spells in a spell line, starting
                                // over from 1 for each new spell line.
                                int sequence = 1;

                                XmlElement elem2 = (XmlElement) node2;
                                string linename = elem2.GetAttribute( "name" );
                                string tooltip = "";
                                List<QRBSpell> sps = new List<QRBSpell>();
                                foreach ( XmlNode node3 in elem2.ChildNodes )
                                {
                                    if ( node3.LocalName.Equals( "ToolTip" ) )
                                    {
                                        XmlElement elem3 = (XmlElement) node3;
                                        tooltip = elem3.InnerText;
                                    }
                                    else if ( node3.LocalName.Equals( "Spell" ) )
                                    {
                                        int level = -1;
                                        string text = "";
                                        long id = 0;
                                        bool df_check = false;

                                        foreach ( XmlNode node4 in node3.ChildNodes )
                                        {
                                            if ( node4.LocalName.Equals( "Level" ) )
                                            {
                                                XmlElement elem4 = (XmlElement) node4;
                                                level = int.Parse( elem4.InnerText );
                                            }
                                            else if ( node4.LocalName.Equals( "Text" ) )
                                            {
                                                XmlElement elem4 = (XmlElement) node4;
                                                text = elem4.InnerText;
                                            }
                                            else if ( node4.LocalName.Equals( "ID" ) )
                                            {
                                                XmlElement elem4 = (XmlElement) node4;
                                                id = long.Parse( elem4.InnerText );

                                                string dfs = elem4.GetAttribute( "DFCheck" );
                                                if ( !String.IsNullOrEmpty( dfs ) && String.Equals( dfs, "true" ) )
                                                    df_check = true;
                                            }
                                        }
                                        // Level must be 0 or positive, ignore anything illegal
                                        if ( level >= 0 )
                                        {
                                            QRBSpell sp = new QRBSpell( level, sequence, text, id, df_check );
                                            sps.Add( sp );
                                            sequence++;
                                        }
                                    }
                                }

                                QRBSpellLine sl = new QRBSpellLine( linename, tooltip );
                                if ( sps.Count > 0 )
                                    sl.Spells = sps;
                                // Make sure our spells are in the correct order
                                sl.resequenceSpells();
                                sc.SpellLines.Add( sl );
                            }
                        }

                        if ( sc.hasSpellLines() )
                        {
                            // Make sure our spell lines are in alphabetical order
                            sc.resequenceSpellLines();
                            SpellClasses.Add( classname, sc );
                        }
                    }
                }
            }
        }

        // Load the button assignments from an XML element tree. This would be rooted at the
        // ButtonAssignments element in the QRBStorage XML schema. We've got the same structure
        // to the code as for loadSpells(): the inner loop takes Button elements and sets specific
        // button assignments in the current class QRBButtonAssignment object, the outer loop
        // reads ButtonClass elements and creates a new QRBButtonAssignment object for that class.
        // As we finish each ButtonClass element, we add that QRBButtonAssignment to the collection.
        private void loadButtonAssignments( XmlElement root )
        {
            foreach ( XmlNode node in root.ChildNodes )
            {
                if ( node.LocalName.Equals( "ButtonClass" ) )
                {
                    XmlElement elem = (XmlElement) node;
                    string classname = elem.GetAttribute( "class" );
                    if ( !ButtonAssignments.ContainsKey( classname ) )
                    {
                        QRBButtonAssignment ba = new QRBButtonAssignment( classname, ButtonCount );

                        foreach ( XmlNode node2 in elem.ChildNodes )
                        {
                            if ( node2.LocalName.Equals( "Button" ) )
                            {
                                XmlElement elem2 = (XmlElement) node2;
                                int btn_number = int.Parse( elem2.GetAttribute( "number" ) );
                                string spelllinename = "";
                                foreach ( XmlNode node3 in elem2.ChildNodes )
                                {
                                    if ( node3.LocalName.Equals( "SpellLineName" ) )
                                    {
                                        XmlElement elem3 = (XmlElement) node3;
                                        spelllinename = elem3.InnerText;
                                    }
                                }
                                if ( spelllinename != "" )
                                    ba.setButton( btn_number, spelllinename );
                            }
                        }

                        if ( ba.buttonCount() > 0 )
                            ButtonAssignments.Add( classname, ba );
                    }
                }
            }
        }

        private void loadSpellIDs( XmlElement root )
        {
            foreach ( XmlNode node in root.ChildNodes )
            {
                if ( node.LocalName.Equals( "SpellID" ) )
                {
                    XmlElement elem = (XmlElement) node;
                    long id = long.Parse( elem.GetAttribute( "id" ) );
                    string spell = "";
                    foreach ( XmlNode node1 in elem.ChildNodes )
                    {
                        XmlElement elem1 = (XmlElement) node1;
                        if ( elem1.LocalName.Equals( "Spell" ) )
                            spell = elem1.InnerText;
                    }
                    QRBSpellID sid = new QRBSpellID( id, spell );
                    SpellIDs.Add( id, sid );
                    SpellIDsName.Add( spell, sid );
                }
            }
        }

        // Read and parse the XML document out of the storage filename, validating it against our schema. If
        // the file doesn't exist, start with a blank slate. If the filename is empty, throw an exception because
        // we've got messed-up application settings.
        private void loadDocument()
        {
            SpellClasses.Clear();
            ButtonAssignments.Clear();
            SpellIDs.Clear();
            SpellIDsName.Clear();

            if ( String.IsNullOrEmpty( StorageFilename ) )
                throw new ApplicationException( "No QRB storage filename found, application settings are not valid" );
            if ( File.Exists( StorageFilename ) )
            {
                // Create an XmlReader and use it to create an XmlDocument read from the storage file
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreWhitespace = true;
                settings.IgnoreComments = true;
                settings.CloseInput = true;
                settings.Schemas.Add( "http://xsd.silverglass.org/QuickRaidButtons/QRBStorage.xsd", SchemaFilename );
                settings.ValidationType = ValidationType.Schema;
                using ( XmlReader reader = XmlReader.Create( StorageFilename, settings ) )
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load( reader );
                    XmlElement root = doc.DocumentElement;
                    foreach ( XmlNode node in root.ChildNodes )
                    {
                        // We only need to deal with elements, there shouldn't be any content at this level
                        if ( node.GetType() == typeof( XmlElement ) )
                        {
                            // Depending on the node name, call the correct load function
                            XmlElement elem = (XmlElement) node;
                            if ( elem.LocalName.Equals( "Spells" ) )
                            {
                                    loadSpells( elem );
                            }
                            else if ( elem.LocalName.Equals( "ButtonAssignments" ) )
                            {
                                loadButtonAssignments( elem );
                            }
                            else if ( elem.LocalName.Equals( "SpellIDs" ) )
                            {
                                loadSpellIDs( elem );
                            }
                        }
                    }
                }
            }
        }

        // Serialize the spell class and button assignment collections into an XML file following our schema.
        public void saveDocument( string fp = null )
        {
            if ( fp != null )
                StorageFilename = fp;
            if ( StorageFilename != "" )
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.CloseOutput = true;
                using ( XmlWriter writer = XmlWriter.Create( StorageFilename, settings ) )
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement( "QuickRaidButtons", "http://xsd.silverglass.org/QuickRaidButtons/QRBStorage.xsd" );
                    writer.WriteAttributeString( "version", "1.3" );

                    // Loop through the spell class collection outputting the contents of the Spells element from it.
                    if ( SpellClasses.Count > 0 )
                    {
                        writer.WriteStartElement( "Spells" );
                        foreach ( KeyValuePair<string, QRBSpellClass> kv in SpellClasses.OrderBy( x => x.Key ) )
                        {
                            QRBSpellClass sc = kv.Value;
                            sc.resequenceSpellLines();

                            writer.WriteStartElement( "SpellClass" );
                            writer.WriteAttributeString( "class", sc.Classname );
                            if ( sc.hasSpellLines() )
                            {
                                foreach ( QRBSpellLine sl in sc.SpellLines.OrderBy( x => x.Linename ) )
                                {
                                    sl.resequenceSpells();

                                    writer.WriteStartElement( "SpellLine" );
                                    writer.WriteAttributeString( "name", sl.Linename );
                                    if ( !string.IsNullOrEmpty( sl.Tooltip ) )
                                    {
                                        writer.WriteElementString( "ToolTip", sl.Tooltip );
                                    }
                                    if ( sl.hasSpells() )
                                    {
                                        foreach ( QRBSpell sp in sl.Spells )
                                        {
                                            writer.WriteStartElement( "Spell" );
                                            writer.WriteElementString( "Level", sp.Level.ToString() );
                                            writer.WriteElementString( "Text", sp.Text.Replace( "%t", "%T" ).Replace( "%i", "%I" ) );
                                            if ( sp.SpellID != 0 )
                                            {
                                                writer.WriteStartElement( "ID" );
                                                writer.WriteAttributeString( "DFCheck", sp.DataFeedChecked ? "true" : "false" );
                                                writer.WriteString( sp.SpellID.ToString() );
                                                writer.WriteEndElement();
                                            }
                                            writer.WriteEndElement();
                                        }
                                    }
                                    writer.WriteEndElement(); // SpellLine
                                }
                            }
                            writer.WriteEndElement(); // SpellClass
                        }
                        writer.WriteEndElement(); // Spells
                    }

                    // And write the contents of the ButtonAssignments element from the button assignment collection
                    if ( ButtonAssignments.Count > 0 )
                    {
                        writer.WriteStartElement( "ButtonAssignments" );
                        foreach ( KeyValuePair<string, QRBButtonAssignment> kv in ButtonAssignments.OrderBy( x => x.Key ) )
                        {
                            QRBButtonAssignment ba = kv.Value;

                            if ( ba.buttonCount() > 0 )
                            {
                                writer.WriteStartElement( "ButtonClass" );
                                writer.WriteAttributeString( "class", ba.Classname );
                                for ( int i = 1; i <= ButtonCount; i++ )
                                {
                                    string s = ba.getButton( i );
                                    if ( !string.IsNullOrEmpty( s ) )
                                    {
                                        writer.WriteStartElement( "Button" );
                                        writer.WriteAttributeString( "number", i.ToString() );
                                        writer.WriteElementString( "SpellLineName", s );
                                        writer.WriteEndElement();
                                    }
                                }
                                writer.WriteEndElement(); // ButtonClass
                            }
                        }
                        writer.WriteEndElement(); // ButtonAssignments
                    }

                    // Spell ID list
                    if ( SpellIDs.Count > 0 )
                    {
                        writer.WriteStartElement( "SpellIDs" );
                        foreach ( KeyValuePair<long, QRBSpellID> kv in SpellIDs.OrderBy( x => x.Value.Name ) )
                        {
                            QRBSpellID sid = kv.Value;

                            writer.WriteStartElement( "SpellID" );
                            writer.WriteAttributeString( "id", sid.ID.ToString() );
                            writer.WriteElementString( "Spell", sid.Name );
                            writer.WriteEndElement(); // SpellID
                        }
                        writer.WriteEndElement(); // SpellIDs
                    }

                    writer.WriteEndElement(); // QuickRaidButtons
                    writer.WriteEndDocument();
                }
            }
        }

        // Methods to create clones of the spell class and button assignment collections. The Dictionary class
        // doesn't implement ICloneable, so we have to implement our own interface for creating a new physical
        // copy of our collections. We do this when we're creating a copy of the collection to hold the current
        // state for the UI forms.
        public Dictionary<string, QRBSpellClass> CloneSpellClasses()
        {
            Dictionary<string, QRBSpellClass> result = new Dictionary<string, QRBSpellClass>();
            foreach ( KeyValuePair<string, QRBSpellClass> kv in SpellClasses )
                result.Add( kv.Key, kv.Value.Clone() as QRBSpellClass );
            return result;
        }
        public Dictionary<string, QRBButtonAssignment> CloneButtonAssignments()
        {
            Dictionary<string, QRBButtonAssignment> result = new Dictionary<string, QRBButtonAssignment>();
            foreach ( KeyValuePair<string, QRBButtonAssignment> kv in ButtonAssignments )
                result.Add( kv.Key, kv.Value.Clone() as QRBButtonAssignment );
            return result;
        }
        public Dictionary<long, QRBSpellID> CloneSpellIDs()
        {
            Dictionary<long, QRBSpellID> result = new Dictionary<long, QRBSpellID>();
            foreach ( KeyValuePair<long, QRBSpellID> kv in SpellIDs )
                result.Add( kv.Key, kv.Value.Clone() as QRBSpellID );
            return result;
        }
        public Dictionary<string, QRBSpellID> CloneSpellIDsName()
        {
            Dictionary<string, QRBSpellID> result = new Dictionary<string, QRBSpellID>();
            foreach ( KeyValuePair<string, QRBSpellID> kv in SpellIDsName )
                result.Add( kv.Key, kv.Value.Clone() as QRBSpellID );
            return result;
        }
    }
}
