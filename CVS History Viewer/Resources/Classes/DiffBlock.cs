using System.Collections.Generic;
using System.Windows.Controls;

namespace CVS_History_Viewer.Resources.Classes
{
    public class DiffBlock
    {
        public int iStartLine = 0;
        public int iEndLine = 0;
        public string sBlockKind = "";
        public List<LineChange> cLines = new List<LineChange>();
        public List<TextBlock> cDiffLines = new List<TextBlock>();

        public class LineChange
        {
            public string sAction;
            public string sLine;
        }        
    }
}
