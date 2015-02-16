After installing, some example files will be located in the Examples folder
under the tool's installation folder (normally this will be
"C:\Programs (x86)\QuickRaidButtons").

SpellIDS.txt and SpellLines.txt are CSV files containing the spell IDS and
spell line definitions for the default ProfitUI setup, suitable for importing
via the Import Spell IDs and Import Spell Lines functions within the tool.
If you already have many button assignments done and want to fill in missing
spell lines, the best way would be to import these files and adjust things
from there.

ButtonAssignments.txt is a CSV file containing the button assignments for
the default ProfitUI layout. It requires the spell lines from SpellLines.txt
to work properly. It can be imported via the Import button on the button
assignments screen.

QRBStorage.xml is a pre-made copy of the ProfitUI QRB configuration, with
all the spell IDs and spell lines imported and the button assignments made
according to the default ProfitUI setup. If you want to start fresh from
the default ProfitUI setup and adjust button assignments from there, you
can just copy this file directly to the AppData\QuickRaidButtons folder
under your user folder. This will destroy any existing work you've done and
replace it with a clean copy of ProfitUI's default QRB setup.

_ProfitUI_QuickRaidButtons.txt is a pre-made copy of the ProfitUI QRB config
file created by the tool from the default ProfitUI spell line definitions and
button configuration.