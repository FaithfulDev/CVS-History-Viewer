using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;

namespace CVS_History_Viewer.Resources.Classes
{
    public class SyntaxHighlighting
    {
        private List<Syntax> cSyntax = new List<Syntax>();

        public SyntaxHighlighting(string sPath)
        {
            IEnumerable<FileInfo> cFileInfos = new DirectoryInfo(sPath).EnumerateFiles("*.json", SearchOption.AllDirectories);

            foreach (FileInfo oFileInfo in cFileInfos)
            {
                StreamReader oStreamReader = new StreamReader(new FileStream(oFileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                                                                          System.Text.Encoding.UTF8);
                string sJSONRaw = oStreamReader.ReadToEnd();
                oStreamReader.Close();

                cSyntax.Add(JsonConvert.DeserializeObject<Syntax>(sJSONRaw));
            }            
        }

        public List<Inline> ParseSyntax(string sLine, string sFileExtension)
        {
            List<Inline> cInlines = new List<Inline>();
            List<string[]> cIndexes = new List<string[]>();

            List<Syntax.Scopes> cSyntaxElements = GetSyntaxScopesForExtension(sFileExtension);

            foreach (Syntax.Scopes oElement in cSyntaxElements)
            {
                foreach (String sPattern in oElement.patterns)
                {
                    var match = Regex.Match(sLine, sPattern, RegexOptions.IgnoreCase);
                    while (match.Success)
                    {
                        if (match.Groups.Count >= 2)
                        {
                            for (int i = 1; i < match.Groups.Count; i++)
                            {
                                Group oSingleMatch = match.Groups[i];
                                cIndexes.Add(new string[] {
                                oSingleMatch.Index.ToString(),
                                oSingleMatch.Value.Length.ToString(),
                                oElement.color
                            });
                            }
                        }
                        else
                        {
                            Group oSingleMatch = match.Groups[0];
                            cIndexes.Add(new string[] {
                            oSingleMatch.Index.ToString(),
                            oSingleMatch.Value.Length.ToString(),
                            oElement.color
                        });
                        }

                        match = match.NextMatch();
                    }
                }
            }

            cIndexes = cIndexes.OrderBy(o => int.Parse(o[0])).ToList();

            int iMarker = 0;

            foreach (string[] aFound in cIndexes)
            {
                if (int.Parse(aFound[0]) >= iMarker)
                {
                    cInlines.Add(new Run(sLine.Substring(iMarker, int.Parse(aFound[0]) - iMarker)));

                    cInlines.Add(new Run(sLine.Substring(int.Parse(aFound[0]), int.Parse(aFound[1])))
                    {
                        Foreground = (Brush)new BrushConverter().ConvertFromString(aFound[2])
                    });

                    iMarker = int.Parse(aFound[0]) + int.Parse(aFound[1]);
                }
            }

            cInlines.Add(new Run(sLine.Substring(iMarker)));

            return cInlines;
        }

        private List<Syntax.Scopes> GetSyntaxScopesForExtension(string sExtension)
        {
            List<Syntax.Scopes> cNeededSyntax = new List<Syntax.Scopes>();

            foreach(Syntax oSyntax in cSyntax)
            {
                foreach(string sSyntaxExtension in oSyntax.extensions)
                {
                    if(sSyntaxExtension == sExtension)
                    {
                        foreach(Syntax.Scopes oScope in oSyntax.scopes)
                        {
                            cNeededSyntax.Add(oScope);
                        }
                        break;
                    }
                }
            }

            return cNeededSyntax;
        }
    }

    public class Syntax
    {
        public string lang;
        public string[] extensions;
        public Syntax.Scopes[] scopes;

        public class Scopes
        {
            public string name;
            public string color;
            public string[] patterns;
        }
    }
}
