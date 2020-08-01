using System;

namespace CVS_History_Viewer.Resources.Classes
{
    public class CVSFile
    {
        public int iID { get; set; }
        public string sName { get; set; }
        public string sPath { get; set; }
        public DateTime dLastUpdated { get; set; }
        public bool bDeleted { get; set; }  = false;
        public string sCVSPath { get; set; }
        public bool bIgnored { get; set; }= false;

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
