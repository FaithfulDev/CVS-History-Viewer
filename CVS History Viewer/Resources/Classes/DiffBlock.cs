using System.Collections.Generic;
using System.Windows.Controls;

namespace CVS_History_Viewer.Resources.Classes
{
    public class DiffBlock
    {
        public int iStartLine { get; set; } = 0;
        public int iEndLine { get; set; } = 0;
        public string sBlockKind { get; set; } = "";
        public List<LineChange> cLines { get; set; } = new List<LineChange>();
        public List<TextBlock> cDiffLines { get; set; } = new List<TextBlock>();

        public class LineChange
        {
            public string sAction { get; set; }
            public string sLine { get; set; }
        }        
    }
}
