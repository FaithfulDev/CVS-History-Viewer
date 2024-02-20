using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CVS_History_Viewer.Resources.Classes
{
    class BackgroundStuff
    {
        private readonly Database oDatabase;
        private readonly List<BackgroundWorker> cDiffWorkers = new List<BackgroundWorker>();

        public delegate void DiffCompleted(object sender, DiffCompletedEventArgs e);
        public event DiffCompleted OnDiffCompleted;

        public BackgroundStuff(Database oDatabase)
        {
            this.oDatabase = oDatabase;
        }

        public class DiffCompletedEventArgs : EventArgs
        {
            public Revision oRevision;
        }

        public void FetchDiff(Revision oRevision)
        {
            if(cDiffWorkers.Count > 0)
            {
                cDiffWorkers[cDiffWorkers.Count - 1].CancelAsync();
            }

            BackgroundWorker oBackgroundWorker = new BackgroundWorker();
            oBackgroundWorker.DoWork += DiffWorkers_DoWork;
            oBackgroundWorker.RunWorkerCompleted += DiffWorkers_Completed;
            oBackgroundWorker.WorkerSupportsCancellation = true;            

            cDiffWorkers.Add(oBackgroundWorker);
            oBackgroundWorker.RunWorkerAsync(oRevision);
        }
        
        public void DiffWorkers_DoWork(object sender, DoWorkEventArgs e)
        {
            Revision oRevision = (Revision)e.Argument;

            //Check if it was canceled a few moments after being started.
            if (((BackgroundWorker)sender).CancellationPending) { return; }

            while (cDiffWorkers.Count > 3)
            {
                if(sender == cDiffWorkers[0])
                {
                    break;
                }
                //There are already 3 works busy, so wait a moment to give them time to finish.
                System.Threading.Thread.Sleep(400);
            }

            oRevision = CVSCalls.GetDiff(oRevision);
            e.Result = oRevision;

            oDatabase.SaveDiff(oRevision);
        }

        public void DiffWorkers_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            cDiffWorkers.Remove((BackgroundWorker)sender);

            if (e.Result != null)
            {                
                OnDiffCompleted(this, new DiffCompletedEventArgs { oRevision = (Revision)e.Result });
            }           
        }

    }
}
