using System.Collections.Generic;

namespace CVS_History_Viewer.Resources.Classes
{
    public class Revision
    {
        public string sRevision;
        public string sState;
        public string sLinesChanged;
        public List<Tag> cTags = new List<Tag>();
        public CVSFile oFile;
        public bool bReAdded = false;
        public List<DiffBlock> cDiffBlocks = new List<DiffBlock>();
    }
}
