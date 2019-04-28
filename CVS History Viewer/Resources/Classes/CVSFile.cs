using System;

namespace CVS_History_Viewer.Resources.Classes
{
    public class CVSFile
    {
        public int iID;
        public string sName;
        public string sPath;
        public DateTime dLastUpdated;
        public bool bDeleted = false;
        public string sCVSPath;

        public CVSFile()
        {
            //Nothing.
        }

        public CVSFile(int iID, string sName, string sPath, DateTime dLastUpdated, bool bDeleted, string sCVSPath)
        {
            this.iID = iID;
            this.sName = sName;
            this.sPath = sPath;
            this.dLastUpdated = dLastUpdated;
            this.bDeleted = bDeleted;
            this.sCVSPath = sCVSPath;
        }
    }
}
