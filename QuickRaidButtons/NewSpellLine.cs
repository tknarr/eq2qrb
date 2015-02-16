using System;
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
    // The class to implement the window that lets users create a new spell line
    public partial class NewSpellLine : Form
    {
        // Local fields to return the contents of the form to the caller.
        public string SpellLineName { get; private set; }
        public string SpellLineDescription { get; private set; }

        public NewSpellLine()
        {
            InitializeComponent();
            SpellLineName = "";
            SpellLineDescription = "";
        }

        // The OK and Cancel buttons are pretty basic. When clicked on the OK button grabs the data from the
        // form fields and Cancel clears out the data, then they both return control to the caller with
        // the OK or Cancel result and the caller will grab the data from the object's fields.
        private void okButton_Click( object sender, EventArgs e )
        {
            SpellLineName = textBox1.Text;
            SpellLineDescription = textBox2.Text;
        }
        private void cancelButton_Click( object sender, EventArgs e )
        {
            SpellLineName = "";
            SpellLineDescription = "";
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
    }
}
