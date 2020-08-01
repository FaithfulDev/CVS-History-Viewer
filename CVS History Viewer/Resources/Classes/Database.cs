using System;
using System.Data.SQLite;
using System.Collections.Generic;

namespace CVS_History_Viewer.Resources.Classes
{
    class Database
    {
        private readonly string sDatabasePath;

        private SQLiteConnection NewSQLConnection()
        {
            return new SQLiteConnection($"Data Source={sDatabasePath}\\CVS History Viewer.db;Version=3;foreign keys=true;");
        }

        /// <summary>
        /// Initialize a new database. Folders and database will be created if they not already exist.
        /// </summary>
        /// <param name="sDatabasePath">Path where the database is supposed to be saved.</param>
        public Database(string sDatabasePath)
        {
            this.sDatabasePath = sDatabasePath;
            System.IO.Directory.CreateDirectory(sDatabasePath);

            using(SQLiteConnection oSQLiteConnection = NewSQLConnection())
            {
                oSQLiteConnection.Open();
                new DatabaseMigration(oSQLiteConnection).CreateOrMigrateAll();
                oSQLiteConnection.Close();
            }
        }

        public List<CVSFile> GetFiles(string sRootDirectory)
        {
            List<CVSFile> cFiles = new List<CVSFile>();

            using (SQLiteConnection oSQLiteConnection = NewSQLConnection())
            {
                oSQLiteConnection.Open();

                CVSFile oFile;
                using (SQLiteCommand oCmd = new SQLiteCommand($"SELECT * FROM Files WHERE Path LIKE '{sRootDirectory.Replace("'", @"''")}%';",
                                                              oSQLiteConnection))
                {
                    using (SQLiteDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            oFile = new CVSFile
                            {
                                iID = int.Parse(oReader["ID"].ToString()),
                                sName = oReader["Name"].ToString(),
                                sPath = oReader["Path"].ToString(),
                                sCVSPath = oReader["CVSPath"].ToString(),
                                dLastUpdated = (DateTime)oReader["LastUpdated"],
                                bDeleted = int.Parse(oReader["Deleted"].ToString()) == 1,
                                bIgnored = int.Parse(oReader["Ignored"].ToString()) == 1
                            };

                            cFiles.Add(oFile);
                        }
                    }
                }

                oSQLiteConnection.Close();
            }           

            return cFiles;
        }

        public List<Tag> GetTags()
        {
            List<Tag> cTags = new List<Tag>();

            using (SQLiteConnection oSQLiteConnection = NewSQLConnection())
            {
                oSQLiteConnection.Open();

                Tag oTag;
                using (SQLiteCommand oCmd = new SQLiteCommand("SELECT * FROM Tags;", oSQLiteConnection))
                {
                    using (SQLiteDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            oTag = new Tag
                            {
                                iID = int.Parse(oReader["ID"].ToString()),
                                sLabel = oReader["Label"].ToString()
                            };

                            cTags.Add(oTag);
                        }
                    }
                }

                oSQLiteConnection.Close();
            }            

