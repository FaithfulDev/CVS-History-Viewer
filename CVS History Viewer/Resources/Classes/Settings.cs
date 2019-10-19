
namespace CVS_History_Viewer.Resources.Classes
{
    class Settings
    {
        private JSONParser.JSONParser oJSONParser = new JSONParser.JSONParser();
        private string sUserSettingsFilePath = "";

        //Setting Variables
        public string sRootDirectory = null;
        public int iWhitespace = 3;
        public int iTabSpaces = 4;
        public string sTab = null;

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

            for(int i = 1; i <= iTabSpaces; i++)
            {
                sTab += " ";
            }
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
