using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
    [Complete]
    public class NullProgressMonitor : ProgressMonitor 
    {
        #region ProgressMonitor Members

        public void Start(int totalTasks)
        {
        }

        public void BeginTask(string title, int totalWork)
        {
        }

        public void Update(int completed)
        {
        }

        public void EndTask()
        {
        }

        public bool IsCancelled
        {
            get { return false; }
        }

        #endregion
    }
}
