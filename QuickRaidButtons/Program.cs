using System;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Configuration;

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
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain currentDomain = default( AppDomain );
            currentDomain = AppDomain.CurrentDomain;
            // Handler for unhandled exceptions.
            currentDomain.UnhandledException += GlobalUnhandledExceptionHandler;
            // Handler for exceptions in threads behind forms.
            System.Windows.Forms.Application.ThreadException += GlobalThreadExceptionHandler;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );

            // Construct our application data path
            StringBuilder sb = new StringBuilder();
            sb.Append( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ) );
            sb.Append( Path.DirectorySeparatorChar );
            sb.Append( Properties.Settings.Default.DefaultStorageFolder );
            string SchemaPath = sb.ToString();
            if ( !Directory.Exists( SchemaPath ) )
                Directory.CreateDirectory( SchemaPath );
            sb.Append( Path.DirectorySeparatorChar );
            string AppDataPath = sb.ToString();

            // Check and migrate settings from previous versions
            if ( !Properties.Settings.Default.Upgraded )
            {
                // From 1.4 on we have an explicit settings version set. 1.4 uses version 4 of the settings. 1.3 is
                // given an assumed version of 3, 1.2 and earlier a version of 2.
                int oldSettingsVersion = 2;
                try
                {
                    oldSettingsVersion = (int) Properties.Settings.Default.GetPreviousVersion( "SettingsVersion" );
                }
                catch ( Exception )
                {
                    oldSettingsVersion = 0;
                }
                // Heuristic logic for old settings. Version 2 didn't have an ExportFolder settings, so if we found
                // that setting our old settings must be version 3. If we didn't find it then we must have a really
                // old set of settings from version 2 or earlier. We don't care about the exact version, everything
                // up through 2 had the same settings format.
                if ( oldSettingsVersion == 0 )
                {
                    try
                    {
                        string s = (string) Properties.Settings.Default.GetPreviousVersion( "ExportFolder" );
                        oldSettingsVersion = 3;
                    }
                    catch ( SettingsPropertyNotFoundException )
                    {
                        oldSettingsVersion = 2;
                    }
                }
                Properties.Settings.Default.Upgrade();
                if ( oldSettingsVersion < 3 )
                {
                    // Originally we stored only a fixed default filename in the StorageFilename setting. As of 1.3 we store
                    // the full path to the storage file and update it when the user selects a new storage file, so we blank
                    // out the storage filename settings after the upgrade and let the check code set the correct default.
                    Properties.Settings.Default.StorageFilename = "";
                }
                // For 1.4 we're only adding settings, so no conversion is needed.
                Properties.Settings.Default.Upgraded = true;
                Properties.Settings.Default.Save();
            }

            // Check and set our initial storage filename if it's not stored in settings
            if ( String.IsNullOrEmpty( Properties.Settings.Default.StorageFilename ) )
            {
                string StorageFilename = AppDataPath + "QRBStorage.xml";
                Properties.Settings.Default.StorageFilename = StorageFilename;
            }

            // Check for whether our schema file exists in our application data folder, and update it if needed
            string SchemaFilename = AppDataPath + "QRBStorage.xsd";
            Debug.WriteLine( "Checking schema file " + SchemaFilename );
            // Update our schema file if the copy in the executable directory's newer than the installed one
            string AppSchemaFilename = Path.GetDirectoryName( Application.ExecutablePath ) + Path.DirectorySeparatorChar + "QRBStorage.xsd";
            if ( !File.Exists( SchemaFilename ) ||
                ( File.GetCreationTime( SchemaFilename ) < File.GetCreationTime( AppSchemaFilename ) ) ||
                ( File.GetLastWriteTime( SchemaFilename ) < File.GetLastWriteTime( AppSchemaFilename ) ) )
            {
                Debug.WriteLine( "Updating schema file from " + AppSchemaFilename );
                File.Copy( AppSchemaFilename, SchemaFilename, true );
            }

            // Create our UI file generation object and our main application form object, then start the form running
            ProfitUICode uicode = new ProfitUICode();
            ButtonAssignment ba = new ButtonAssignment( uicode );
            Application.Run( ba );
        }

        // Exception handling. For unhandled exceptions we log them into Crash.log in the application data folder.
        // Hopefully there won't be any unhandled exceptions, but if one occurs this lets the user send a report
        // so the bug can be fixed.
        private static void GlobalUnhandledExceptionHandler( object sender, UnhandledExceptionEventArgs e )
        {
            Exception ex = null;
            ex = e.ExceptionObject as Exception;
            LogException( ex );
        }
        private static void GlobalThreadExceptionHandler( object sender, System.Threading.ThreadExceptionEventArgs e )
        {
            Exception ex = null;
            ex = e.Exception;
            LogException( ex );
        }
        private static void LogException( Exception e )
        {
            DateTime now = DateTime.Now;

            StringBuilder sb = new StringBuilder();
            sb.Append( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ) );
            sb.Append( Path.DirectorySeparatorChar );
            sb.Append( Properties.Settings.Default.DefaultStorageFolder );
            string DumpPath = sb.ToString();
            if ( !Directory.Exists( DumpPath ) )
                Directory.CreateDirectory( DumpPath );
            sb.Append( Path.DirectorySeparatorChar );
            sb.Append( "Crash.log" );
            string DumpFile = sb.ToString();
            File.WriteAllText( DumpFile, now.ToString() + e.ToString() );
        }
    }
}
