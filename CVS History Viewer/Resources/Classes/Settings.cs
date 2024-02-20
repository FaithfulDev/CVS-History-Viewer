
using System.Text;

namespace CVS_History_Viewer.Resources.Classes
{
    public class Settings
    {
        private readonly JSONParser.JSONParser oJSONParser = new JSONParser.JSONParser();
        private readonly string sUserSettingsFilePath;

        //Setting Variables
        public string sRootDirectory { get; set; } = null;
        public int iWhitespace { get; set; } = 3;
        private int _iTabSpaces = 4;
        public int iTabSpaces
        {
            get { return _iTabSpaces; }
            set
            {
                _iTabSpaces = value;
                StringBuilder tabBuilder = new StringBuilder(); 
                for (int i = 1; i <= iTabSpaces; i++)
                {
                    tabBuilder.Append(" ");
                }

                sTab = tabBuilder.ToString();
            }
        }
        public string sTab { get; set; } = null;

        public Settings(string sSettingsFilePath)
        {
            this.sUserSettingsFilePath = sSettingsFilePath + "\\UserSettings.json";
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (System.IO.File.Exists(sUserSettingsFilePath))
            {
                oJSONParser.Load(sUserSettingsFilePath);
            }            

            sRootDirectory = (string)oJSONParser.GetValue("RootDirectory", null);
            iWhitespace = (int)oJSONParser.GetValue("Whitespace", iWhitespace);
            iTabSpaces = (int)oJSONParser.GetValue("TabSpaces", iTabSpaces);
        }

        public void SaveSettings()
        {
            oJSONParser.SetValue("RootDirectory", sRootDirectory);
            oJSONParser.SetValue("Whitespace", iWhitespace);
            oJSONParser.SetValue("TabSpaces", iTabSpaces);

            oJSONParser.Save(sUserSettingsFilePath);
        }
    }
}
