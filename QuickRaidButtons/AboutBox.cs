using System;
using System.Reflection;
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
    partial class AboutBox : Form
    {
        public AboutBox()
        {
            InitializeComponent();
            this.Text = String.Format( "About {0}", AssemblyTitle );
            this.labelProductName.Text = AssemblyProduct;
            this.labelVersion.Text = AssemblyVersion;
            this.labelCopyright.Text = AssemblyCopyright;
            // We set these three ourselves because the defaults didn't do what I wanted
            this.labelLicense.Text = "Licensed under the GPLv3 or any later version";
            this.textBoxDescription.Text = "GUI tool for generating quick-raid buttons for ProfitUI";
        }

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyTitleAttribute ), false );
                if ( attributes.Length > 0 )
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute) attributes[0];
                    if ( titleAttribute.Title != "" )
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension( Assembly.GetExecutingAssembly().CodeBase );
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyDescriptionAttribute ), false );
                if ( attributes.Length == 0 )
                {
                    return "";
                }
                return ( (AssemblyDescriptionAttribute) attributes[0] ).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyProductAttribute ), false );
                if ( attributes.Length == 0 )
                {
                    return "";
                }
                return ( (AssemblyProductAttribute) attributes[0] ).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyCopyrightAttribute ), false );
                if ( attributes.Length == 0 )
                {
                    return "";
                }
                return ( (AssemblyCopyrightAttribute) attributes[0] ).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyCompanyAttribute ), false );
                if ( attributes.Length == 0 )
                {
                    return "";
                }
                return ( (AssemblyCompanyAttribute) attributes[0] ).Company;
            }
        }
        #endregion
    }
}
