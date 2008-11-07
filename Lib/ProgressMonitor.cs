using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
    [Complete]
    public interface ProgressMonitor
    {
        void Start(int totalTasks);
        void BeginTask(String title, int totalWork);
        void Update(int completed);
        void EndTask();

        bool IsCancelled{ get; } 
    }
}