            return cTags;
        }

        public List<CommitTag> GetCommitTags(List<Tag> cTags)
        {
            List<CommitTag> cCommitTags = new List<CommitTag>();

            using (SQLiteConnection oSQLiteConnection = NewSQLConnection())
            {
                oSQLiteConnection.Open();

                CommitTag oCommitTag;
                using (SQLiteCommand oCmd = new SQLiteCommand("SELECT * FROM CommitTags;", oSQLiteConnection))
                {
                    using (SQLiteDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            oCommitTag = new CommitTag
                            {
                                iCommitID = int.Parse(oReader["CommitID"].ToString())
                            };

                            foreach (Tag oTag in cTags)
                            {
                                if (oTag.iID == int.Parse(oReader["TagID"].ToString()))
                                {
                                    oCommitTag.oTag = oTag;
                                    break;
                                }
                            }

                            cCommitTags.Add(oCommitTag);
                        }
                    }
                }

                oSQLiteConnection.Close();
            }            

            return cCommitTags;
        }

        public List<Commit> GetCommits(string sRootDirectory, string sFilter, string sLimit, List<CommitTag> cCommitTags)
        {
            List<Commit> cCommits = new List<Commit>();

            using (SQLiteConnection oSQLiteConnection = NewSQLConnection())
            {
                oSQLiteConnection.Open();

                string sSQL = $@"SELECT Commits.*, Files.Name FileName, Files.Path FilePath, Files.LastUpdated FileLastUpdated, Files.CVSPath FileCVSPath, 
                                    (SELECT IFNULL(count(TagID),0) FROM CommitTags WHERE CommitID = Commits.ID) HasTags 
                             FROM Commits
                             LEFT JOIN Files ON Files.ID = Commits.FileID
                             WHERE Commits.HASH IN (
                                     SELECT DISTINCT Commits.HASH 
                                     FROM Commits
                                     LEFT JOIN Files ON Files.ID = Commits.FileID
                                     WHERE Path LIKE '{sRootDirectory.Replace("'", @"''")}%' {sFilter}
                                     ORDER BY Date DESC, HASH ASC
                                     {sLimit}
                                )
                             ORDER BY Date DESC, HASH ASC;";

                Commit oCommit;
                Revision oRevision;
                string sPrevHash = null;
                using (SQLiteCommand oCmd = new SQLiteCommand(sSQL, oSQLiteConnection))
                {
                    using (SQLiteDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            if (sPrevHash != oReader["HASH"].ToString())
                            {
                                oCommit = new Commit
                                {
                                    iID = int.Parse(oReader["ID"].ToString()),
                                    dDate = (DateTime)oReader["Date"],
                                    dLocalDate = ((DateTime)oReader["Date"]).ToLocalTime(),
                                    sAuthor = oReader["Author"].ToString(),
                                    sDescription = oReader["Description"].ToString(),
                                    sHASH = oReader["HASH"].ToString(),
                                    sShortHASH = oReader["HASH"].ToString().Substring(0, 7),
                                    sDescriptionTable = new System.IO.StringReader(oReader["Description"].ToString()).ReadLine()
                                };
                            }
                            else
                            {
                                oCommit = cCommits[cCommits.Count - 1];
                            }

                            oRevision = new Revision
                            {
                                iCommitID = int.Parse(oReader["ID"].ToString()),
                                sLinesChanged = oReader["LinesChanged"].ToString(),
                                sRevision = oReader["Revision"].ToString(),
                                sState = oReader["State"].ToString(),
                                bReAdded = int.Parse(oReader["ReAdded"].ToString()) == 1,
                                oFile = new CVSFile()
                                {
                                    iID = int.Parse(oReader["FileID"].ToString()),
                                    sName = oReader["FileName"].ToString(),
                                    sPath = oReader["FilePath"].ToString(),
                                    sCVSPath = oReader["FileCVSPath"].ToString(),
                                    dLastUpdated = (DateTime)oReader["FileLastUpdated"]
                                },
                                iWhitespace = int.Parse(oReader["Whitespace"].ToString())
                            };

                            string sSQL2 = $@"SELECT DiffBlocks.*, DiffLines.'Action', DiffLines.Line FROM DiffBlocks
                                          LEFT JOIN DiffLines ON DiffLines.DiffBlockID = DiffBlocks.ID
                                          WHERE DiffBlocks.CommitID = {oReader["ID"]}
                                          ORDER BY DiffBlocks.ID, DiffLines.ID;";

                            using (SQLiteCommand oCmd2 = new SQLiteCommand(sSQL2, oSQLiteConnection))
                            {
                                string sPrevBlockID = null;
                                DiffBlock oDiffBlock = new DiffBlock();
                                DiffBlock.LineChange oLineChange;
                                using (SQLiteDataReader oReader2 = oCmd2.ExecuteReader())
                                {
                                    while (oReader2.Read())
                                    {
                                        if (sPrevBlockID != oReader2["ID"].ToString())
                                        {
                                            oDiffBlock = new DiffBlock
                                            {
                                                iStartLine = int.Parse(oReader2["StartLine"].ToString()),
                                                iEndLine = int.Parse(oReader2["EndLine"].ToString())
                                            };

                                            sPrevBlockID = oReader2["ID"].ToString();
                                            oRevision.cDiffBlocks.Add(oDiffBlock);
                                        }

                                        if (!string.IsNullOrWhiteSpace(oReader2["Action"].ToString()))
                                        {
                                            oLineChange = new DiffBlock.LineChange
                                            {
                                                sAction = oReader2["Action"].ToString(),
                                                sLine = oReader2["Line"].ToString()
                                            };

                                            oDiffBlock.cLines.Add(oLineChange);
                                        }
                                    }
                                }
                            }

                            if (oReader["HasTags"].ToString() != "0")
                            {
                                oRevision.cTags = GetTagsForCommit(int.Parse(oReader["ID"].ToString()), cCommitTags);
                            }

                            oCommit.cRevisions.Add(oRevision);

                            if (sPrevHash != oReader["HASH"].ToString())
                            {
                                cCommits.Add(oCommit);
                                sPrevHash = oReader["HASH"].ToString();
                            }
                        }
                    }
                }

                oSQLiteConnection.Close();
            }            

            return cCommits;
        }

        private List<Tag> GetTagsForCommit(int iCommitID, List<CommitTag> cCommitTags)
        {
            List<Tag> cTags = new List<Tag>();

            foreach(CommitTag oCommitTag in cCommitTags)
            {
                if(oCommitTag.iCommitID == iCommitID)
                {
                    cTags.Add(oCommitTag.oTag);
                }
            }

            return cTags;
        }

        /// <summary>
        /// Save a list of commits to the database. This will also create/update all sub-parts (files, tags, etc.).
        /// This method is supposed to be used after getting the commits for a single file and should only be used for that as it
        /// will only update the file referenced in the first commit.
        /// </summary>
        /// <param name="cCommits">List of Commits that need to be saved.</param>
        public void SaveCommits(List<Commit> cCommits, List<Tag> cTags)
        {
            using (SQLiteConnection oSQLiteConnection = NewSQLConnection())
            {
                oSQLiteConnection.Open();

                string sSQL;

                //Save File
                if (cCommits[0].cRevisions[0].oFile.iID == 0)
                {
                    string sCVSPath = (cCommits[0].cRevisions[0].oFile.sCVSPath != null) ? cCommits[0].cRevisions[0].oFile.sCVSPath.Replace("'", @"''") : "";

                    sSQL = $@"INSERT INTO Files (ID, Name, Path, CVSPath, LastUpdated, Deleted, Ignored) VALUES
                              (Null,
                              '{cCommits[0].cRevisions[0].oFile.sName.Replace("'", @"''")}', 
                              '{cCommits[0].cRevisions[0].oFile.sPath.Replace("'", @"''")}', 
                              '{sCVSPath}', 
                              '{cCommits[0].cRevisions[0].oFile.dLastUpdated:yyyy-MM-dd HH:mm:ss}',
                               {(cCommits[0].cRevisions[0].oFile.bDeleted ? 1 : 0)},
                               {(cCommits[0].cRevisions[0].oFile.bIgnored ? 1 : 0)} );";

                    new SQLiteCommand(sSQL, oSQLiteConnection).ExecuteNonQuery();

                    cCommits[0].cRevisions[0].oFile.iID = (int)oSQLiteConnection.LastInsertRowId;
                }
                else
                {
                    sSQL = $@"UPDATE Files
                              SET LastUpdated = '{cCommits[0].cRevisions[0].oFile.dLastUpdated:yyyy-MM-dd HH:mm:ss}', 
                                  Deleted = {(cCommits[0].cRevisions[0].oFile.bDeleted ? 1 : 0)},
                                  Ignored = {(cCommits[0].cRevisions[0].oFile.bIgnored ? 1 : 0)}
                              WHERE ID = {cCommits[0].cRevisions[0].oFile.iID};";

                    new SQLiteCommand(sSQL, oSQLiteConnection).ExecuteNonQuery();
                }

                foreach (Commit oCommit in cCommits)
                {
                    foreach (Revision oRevision in oCommit.cRevisions)
                    {
                        int iCommitID = 0;

                        //Save Commit (aka Revisions)
                        sSQL = $@"INSERT OR IGNORE INTO Commits VALUES (
                                (SELECT IFNULL(max(id),0) + 1 FROM Commits),
                                '{oRevision.sRevision}',
                                {cCommits[0].cRevisions[0].oFile.iID},
                                '{oCommit.sDescription.Replace("'", @"''")}',
                                '{oCommit.dDate:yyyy-MM-dd HH:mm:ss}',
                                '{oCommit.sAuthor}',
                                '{oRevision.sState}',
                                '{oRevision.sLinesChanged}',
                                '{oCommit.sHASH}',
                                {(oRevision.bReAdded ? 1 : 0)},
                                {oRevision.iWhitespace}
                            )";

                        new SQLiteCommand(sSQL, oSQLiteConnection).ExecuteNonQuery();


                        SQLiteDataReader oSQLiteReader = new SQLiteCommand($@"SELECT * FROM Commits WHERE Revision = '{oRevision.sRevision}' 
                                                                          AND FileID = {cCommits[0].cRevisions[0].oFile.iID} ;",
                                                                    oSQLiteConnection).ExecuteReader();

                        while (oSQLiteReader.Read())
                        {
                            iCommitID = int.Parse(oSQLiteReader["ID"].ToString());
                        }

                        //Save Tags + CommitTags (Tag+Commit combination)
                        foreach (Tag oTag in oRevision.cTags)
                        {
                            if (oTag.iID == 0)
                            {
                                sSQL = $@"INSERT INTO Tags (ID, Label) VALUES
                              (Null, '{oTag.sLabel.Replace("'", @"''")}');";

                                new SQLiteCommand(sSQL, oSQLiteConnection).ExecuteNonQuery();

                                oTag.iID = (int)oSQLiteConnection.LastInsertRowId;
                            }

                            sSQL = $@"INSERT OR IGNORE INTO CommitTags (TagID, CommitID) VALUES
                              ({oTag.iID}, '{iCommitID}');";

                            new SQLiteCommand(sSQL, oSQLiteConnection).ExecuteNonQuery();
                        }
                    }
                }

                oSQLiteConnection.Close();
            }            
        }

        public void SaveFile(CVSFile oFile)
        {
            using (SQLiteConnection oSQLiteConnection = NewSQLConnection())
            {
                oSQLiteConnection.Open();

                //Save File
                if (oFile.iID == 0)
                {
                    string sSQL = $@"INSERT INTO Files (ID, Name, Path, CVSPath, LastUpdated, Deleted, Ignored) VALUES
                              (Null,
                              '{oFile.sName.Replace("'", @"''")}', 
                              '{oFile.sPath.Replace("'", @"''")}', 
                              '{((oFile.sCVSPath != null) ? oFile.sCVSPath.Replace("'", @"''") : "")}',
                              '{oFile.dLastUpdated:yyyy-MM-dd HH:mm:ss}',
                               {(oFile.bDeleted ? 1 : 0)},
                               {(oFile.bIgnored ? 1 : 0)});";

                    new SQLiteCommand(sSQL, oSQLiteConnection).ExecuteNonQuery();

                    oFile.iID = (int)oSQLiteConnection.LastInsertRowId;
                }
                else
                {
                    string sSQL = $@"UPDATE Files
                              SET LastUpdated = '{oFile.dLastUpdated:yyyy-MM-dd HH:mm:ss}', Deleted = {(oFile.bDeleted ? 1 : 0)},
                              Ignored = {(oFile.bIgnored ? 1 : 0)}
                              WHERE ID = {oFile.iID};";

                    new SQLiteCommand(sSQL, oSQLiteConnection).ExecuteNonQuery();
                }

                oSQLiteConnection.Close();
            }            
        }

        public void SaveDiff(Revision oRevision)
        {
            using (SQLiteConnection oSQLiteConnection = NewSQLConnection())
            {
                oSQLiteConnection.Open();

                new SQLiteCommand($@"UPDATE Commits SET Whitespace = {oRevision.iWhitespace} WHERE ID = {oRevision.iCommitID};", 
                    oSQLiteConnection).ExecuteNonQuery();

                foreach (DiffBlock oDiffBlock in oRevision.cDiffBlocks)
                {
                    string sSQL = $@"INSERT INTO DiffBlocks (CommitID, StartLine, EndLine) VALUES (
                                {oRevision.iCommitID},
                                {oDiffBlock.iStartLine},
                                {oDiffBlock.iEndLine}
                                )";

                    new SQLiteCommand(sSQL, oSQLiteConnection).ExecuteNonQuery();

                    int iBlockID = (int)oSQLiteConnection.LastInsertRowId;

                    foreach (DiffBlock.LineChange oLine in oDiffBlock.cLines)
                    {
                        sSQL = $@"INSERT INTO DiffLines (DiffBlockID, Action, Line) VALUES (
                             {iBlockID},
                             '{oLine.sAction}',
                             '{oLine.sLine.Replace("'", @"''")}'
                            )";

                        new SQLiteCommand(sSQL, oSQLiteConnection).ExecuteNonQuery();
                    }
                }

                oSQLiteConnection.Close();
            }            
        }

        public void DeleteDiffLines(Revision oRevision)
        {
            using (SQLiteConnection oSQLiteConnection = NewSQLConnection())
            {
                oSQLiteConnection.Open();
                
                string sSQL = $@"DELETE FROM DiffLines
                                 WHERE DiffBlockID IN (SELECT ID FROM DiffBlocks WHERE CommitID = {oRevision.iCommitID});";

                new SQLiteCommand(sSQL, oSQLiteConnection).ExecuteNonQuery();

                sSQL = $@"DELETE FROM DiffBlocks WHERE CommitID = {oRevision.iCommitID};";

                new SQLiteCommand(sSQL, oSQLiteConnection).ExecuteNonQuery();

                oSQLiteConnection.Close();
            }
        }

        public int GetRevisionCount(CVSFile oFile)
        {

            int iCount = 0;

            if(oFile.iID == 0)
            {
                return iCount;
            }

            using(SQLiteConnection oSQLiteConnection = NewSQLConnection())
            {
                oSQLiteConnection.Open();

                using (SQLiteCommand oCmd = new SQLiteCommand($"SELECT count(id) as count FROM Commits WHERE FileID = {oFile.iID};",
                                                              oSQLiteConnection))
                {
                    using (SQLiteDataReader oReader = oCmd.ExecuteReader())
                    {
                        if (oReader.Read())
                        {
                            iCount = int.Parse(oReader["count"].ToString());
                        }
                    }
                }

                oSQLiteConnection.Close();
            }

            return iCount;

        }
    }
}
