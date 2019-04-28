using CVS_History_Viewer.Resources.Windows;
using System;
using System.Windows.Threading;

namespace CVS_History_Viewer.Resources.Classes
{
    public static class GlobalFunctions
    {
        public static DateTime ParseDateTime(string sDate)
        {
            return DateTime.ParseExact(sDate, "yyyy/MM/dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static void CrashHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            new CrashReport(e.Exception).ShowDialog();
        }
    }
}
