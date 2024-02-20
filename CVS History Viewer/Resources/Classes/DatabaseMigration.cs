using System.Data.SQLite;

namespace CVS_History_Viewer.Resources.Classes
{
    class DatabaseMigration
    {
        private readonly SQLiteConnection oSQLiteConnection;

        public DatabaseMigration(SQLiteConnection oSQLiteConnection)
        {
            this.oSQLiteConnection = oSQLiteConnection;
        }

        public void CreateOrMigrateAll()
        {
            CreateOrMigrateFilesTable();
            CreateOrMigrateTagsTable();
            CreateOrMigrateCommitsTable();
            CreateOrMigrateCommitTagsTable();
            CreateOrMigrateDiffBlocksTable();
            CreateOrMigrateDiffLinesTable();

            new SQLiteCommand(@"CREATE INDEX IF NOT EXISTS DiffLines_DiffBlockID ON DiffLines (DiffBlockID);"
                  , oSQLiteConnection).ExecuteNonQuery();
            new SQLiteCommand(@"CREATE INDEX IF NOT EXISTS Commit_date_desc ON Commits (Date DESC, HASH ASC);"
                              , oSQLiteConnection).ExecuteNonQuery();
            new SQLiteCommand(@"CREATE INDEX IF NOT EXISTS CommitTags_CommitID ON CommitTags (CommitID, TagID);"
                              , oSQLiteConnection).ExecuteNonQuery();
        }

        private void CreateOrMigrateFilesTable()
        {
            bool bExist = false;
            string sSQL = "";
            using (SQLiteCommand oCmd = new SQLiteCommand("SELECT sql FROM sqlite_master WHERE type='table' AND name='Files';",
                                                          oSQLiteConnection))
            {
                using (SQLiteDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        bExist = true;
                        sSQL = oReader["sql"].ToString();
                    }
                }
            }

            if (!bExist)
            {
                new SQLiteCommand(@"CREATE TABLE Files (
	                                    ID	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	                                    Name	TEXT NOT NULL,
	                                    Path	TEXT NOT NULL,
                                        CVSPath TEXT NOT NULL,
	                                    LastUpdated	DATETIME NOT NULL,
                                        Deleted	INTEGER NOT NULL DEFAULT 0,
                                        Ignored INTEGER NOT NULL DEFAULT 0,
                                        UNIQUE(Name, Path)
                                    );", oSQLiteConnection).ExecuteNonQuery();
            }
            else
            {
                if (!sSQL.ToLower().Contains("ignored integer"))
                {
                    new SQLiteCommand(@"ALTER TABLE Files
                                            ADD Ignored INTEGER DEFAULT (0)
                                            NOT NULL;", oSQLiteConnection).ExecuteNonQuery();
                }
            }
        }

        private void CreateOrMigrateTagsTable()
        {
            bool bExist = false;
            using (SQLiteCommand oCmd = new SQLiteCommand("SELECT sql FROM sqlite_master WHERE type='table' AND name='Tags';",
                                                          oSQLiteConnection))
            {
                using (SQLiteDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        bExist = true;
                    }
                }
            }

            if (!bExist)
            {
                new SQLiteCommand(@"CREATE TABLE Tags (
	                                    ID	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	                                    Label	TEXT NOT NULL UNIQUE
                                    );", oSQLiteConnection).ExecuteNonQuery();
            }
        }

        private void CreateOrMigrateCommitsTable()
        {
            bool bExist = false;
            string sSQL = "";
            using (SQLiteCommand oCmd = new SQLiteCommand("SELECT sql FROM sqlite_master WHERE type='table' AND name='Commits';",
                                                          oSQLiteConnection))
            {
                using (SQLiteDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        bExist = true;
                        sSQL = oReader["sql"].ToString();
                    }
                }
            }

            if (!bExist)
            {
                new SQLiteCommand(@"CREATE TABLE Commits (
	                                    ID	INTEGER NOT NULL,
	                                    Revision	TEXT NOT NULL,
	                                    FileID	INTEGER NOT NULL,
	                                    Description	TEXT NOT NULL,
	                                    Date	DATETIME NOT NULL,
	                                    Author	TEXT NOT NULL,
	                                    State	TEXT NOT NULL,
	                                    LinesChanged	TEXT NOT NULL,
	                                    HASH	TEXT NOT NULL,
                                        ReAdded	INTEGER NOT NULL DEFAULT 0,
                                        Whitespace INTEGER  DEFAULT (0) NOT NULL,
	                                    PRIMARY KEY('FileID','Revision'),
	                                    FOREIGN KEY('FileID') REFERENCES 'Files'('ID')
                                    );", oSQLiteConnection).ExecuteNonQuery();
            }
            else
            {
                if (!sSQL.ToLower().Contains("whitespace integer"))
                {
                    new SQLiteCommand(@"ALTER TABLE Commits
                                            ADD Whitespace INTEGER DEFAULT (0)
                                            NOT NULL;", oSQLiteConnection).ExecuteNonQuery();
                }
            }
        }

        private void CreateOrMigrateCommitTagsTable()
        {
            bool bExist = false;
            using (SQLiteCommand oCmd = new SQLiteCommand("SELECT sql FROM sqlite_master WHERE type='table' AND name='CommitTags';",
                                                          oSQLiteConnection))
            {
                using (SQLiteDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        bExist = true;
                    }
                }
            }

            if (!bExist)
            {
                new SQLiteCommand(@"CREATE TABLE CommitTags (
	                                    TagID	INTEGER NOT NULL,
	                                    CommitID	INTEGER NOT NULL,
	                                    PRIMARY KEY('TagID','CommitID')
                                    );", oSQLiteConnection).ExecuteNonQuery();
            }
        }

        private void CreateOrMigrateDiffBlocksTable()
        {
            bool bExist = false;
            using (SQLiteCommand oCmd = new SQLiteCommand("SELECT sql FROM sqlite_master WHERE type='table' AND name='DiffBlocks';",
                                                          oSQLiteConnection))
            {
                using (SQLiteDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        bExist = true;
                    }
                }
            }

            if (!bExist)
            {
                new SQLiteCommand(@"CREATE TABLE DiffBlocks (
                                    ID  INTEGER PRIMARY KEY AUTOINCREMENT UNIQUE NOT NULL,
                                    CommitID  INTEGER NOT NULL,
                                    StartLine INTEGER NOT NULL,
                                    EndLine   INTEGER NOT NULL
                                );", oSQLiteConnection).ExecuteNonQuery();
            }
        }

        private void CreateOrMigrateDiffLinesTable()
        {
            bool bExist = false;
            using (SQLiteCommand oCmd = new SQLiteCommand("SELECT sql FROM sqlite_master WHERE type='table' AND name='DiffLines';",
                                                          oSQLiteConnection))
            {
                using (SQLiteDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        bExist = true;
                    }
                }
            }

            if (!bExist)
            {
                new SQLiteCommand(@"CREATE TABLE DiffLines (
                                    ID INTEGER PRIMARY KEY AUTOINCREMENT UNIQUE NOT NULL,
                                    DiffBlockID INTEGER NOT NULL,
                                    [Action]    TEXT    NOT NULL,
                                    Line        TEXT    NOT NULL
                                );", oSQLiteConnection).ExecuteNonQuery();
            }
        }
    }
}
