using System;
using System.Collections.Generic;

namespace CVS_History_Viewer.Resources.Classes
{
    public class Commit
    {
        public int iID { get; set; }
        public string sDescription { get; set; }
        public DateTime dDate { get; set; }
        public DateTime dLocalDate { get; set; }
        public string sAuthor { get; set; }
        public string sHASH { get; set; }
        public string sShortHASH { get; set; }
        public string sDescriptionTable { get; set; }
        public List<Revision> cRevisions { get; set; } = new List<Revision>();
    }
}
