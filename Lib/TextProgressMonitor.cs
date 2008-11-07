using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Gitty.Lib
{
    [Complete]
    public class TextProgressMonitor : ProgressMonitor
    {
        private DateTime _taskBeganAt;
        private string _message;
        private int _lastWorked;
        private int _totalWork;
        private bool _output;
        private TextWriter _writer;

        public TextProgressMonitor()
            : this(Console.Error)
        {
        }

        public TextProgressMonitor(TextWriter writer)
        {
            _writer = writer;
            _taskBeganAt = DateTime.Now;
        }

        #region ProgressMonitor Members

        public void Start(int totalTasks)
        {
            _taskBeganAt = DateTime.Now;
        }

        public void BeginTask(string title, int totalWork)
        {
            EndTask();
            this._message = title;
            this._lastWorked = 0;
            this._totalWork = totalWork;
        }

        public void Update(int completed)
        {
            if (_message == null)
                return;
            int cmp = _lastWorked + completed;
            if (!_output && ((DateTime.Now - _taskBeganAt).TotalMilliseconds < 500))
                return;

            if (_totalWork == 0)
            {
                Display(cmp);
                _writer.Flush();
            }
            else if ((cmp * 100 / _totalWork) != (_lastWorked * 100) / _totalWork)
            {
                Display(cmp);
                _writer.Flush();
            }
            
            _lastWorked = cmp;
            _output = true;
        }

        private void Display(int cmp)
        {
            StringBuilder m = new StringBuilder();
            m.Append('\r');
            m.Append(_message);
            m.Append(": ");
            while (m.Length < 25)
                m.Append(' ');

            if (_totalWork == 0)
            {
                m.Append(cmp);
            }
            else
            {
                String twstr = _totalWork.ToString();
                String cmpstr = cmp.ToString();
                while (cmpstr.Length < twstr.Length)
                    cmpstr = " " + cmpstr;
                int pcnt = (cmp * 100 / _totalWork);
                if (pcnt < 100)
                    m.Append(' ');
                if (pcnt < 10)
                    m.Append(' ');
                m.Append(pcnt);
                m.Append("% (");
                m.Append(cmpstr);
                m.Append("/");
                m.Append(twstr);
                m.Append(")");
            }

            _writer.Write(m);
        }

        public void EndTask()
        {
            if (_output)
            {
                if (_totalWork != 0)
                    Display(_totalWork);
                _writer.WriteLine();
            }
            _output = false;
            _message = null;
        }

        public bool IsCancelled
        {
            get { return false ; }
        }

        #endregion
    }
}
