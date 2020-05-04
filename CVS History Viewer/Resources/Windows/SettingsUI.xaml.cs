using System;
using System.Windows;
using System.Windows.Controls;
using CVS_History_Viewer.Resources.Classes;

namespace CVS_History_Viewer.Resources.Windows
{
    /// <summary>
    /// Interaktionslogik für SettingsUI.xaml
    /// </summary>
    public partial class SettingsUI : Window
    {
        private readonly Settings oSettings;

        public SettingsUI(Settings settings)
        {
            InitializeComponent();
            this.oSettings = settings;

            this.uiTabspace.Text = oSettings.iTabSpaces.ToString();
            this.uiWhitespace.Text = oSettings.iWhitespace.ToString();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void uiWhitespace_TextChanged(object sender, TextChangedEventArgs e)
        {
            string sInput = ((TextBox)sender).Text;
            
            if (int.TryParse(sInput, out int iResult))
            {
                this.oSettings.iWhitespace = iResult;
            }
        }

        private void uiTabspace_TextChanged(object sender, TextChangedEventArgs e)
        {
            string sInput = ((TextBox)sender).Text;

            if (int.TryParse(sInput, out int iResult))
            {
                this.oSettings.iTabSpaces = iResult;
            }
        }
    }
}
