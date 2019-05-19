using System.Collections.Generic;

namespace CVS_History_Viewer.Resources.Classes
{
    public class Revision
    {
        public int iCommitID;
        public string sRevision;
        public string sState;
        public string sLinesChanged;
        public List<Tag> cTags = new List<Tag>();
        public CVSFile oFile;
        public bool bReAdded = false;
        public int iWhitespace = 3;
        public List<DiffBlock> cDiffBlocks = new List<DiffBlock>();
    }
}
