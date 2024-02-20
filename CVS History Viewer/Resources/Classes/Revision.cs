using System.Collections.Generic;

namespace CVS_History_Viewer.Resources.Classes
{
    public class Revision
    {
        public int iCommitID { get; set; }
        public string sRevision { get; set; }
        public string sState { get; set; }
        public string sLinesChanged { get; set; }
        public List<Tag> cTags { get; set; } = new List<Tag>();
        public CVSFile oFile { get; set; }
        public bool bReAdded { get; set; } = false;
        public int iWhitespace { get; set; } = 3;
        public List<DiffBlock> cDiffBlocks { get; set; } = new List<DiffBlock>();
    }
}
