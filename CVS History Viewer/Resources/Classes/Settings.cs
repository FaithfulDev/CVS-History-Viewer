
namespace CVS_History_Viewer.Resources.Classes
{
    class Settings
    {
        private JSONParser.JSONParser oJSONParser = new JSONParser.JSONParser();
        private string sUserSettingsFilePath = "";

        //Setting Variables
        public string sRootDirectory = null;
        public int iWhitespace = 3;

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
        }

        public void SaveSettings()
        {
            oJSONParser.SetValue("RootDirectory", sRootDirectory);

            oJSONParser.Save(sUserSettingsFilePath);
        }
    }
}
