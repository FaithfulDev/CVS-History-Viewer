using System;
using System.Windows;

namespace CVS_History_Viewer.Resources.Windows
{
    /// <summary>
    /// Interaktionslogik für CrashReport.xaml
    /// </summary>
    public partial class CrashReport : Window
    {
        public CrashReport(Exception oException)
        {
            InitializeComponent();
            this.uiStacktrace.Text = oException.Message + Environment.NewLine + oException.StackTrace;
            this.uiStacktrace.Tag = oException;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ReportIssue_Click(object sender, RoutedEventArgs e)
        {
            string sBody = Uri.EscapeUriString($"## What happened in your own words?\n\n\n\n ## Stacktrace\n```cs\n{this.uiStacktrace.Text}\n```");
            string sTitle = Uri.EscapeUriString($"Crash Report: \"{((Exception)this.uiStacktrace.Tag).Message}\"");
            sBody = sBody.Replace("#", "%23");
            string sURL = $@"https://github.com/NinjaPewPew/CVS-History-Viewer/issues/new?labels=crash+report&title={sTitle}&body={sBody}";
            System.Diagnostics.Process.Start(sURL);
        }

        private void CloseCrashReport_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
