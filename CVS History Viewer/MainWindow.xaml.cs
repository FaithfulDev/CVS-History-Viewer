using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Text.RegularExpressions;
using CVS_History_Viewer.Resources.Classes;
using CVS_History_Viewer.Resources.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Media;
using System.Windows.Threading;

namespace CVS_History_Viewer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string sAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\CVS History Viewer";
        private readonly string sSyntaxPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\Resources\\Syntax";

        private readonly Database oDatabase;
        private readonly Settings oSettings;
        private readonly BackgroundStuff oBackgroundStuff;
        private readonly SyntaxHighlighting oSyntaxHighlighting;

        private readonly BackgroundWorker oUpdateCommitsWorker;

        private readonly Dictionary<string, int> PreviousRevisionSelection = new Dictionary<string, int>();

        private readonly DispatcherTimer oDiffFetchDelay = new DispatcherTimer();

        private List<CVSFile> cFiles = new List<CVSFile>();
        private List<Tag> cTags = new List<Tag>();
        private List<CommitTag> cCommitTags = new List<CommitTag>();
        private List<Commit> cCommits = new List<Commit>();

        private bool bIssueOnLoad = true;

        public MainWindow()
        {
            InitializeComponent();

            Application.Current.DispatcherUnhandledException += GlobalFunctions.CrashHandler;

            oDatabase = new Database(sAppDataPath);
            oSettings = new Settings(sAppDataPath);
            oSyntaxHighlighting = new SyntaxHighlighting(sSyntaxPath);

            oBackgroundStuff = new BackgroundStuff(oDatabase);
            oBackgroundStuff.OnDiffCompleted += BackgroundStuff_DiffCompleted;

            oUpdateCommitsWorker = new BackgroundWorker();

            oUpdateCommitsWorker.DoWork += UpdateCommits_BackgroundWorker_DoWork;
            oUpdateCommitsWorker.ProgressChanged += UpdateCommits_BackgroundWorker_ProgressChanged;
            oUpdateCommitsWorker.RunWorkerCompleted += UpdateCommits_BackgroundWorker_Completed;

            oUpdateCommitsWorker.WorkerSupportsCancellation = true;
            oUpdateCommitsWorker.WorkerReportsProgress = true;

            oDiffFetchDelay.Interval = new TimeSpan(0, 0, 0, 0, 600 );
            oDiffFetchDelay.Tick += oDiffFetchDelay_Tick;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
        }

        private void Normalize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Check if CVS is available
            if (!Environment.GetEnvironmentVariable("path").ToLower().Contains("cvs"))
            {
                //Show CVS Error Message
                ShowCVSMissing();
                return;
            }

            //Check previous directory
            if (oSettings.sRootDirectory == null)
            {
                //Show "Welcome" Dialog and ask user to select a directory
                ShowWelcome();
                return;
            }

            bIssueOnLoad = false;
            ShowLoadingData();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (bIssueOnLoad)
            {
                return;
            }

            LoadFromDB();

            //Create & Show UI Commit list
            this.uiCommits.ItemsSource = cCommits;
            this.uiCommits.SelectedIndex = 0;

            HideLoadingData();
        }

        private void LoadFromDB(string sSearch = " AND 1 = 1", string sLimit = " LIMIT 220")
        {
            // * File List (associated with root directory).
            cFiles = oDatabase.GetFiles(oSettings.sRootDirectory);

            // * Tag List
            cTags = oDatabase.GetTags();

            // * CommitTags
            cCommitTags = oDatabase.GetCommitTags(cTags);

            // * Commits (associated with root directory).
            cCommits = oDatabase.GetCommits(oSettings.sRootDirectory, sSearch, sLimit, cCommitTags);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(oSettings.sRootDirectory))
            {
                return;
            }

            this.uiUpdateState.Content = "Initializing";
            this.uiUpdateCounter.Content = "0 of 0";
            this.uiUpdateProgress.Value = 0;

            this.uiOverlay.Visibility = Visibility.Visible;
            this.uiProgress.Visibility = Visibility.Visible;

            oUpdateCommitsWorker.RunWorkerAsync();
        }

        private void UpdateCommits_BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<CVSFile> cOutDatedFiles = new List<CVSFile>();
            List<CVSFile> cTempFileList = cFiles;
            UpdateProgress oProgress = new UpdateProgress();

            //Get List of all Files in the root
            IEnumerable<FileInfo> cFileInfos = new DirectoryInfo(oSettings.sRootDirectory).EnumerateFiles("*", SearchOption.AllDirectories);

            //Compare with internal file list
            //Identify
            // * New Files
            // * Updated Files

            //Fetch CVS commits for each identified file.
            foreach (FileInfo oFileInfo in cFileInfos)
            {
                bool bFound = false;
                for(int i = cTempFileList.Count - 1; i >= 0; i--)
                {
                    if (cTempFileList[i].sName.ToLower() == oFileInfo.Name.ToLower() && cTempFileList[i].sPath.ToLower() == oFileInfo.DirectoryName.ToLower())
                    {
                        bFound = true;

                        if (cTempFileList[i].dLastUpdated < oFileInfo.LastWriteTime.AddTicks(-oFileInfo.LastWriteTime.Ticks % TimeSpan.TicksPerSecond)
                            && !cTempFileList[i].bIgnored)
                        {
                            cTempFileList[i].dLastUpdated = oFileInfo.LastWriteTime;
                            cTempFileList[i].bDeleted = false;
                            cOutDatedFiles.Add(cTempFileList[i]);
                            oProgress.iTotal = cOutDatedFiles.Count;
                            oUpdateCommitsWorker.ReportProgress(0, oProgress);                            
                        }

                        cTempFileList.RemoveAt(i);
                        break;
                    }

                    if (oUpdateCommitsWorker.CancellationPending)
                    {
                        return;
                    }
                }

                if (!bFound)
                {
                    cOutDatedFiles.Add(new CVSFile { iID = 0, sName = oFileInfo.Name, sPath = oFileInfo.DirectoryName, dLastUpdated = oFileInfo.LastWriteTime });
                    oProgress.iTotal = cOutDatedFiles.Count;
                    oUpdateCommitsWorker.ReportProgress(0, oProgress);
                }
            }

            //Look for newly deleted files (files known to the DB, not already marked as deleted, but not findable in the directory)
            foreach (CVSFile oFile in cTempFileList)
            {
                if (!oFile.bDeleted)
                {
                    if (!File.Exists(oFile.sPath + "\\" + oFile.sName))
                    {
                        //For future checks the file will be marked as deleted...
                        oFile.bDeleted = true;
                        //...but it does get one last check.
                        cOutDatedFiles.Add(oFile);
                        oProgress.iTotal = cOutDatedFiles.Count;
                        oUpdateCommitsWorker.ReportProgress(0, oProgress);
                    }
                }
            }            

            oProgress.iTotal = cOutDatedFiles.Count;

            foreach (CVSFile oFile in cOutDatedFiles)
            {
                FetchNewCommitsFromCVS(oFile);

                if (oUpdateCommitsWorker.CancellationPending)
                {
                    return;
                }
                
                oProgress.iDone += 1;
                oUpdateCommitsWorker.ReportProgress(0, oProgress);
            }                    
        }

        private void UpdateCommits_BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            UpdateProgress oProgress = (UpdateProgress)e.UserState;
            this.uiUpdateCounter.Content = $"{oProgress.iDone} of {oProgress.iTotal}";
            this.uiUpdateProgress.Value = Math.Round((double)oProgress.iDone * 100 / oProgress.iTotal, 0);

            if(oProgress.iDone == 1)
            {
                this.uiUpdateState.Content = "Updating";
            }
        }

        private void UpdateCommits_BackgroundWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Error == null)
            {
                //Reload UI Commit List
                LoadFromDB();
                this.uiCommits.ItemsSource = cCommits;
                this.uiCommits.Items.Refresh();
                this.uiCommits.SelectedIndex = 0;                               
            }
            else
            {
                new CrashReport(e.Error).ShowDialog();
            }

            this.uiProgress.Visibility = Visibility.Collapsed;
            this.uiOverlay.Visibility = Visibility.Collapsed;
            this.uiCancelUpdate.IsEnabled = true;
        }

        private void FetchNewCommitsFromCVS(CVSFile oFile)
        {
            if (!Directory.Exists(oFile.sPath))
            {
                //This file is in a directory that was deleted. We can't get to it's commits anymore.
                //File is already marked as "deleted", update in DB to prevent further checks.
                oDatabase.SaveFile(oFile);
                return;
            }

            List<Commit> cNewCommits = CVSCalls.GetCommits(oFile, cTags);

            if((cNewCommits.Count == 0 || oDatabase.GetRevisionCount(oFile) == cNewCommits.Count) && !oFile.bIgnored)
            {
                //This file has no (new) commits, maybe they are still pending. Skip saving this to the database.
                //Next refresh might show a different result.
                return;
            }else if(oFile.bIgnored)
            {
                //This file needs to be marked as ignored, to not be checked again next time.
                oDatabase.SaveFile(oFile);
                return;
            }

            //Save File (or update "LastChanged"), tags and commits.
            oDatabase.SaveCommits(cNewCommits, cTags);
        }

        private void ChooseDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var oDialog = new CommonOpenFileDialog())
            {
                oDialog.IsFolderPicker = true;
                oDialog.InitialDirectory = oSettings.sRootDirectory;
                
                if(oDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    oSettings.sRootDirectory = oDialog.FileName;
                    oSettings.SaveSettings();

                    //Refresh internal file list with the new root directory.
                    cFiles = oDatabase.GetFiles(oSettings.sRootDirectory);

                    //Scan and show commits.
                    Refresh_Click(null, null);
                }
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            Search(this.uiSearchText.Text.ToLower().Replace('*', '%') + " ");            
        }

        private void Search(string sText, bool bFolowUpSearch = false)
        {
            string sOriginalText = sText;

            MatchCollection cMatches;
            List<KeyValuePair<string, object>> cPairs = new List<KeyValuePair<string, object>>();

            Dictionary<string, string> cPatterns = new Dictionary<string, string>
            {
                { "files.name", "file:[ ]{0,1}(.+?)[ ]{1}" },
                { "author", "author:[ ]{0,1}(.+?)[ ]{1}" },
                { "hash", "commit:[ ]{0,1}(.+?)[ ]{1}" },
                { "date", "date:[ ]{0,1}(.+?)[ ]{1}" },
                { "from", "from:[ ]{0,1}(.+?)[ ]{1}" },
                { "to", "to:[ ]{0,1}(.+?)[ ]{1}" },
                { "limit", "limit:[ ]{0,1}([0-9]+?)[ ]{1}" }
            };

            foreach (KeyValuePair<string, string> oPattern in cPatterns)
            {
                cMatches = new Regex(oPattern.Value).Matches(sText);

                foreach (Match oMatch in cMatches)
                {
                    if (oMatch.Groups[1].ToString() != "")
                    {
                        if (oPattern.Key == "date" || oPattern.Key == "from" || oPattern.Key == "to")
                        {
                            try
                            {
                                cPairs.Add(new KeyValuePair<string, object>(oPattern.Key, DateTime.Parse(oMatch.Groups[1].ToString())));
                            }
                            catch (Exception)
                            {
                                //do nothing - just a bad date.
                            }
                        }
                        else
                        {
                            cPairs.Add(new KeyValuePair<string, object>(oPattern.Key, oMatch.Groups[1].ToString()));
                        }

                        sText = sText.Replace(oMatch.Value, "");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(sText))
            {
                cPairs.Add(new KeyValuePair<string, object>("description", "%" + sText.Trim(' ') + "%"));
            }

            string sWhere = "";
            string sLimit = "";

            foreach (KeyValuePair<string, object> oPair in cPairs)
            {
                switch (oPair.Key)
                {
                    case "date":
                        sWhere += $" AND strftime('%Y-%m-%d', date) = '{((DateTime)oPair.Value).ToString("yyyy-MM-dd")}'";
                        break;
                    case "from":
                        sWhere += $" AND strftime('%Y-%m-%d', date) >= '{((DateTime)oPair.Value).ToString("yyyy-MM-dd")}'";
                        break;
                    case "to":
                        sWhere += $" AND strftime('%Y-%m-%d', date) <= '{((DateTime)oPair.Value).ToString("yyyy-MM-dd")}'";
                        break;
                    case "description":
                        sWhere += $" AND {oPair.Key} LIKE '{oPair.Value.ToString().Replace("'", @"''")}'";
                        break;
                    case "limit":
                        sLimit = " LIMIT " + oPair.Value;
                        break;
                }
            }

            //Author, filename and hash need to be OR connected
            Dictionary<string, string> cSubWheres = new Dictionary<string, string>
            {
                { "files.name", "" },
                { "author", "" },
                { "hash", "" }
            };

            foreach (KeyValuePair<string, object> oPair in cPairs)
            {
                switch (oPair.Key)
                {
                    case "files.name":
                    case "author":
                    case "hash":
                        if (cSubWheres[oPair.Key] != "")
                        {
                            cSubWheres[oPair.Key] += $" OR {oPair.Key} LIKE '{oPair.Value.ToString().Replace("'", @"''")}{((oPair.Key == "hash") ? "%" : "")}'";
                        }
                        else
                        {
                            cSubWheres[oPair.Key] += $"{oPair.Key} LIKE '{oPair.Value.ToString().Replace("'", @"''")}{((oPair.Key == "hash") ? "%" : "")}'";
                        }
                        break;
                }
            }

            foreach (KeyValuePair<string, string> oSubWhere in cSubWheres)
            {
                if (oSubWhere.Value != "")
                {
                    sWhere += " AND (" + oSubWhere.Value + ")";
                }
            }

            if (sLimit != "")
            {
                LoadFromDB(sWhere, sLimit);
            }
            else
            {
                LoadFromDB(sWhere);
            }

            //We do a follow up search, if the search term could be non-key-worded commit hash.
            if(cCommits.Count == 0 && 
               !bFolowUpSearch && 
               !sOriginalText.Trim().Contains(" ") && //Only 1 word
               sOriginalText.Trim().Length <= 40 && //Maximum length of 40
               !sOriginalText.Contains(":") && //No ":" in text, as it would be with key-worded searches
               !sOriginalText.Contains("commit:")) //Not already as a commit hash key-worded before
            {
                Search("commit:" + sText, true);
            }
            else
            {
                //Create & Show UI Commit list
                this.uiCommits.ItemsSource = cCommits;
                this.uiCommits.SelectedIndex = 0;
            }            
        }

        private void Commits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(this.uiCommits.SelectedItem == null)
            {
                this.uiCommitHASH.Text = "";
                this.uiCommitAuthor.Text = "";
                this.uiCommitDate.Text = "";
                this.uiCommitDescription.Text = "";

                this.uiCommitRevisions.Items.Clear();
                this.uiDiffView.Children.Clear();

                return;
            }            

            Commit oCommit = (Commit)this.uiCommits.SelectedItem;

            this.uiCommitHASH.Text = oCommit.sHASH + $" [{oCommit.sShortHASH}]";
            this.uiCommitAuthor.Text = oCommit.sAuthor;
            this.uiCommitDate.Text = oCommit.dLocalDate.ToLongDateString() + " " + oCommit.dLocalDate.ToLongTimeString();
            this.uiCommitDescription.Text = oCommit.sDescription;

            this.uiCommitRevisions.Items.Clear();

            //Revisions
            foreach(Revision oRevision in oCommit.cRevisions)
            {
                ListBoxItem oItem = new ListBoxItem();

                if(oRevision.sState == "dead")
                {
                    oItem.Height = 20;
                }else if(oRevision.sRevision == "1.1" || oRevision.bReAdded)
                {
                    oItem.Height = 21;
                }
                else
                {
                    oItem.Height = 22;
                }

                oItem.Content = $"  {oRevision.oFile.sName } {oRevision.sRevision}";
                oItem.Tag = oRevision;
                oItem.ToolTip = oRevision.oFile.sPath + "\\" + oRevision.oFile.sName;
                oItem.ContextMenu = (ContextMenu)FindResource("RevisionContextMenu");
                oItem.ContextMenu.Tag = oRevision;

                this.uiCommitRevisions.Items.Add(oItem);
            }

            if (this.PreviousRevisionSelection.ContainsKey(oCommit.sHASH))
            {
                this.uiCommitRevisions.SelectedIndex = this.PreviousRevisionSelection[oCommit.sHASH];
            }else
            {
                this.uiCommitRevisions.SelectedIndex = 0;
            }            
        }

        private void SearchText_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                Search_Click(null, null);
            }
        }

        private void CommitRevisions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.uiCommitRevisions.SelectedItem != null && e.OriginalSource.GetType() == typeof(TextBlock))
            {
                CVSFile oFile = ((Revision)((ListBoxItem)this.uiCommitRevisions.SelectedItem).Tag).oFile;
                if(System.IO.File.Exists(oFile.sPath + "\\" + oFile.sName))
                {
                    System.Diagnostics.Process.Start(oFile.sPath + "\\" + oFile.sName);
                }                
            }
        }

        private void ShowWelcome()
        {
            this.uiOverlay.Visibility = Visibility.Visible;
            this.uiWelcome.Visibility = Visibility.Visible;
        }

        private void ShowCVSMissing()
        {
            this.uiOverlay.Visibility = Visibility.Visible;
            this.uiCVSMissing.Visibility = Visibility.Visible;
        }

        private void ShowLoadingData()
        {
            this.uiOverlay.Visibility = Visibility.Visible;
            this.uiLoadingData.Visibility = Visibility.Visible;
        }

        private void HideLoadingData()
        {
            this.uiOverlay.Visibility = Visibility.Collapsed;
            this.uiLoadingData.Visibility = Visibility.Collapsed;
        }

        private void WelcomeButton_Click(object sender, RoutedEventArgs e)
        {
            ChooseDirectory_Click(null, null);
            this.uiWelcome.Visibility = Visibility.Collapsed;
        }

        private void CloseAbout_Click(object sender, RoutedEventArgs e)
        {
            this.uiOverlay.Visibility = Visibility.Collapsed;
            this.uiAbout.Visibility = Visibility.Collapsed;    
        }

        private void ShowAbout_Click(object sender, RoutedEventArgs e)
        {
            this.uiAboutVersion.Content = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;

            this.uiOverlay.Visibility = Visibility.Visible;
            this.uiAbout.Visibility = Visibility.Visible;
        }

        private void License_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(System.AppDomain.CurrentDomain.BaseDirectory + "LICENSE.txt");
        }

        private void ThirdPartyLicenses_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(System.AppDomain.CurrentDomain.BaseDirectory + "Third Party Licenses.txt");
        }

        private void CancelUpdate_Click(object sender, RoutedEventArgs e)
        {
            this.uiCancelUpdate.IsEnabled = false;
            this.uiUpdateState.Content = "Canceling";
            oUpdateCommitsWorker.CancelAsync();
        }

        private void CommitRevisions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.uiCommitRevisions.SelectedItem == null)
            {
                return;
            }

            Commit oCommit = (Commit)this.uiCommits.SelectedItem;

            if (this.PreviousRevisionSelection.ContainsKey(oCommit.sHASH))
            {
                this.PreviousRevisionSelection[oCommit.sHASH] = this.uiCommitRevisions.SelectedIndex;
            }
            else
            {
                this.PreviousRevisionSelection.Add(oCommit.sHASH, this.uiCommitRevisions.SelectedIndex);
            }

            this.uiDiffLoading.Visibility = Visibility.Visible;
            oDiffFetchDelay.Stop();
            oDiffFetchDelay.Start();
        }

        private void oDiffFetchDelay_Tick(object sender, EventArgs e)
        {
            oDiffFetchDelay.Stop();
            if (this.uiCommitRevisions.SelectedItem == null)
            {
                return;
            }

            Revision oRevision = (Revision)((ListBoxItem)this.uiCommitRevisions.SelectedItem).Tag;           
            
            if(oRevision.iWhitespace != oSettings.iWhitespace)
            {
                oDatabase.DeleteDiffLines(oRevision);
                oRevision.cDiffBlocks.Clear();
                oRevision.iWhitespace = oSettings.iWhitespace;
            }

            if (oRevision.cDiffBlocks.Count > 0)
            {
                LoadDiffView(oRevision);
            }
            else
            {
                oBackgroundStuff.FetchDiff(oRevision);
            }
        }

        private void BackgroundStuff_DiffCompleted(object sender, BackgroundStuff.DiffCompletedEventArgs e)
        {
            LoadDiffView(e.oRevision);
        }

        private void LoadDiffView(Revision oRevision)
        {
            //Load Diff Block(s) into view.
            this.uiDiffView.Children.Clear();

            string sFileExtension = Path.GetExtension(oRevision.oFile.sName).Replace(".", "");

            int iBlock = 1;
            foreach (DiffBlock oDiffBlock in oRevision.cDiffBlocks)
            {
                int iPadLength = oRevision.cDiffBlocks[oRevision.cDiffBlocks.Count - 1].iEndLine.ToString().Length;

                if(oDiffBlock.cDiffLines.Count == 0)
                {
                    if(this.uiDiffView.Children.Count == 0)
                    {
                        oDiffBlock.cDiffLines.Add(new TextBlock()
                        {
                            Background = (Brush)this.TryFindResource("Red"),
                            FontSize = 14,
                            Foreground = Brushes.White,
                            Text = $"File: {oRevision.oFile.sName} {oRevision.sRevision} {((oRevision.sLinesChanged != "") ? "(" + oRevision.sLinesChanged + ")" : "")}",
                            Margin = new Thickness(0, 0, 0, 0)
                        });

                        this.uiDiffView.Children.Add(oDiffBlock.cDiffLines[oDiffBlock.cDiffLines.Count - 1]);
                    }

                    oDiffBlock.cDiffLines.Add(new TextBlock()
                    {
                        Background = (Brush)this.TryFindResource("Red"),
                        FontSize = 14,
                        Foreground = Brushes.White,
                        Text = $"Block {iBlock++}: {oDiffBlock.iStartLine} - {oDiffBlock.iEndLine}",
                        Margin = new Thickness(0, ((this.uiDiffView.Children.Count > 1) ? 10 : 0), 0, 0)
                    });

                    this.uiDiffView.Children.Add(oDiffBlock.cDiffLines[oDiffBlock.cDiffLines.Count - 1]);

                    int iLine = oDiffBlock.iStartLine;
                    foreach (DiffBlock.LineChange oChange in oDiffBlock.cLines)
                    {
                        TextBlock oTextBlock = new TextBlock();

                        switch (oChange.sAction)
                        {
                            case "+":
                                oTextBlock.Background = (Brush)new BrushConverter().ConvertFromString("#FFDDFFDD");
                                oTextBlock.Text = $"{iLine++.ToString().PadLeft(iPadLength, ' ')} {oChange.sAction} ";
                                break;
                            case "-":
                                oTextBlock.Background = (Brush)new BrushConverter().ConvertFromString("#FFFEE8E9");
                                oTextBlock.Text = $"{"#".PadLeft(iPadLength, ' ')} {oChange.sAction} ";
                                break;
                            default:
                                oTextBlock.Background = Brushes.White;
                                oTextBlock.Text = $"{iLine++.ToString().PadLeft(iPadLength, ' ')}   ";
                                break;
                        }

                        oTextBlock.FontFamily = new FontFamily("Consolas");
                        oTextBlock.Foreground = Brushes.Black;
                        
                        oTextBlock.Inlines.AddRange(oSyntaxHighlighting.ParseSyntax(oChange.sLine.Replace("\t", oSettings.sTab), sFileExtension));

                        oDiffBlock.cDiffLines.Add(oTextBlock);

                        this.uiDiffView.Children.Add(oDiffBlock.cDiffLines[oDiffBlock.cDiffLines.Count - 1]);
                    }
                }
                else
                {
                    foreach(TextBlock oDiffLine in oDiffBlock.cDiffLines)
                    {
                        this.uiDiffView.Children.Add(oDiffLine);
                    }
                }                
            }

            this.uiDiffLoading.Visibility = Visibility.Collapsed;
        }

        private void MenuItem_ShowInExplorerClick(object sender, RoutedEventArgs e)
        {
            CVSFile oFile = ((Revision)((ContextMenu)((MenuItem)sender).Parent).Tag).oFile;
            if (System.IO.File.Exists(oFile.sPath + "\\" + oFile.sName))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select, \"{oFile.sPath}\\{oFile.sName}\"");
            }            
        }

        private void MenuItem_OpenCurrentRevisionClick(object sender, RoutedEventArgs e)
        {
            CVSFile oFile = ((Revision)((ContextMenu)((MenuItem)sender).Parent).Tag).oFile;
            if (System.IO.File.Exists(oFile.sPath + "\\" + oFile.sName))
            {
                System.Diagnostics.Process.Start(oFile.sPath + "\\" + oFile.sName);
            }
        }

        private void MenuItem_OpenSelectedRevisionClick(object sender, RoutedEventArgs e)
        {
            string newFile = CVSCalls.OutputRevisionToFile((Revision)((ContextMenu)((MenuItem)sender).Parent).Tag);
            System.Diagnostics.Process.Start(newFile);
        }
    }

    class UpdateProgress
    {
        public int iDone = 0;
        public int iTotal = 0;
    }
}
