using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CVS_History_Viewer.Resources.Classes
{
    public static class CVSCalls
    {
        private static List<string> RunCommand(string sPath, string sCommand)
        {
            System.Diagnostics.Process oProcess = new System.Diagnostics.Process();

            oProcess.StartInfo.FileName = "cmd.exe";
            oProcess.StartInfo.Arguments = "/C " + sCommand;
            oProcess.StartInfo.UseShellExecute = false;
            oProcess.StartInfo.RedirectStandardOutput = true;
            oProcess.StartInfo.WorkingDirectory = sPath;
            oProcess.StartInfo.CreateNoWindow = true; //hide console window
            oProcess.Start();

            string sOutput = oProcess.StandardOutput.ReadToEnd();

            oProcess.WaitForExit();

            List<string> cLines = new List<string>();

            using (StringReader oReader = new StringReader(sOutput))
            {
                string sLine = string.Empty;
                do
                {
                    sLine = oReader.ReadLine();
                    if (sLine != null)
                    {
                        cLines.Add(sLine);
                    }

                } while (sLine != null);
            }

            return cLines;
        }

        public static List<Commit> GetCommits(CVSFile oFile, List<Tag> cTags)
        {
            List<Commit> cCommits = new List<Commit>();

            //Check if it's an CVS related File; those can be ignored.
            string sTest = oFile.sPath + "\\" + oFile.sName;
            if (sTest.Contains("CVS\\Repository") || sTest.Contains("CVS\\Root") || sTest.Contains("CVS\\Entries") || sTest.Contains("CVS\\Baserev"))
            {
                Console.WriteLine($"Ignored: {oFile.sPath}\\{oFile.sName}");
                return cCommits;
            }

            List<string> cLines = CVSCalls.RunCommand(oFile.sPath, $"cvs log \"{oFile.sName}\"");            

            //If output is just 5 line, it probably means the file is not known in cvs (yet) or some other error.
            if (cLines.Count <= 5)
            {
                Console.WriteLine($"No Data: {oFile.sPath}\\{oFile.sName}");
                return cCommits;
            }

            int iLine = 0;
            List<KeyValuePair<string, string>> cRawTags = new List<KeyValuePair<string, string>>();
            string sRCS = null;
            if (oFile.iID == 0)
            {
                if (cLines[1].Contains("RCS file:"))
                {
                    sRCS = cLines[1].Replace("RCS file: ", "").Replace("/" + oFile.sName + ",v", "");                    
                }
            }           

            //Find begin of symbolic names.
            for (int i = iLine; i < cLines.Count; i++)
            {
                if (cLines[i].Contains("symbolic names:"))
                {
                    iLine = i + 1;
                    break;
                }
            }

            //Process symbolic names (aka tags).
            for (int i = iLine; i < cLines.Count; i++)
            {
                if (cLines[i].Contains("keyword substitution:"))
                {
                    iLine = i + 1;
                    break;
                }

                string[] aRawTag = cLines[i].Split(':');

                KeyValuePair<string, string> oRawTag = new KeyValuePair<string, string>(
                    aRawTag[0].Trim(new char[] { ' ', (char)9 }),
                    aRawTag[1].TrimStart(' ')
                    );

                cRawTags.Add(oRawTag);
            }

            //Find begin of commits
            for (int i = iLine; i < cLines.Count; i++)
            {
                if (cLines[i].Contains("description:"))
                {
                    iLine = i + 1;
                    break;
                }
            }

            //Process commits
            for (int i = iLine; i < cLines.Count; i++)
            {
                Commit oCommit = new Commit();
                Revision oRevision = new Revision();

                oRevision.sRevision = cLines[i + 1].Replace("revision ", "");

                Match oMatch;

                oMatch = new Regex("date: (.*?);").Match(cLines[i + 2]);

                oCommit.dDate = GlobalFunctions.ParseDateTime(oMatch.Groups[1].ToString());

                oCommit.sAuthor = new Regex("author: (.*?);").Match(cLines[i + 2]).Groups[1].ToString();
                oRevision.sState = new Regex("state: (.*?);").Match(cLines[i + 2]).Groups[1].ToString();
                oRevision.sLinesChanged = new Regex("lines: (.*)").Match(cLines[i + 2]).Groups[1].ToString();

                int iDescriptionStart = 3;

                if (cLines[i + 3].Contains("branches:"))
                {
                    iDescriptionStart = 4;
                }

                oCommit.sDescription = cLines[i + iDescriptionStart];

                //Collect additional description lines
                for (int j = i + iDescriptionStart + 1; j < cLines.Count; j++)
                {
                    if (cLines[j] == "----------------------------" ||
                       cLines[j] == "=============================================================================")
                    {
                        i = j - 1;
                        break;
                    }
                    else
                    {
                        oCommit.sDescription += Environment.NewLine + cLines[j];
                    }
                }

                oCommit.sDescription = oCommit.sDescription.Trim();

                //Tags
                foreach (KeyValuePair<string, string> oRawTag in cRawTags)
                {
                    if (oRawTag.Value == oRevision.sRevision)
                    {
                        bool bFound = false;
                        foreach (Tag oTag in cTags)
                        {
                            if (oTag.sLabel == oRawTag.Key)
                            {
                                bFound = true;
                                oRevision.cTags.Add(oTag);
                                break;
                            }
                        }

                        if (!bFound)
                        {
                            Tag oNewTag = new Tag { iID = 0, sLabel = oRawTag.Key };

                            oRevision.cTags.Add(oNewTag);
                            cTags.Add(oNewTag);
                        }
                    }
                }

                //HASH
                using (SHA1Managed sha1 = new SHA1Managed())
                {
                    byte[] aHash = sha1.ComputeHash(Encoding.UTF8.GetBytes(oCommit.sAuthor + oCommit.dDate.ToString() + oCommit.sDescription));
                    StringBuilder oSB = new StringBuilder(aHash.Length * 2);

                    foreach (byte b in aHash)
                    {
                        //X2 = Upper Case
                        oSB.Append(b.ToString("X2"));
                    }

                    oCommit.sHASH = oSB.ToString();
                }

                oRevision.oFile = oFile;

                //Check if this file was re-added to the repository.
                if (cCommits.Count > 0 && !oRevision.bReAdded)
                {
                    if (cCommits[cCommits.Count - 1].cRevisions[0].sState != "dead" && oRevision.sState == "dead")
                    {
                        cCommits[cCommits.Count - 1].cRevisions[0].bReAdded = true;
                    }
                }

                //Finally
                oCommit.cRevisions.Add(oRevision);
                cCommits.Add(oCommit);

                //Check if there is another commit coming or not.
                if (cLines.Count - 1 - i < 5)
                {
                    break;
                }
            }

            //Set CVS Path for File Object
            if (oFile.iID == 0 && !string.IsNullOrWhiteSpace(sRCS))
            {
                if (cCommits[0].cRevisions[0].sState == "dead")
                {
                    sRCS = sRCS.Replace("/Attic", "");
                }

                string[] aSplitTempRCS = sRCS.Split('/');

                List<string> cSplitRCS = new List<string>();
                foreach (string sPart in aSplitTempRCS)
                {
                    if (!string.IsNullOrWhiteSpace(sPart))
                    {
                        string[] aSplitTemp2RCS = sPart.Split('\\');
                        foreach (string sPart2 in aSplitTemp2RCS)
                        {
                            if (!string.IsNullOrWhiteSpace(sPart2))
                            {
                                cSplitRCS.Add(sPart2);
                            }
                        }
                    }
                }

                List<string> cSplitFile = new List<string>(oFile.sPath.Split('\\'));

                int iCount = 0;
                if (cSplitRCS.Count < cSplitFile.Count)
                {
                    iCount = cSplitRCS.Count;
                }
                else
                {
                    iCount = cSplitFile.Count;
                }

                int iRCSIndex = cSplitRCS.Count - 1;
                int iFileIndex = cSplitFile.Count - 1;

                List<string> cMatches = new List<string>();
                for (int g = iCount - 1; g >= 0; g--)
                {
                    if (cSplitRCS[iRCSIndex] == cSplitFile[iFileIndex--])
                    {
                        cMatches.Add(cSplitRCS[iRCSIndex--]);
                    }
                    else
                    {
                        break;
                    }
                }

                for (int g = cMatches.Count - 1; g >= 0; g--)
                {
                    oFile.sCVSPath += cMatches[g] + ((g != 0) ? "/" : "");
                }
            }

            return cCommits;
        }

        public static Revision GetDiff(Revision oRevision)
        {
            if (oRevision.sRevision.EndsWith(".1") || oRevision.bReAdded || oRevision.sState == "dead")
            {
                oRevision = CVSCalls.GetBaseVersion(oRevision);
            }
            else
            {
                oRevision = CVSCalls.GetDiffPrevious(oRevision);
            }
            
            return oRevision;
        }

        private static Revision GetDiffPrevious(Revision oRevision)
        {
            int iPrevRevision = int.Parse(oRevision.sRevision.Substring(oRevision.sRevision.LastIndexOf('.') + 1)) - 1;
            string sPrevRevision = oRevision.sRevision.Substring(0, oRevision.sRevision.LastIndexOf('.') + 1) + iPrevRevision.ToString();
             
            List<string> cLines = CVSCalls.RunCommand(oRevision.oFile.sPath, $"cvs diff -r {oRevision.sRevision} -r {sPrevRevision} \"{oRevision.oFile.sName}\"");

            DiffBlock oDiffBlock = new DiffBlock();
            string sBlockKind = "";
            for(int i = 6; i < cLines.Count; i++)
            {
                DiffBlock.LineChange oChange = new DiffBlock.LineChange();

                if (string.IsNullOrEmpty(cLines[i]))
                {
                    //Do nothing.
                }else if (cLines[i].Substring(0,1) == "<")
                {
                    oChange.sAction = "+";
                    oChange.sLine = cLines[i].Substring(1, cLines[i].Length - 1);
                    oDiffBlock.cLines.Add(oChange);
                }else if (cLines[i].Substring(0, 1) == ">")
                {
                    switch (sBlockKind)
                    {
                        case "a":
                            oChange.sAction = "-";
                            break;
                        case "d":
                            oChange.sAction = "+";
                            break;
                        default:
                            oChange.sAction = "-";
                            break;
                    }

                    oChange.sLine = cLines[i].Substring(1, cLines[i].Length - 1);
                    oDiffBlock.cLines.Add(oChange);

                }
                else if(cLines[i] == "---" || cLines[i].Substring(0, 1) == "\\")
                {
                    //Do Nothing
                }
                else
                {
                    Match oMatch = new Regex("([0-9]+),{0,1}([0-9]*)([adc]){1}").Match(cLines[i]);

                    if(oDiffBlock.cLines.Count != 0)
                    {
                        oRevision.cDiffBlocks.Add(oDiffBlock);
                    }
                    oDiffBlock = new DiffBlock();
                    oDiffBlock.iStartLine = int.Parse(oMatch.Groups[1].Value);
                    oDiffBlock.iEndLine = int.Parse((!string.IsNullOrWhiteSpace(oMatch.Groups[2].Value))? oMatch.Groups[2].Value : oMatch.Groups[1].Value);
                    sBlockKind = oMatch.Groups[3].Value;
                }
            }

            oRevision.cDiffBlocks.Add(oDiffBlock);

            return oRevision;
        }

        private static Revision GetBaseVersion(Revision oRevision)
        {
            List<string> cLines = new List<string>();

            if (oRevision.sState != "dead")
            {
                cLines = CVSCalls.RunCommand(oRevision.oFile.sPath, $"cvs co -r {oRevision.sRevision} -p \"{oRevision.oFile.sCVSPath}/{oRevision.oFile.sName}\"");
            }
            else
            {
                int iPrevRevision = int.Parse(oRevision.sRevision.Substring(oRevision.sRevision.LastIndexOf('.') + 1)) - 1;
                string sPrevRevision = oRevision.sRevision.Substring(0, oRevision.sRevision.LastIndexOf('.') + 1) + iPrevRevision.ToString();
                cLines = CVSCalls.RunCommand(oRevision.oFile.sPath, $"cvs co -r {sPrevRevision} -p \"{oRevision.oFile.sCVSPath}/{oRevision.oFile.sName}\"");
            }            

            DiffBlock oDiffBlock = new DiffBlock();

            oDiffBlock.iStartLine = (cLines.Count == 0) ? 0 : 1;
            oDiffBlock.iEndLine = cLines.Count;

            List<DiffBlock.LineChange> cChanges = new List<DiffBlock.LineChange>();
            for(int i = 0; i < cLines.Count; i++)
            {
                DiffBlock.LineChange oLineChange = new DiffBlock.LineChange()
                {
                    sAction = (oRevision.sState != "dead")? "+" : "-",
                    sLine = cLines[i]
                };
                cChanges.Add(oLineChange);
            }

            oDiffBlock.cLines = cChanges;
            oRevision.cDiffBlocks.Add(oDiffBlock);

            return oRevision;
        }
    }
}
