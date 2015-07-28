using Diagnostics.Tracing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester.Analysis
{
   

    /// <summary>
    /// Represents some information about the thread.
    /// </summary>
    public struct EventThread : IEquatable<EventThread>
    {
        /// <summary>
        /// Gets or sets the thread number.
        /// </summary>
        public int Tid;

        /// <summary>
        /// Gets or sets the proccess name associated with the thread.
        /// </summary>
        public string Process;

        /// <summary>
        /// Gets or sets the user name associated with the thread.
        /// </summary>
        public string User;

        /// <summary>
        /// Gets or sets the process number associated with the thread.
        /// </summary>
        public int Pid;

        /// <summary>
        /// Gets or sets the user id associated with the thread.
        /// </summary>
        public int Uid;

        /// <summary>
        /// Gets the hash code for the thread.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Tid.GetHashCode() ^ Pid.GetHashCode() ^ Uid.GetHashCode();
        }

        /// <summary>
        /// Compares the thread to another thread.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(EventThread other)
        {
            return other.Tid == this.Tid && other.Pid == this.Pid && other.Uid == this.Uid;
        }

        #region Static Members
        /// <summary>
        /// Gets the default system thread.
        /// </summary>
        public static readonly EventThread System = new EventThread()
        {
            Tid = 0,
            Pid = 0,
            Uid = 0,
            User = "root",
            Process = "system"
        };

        /// <summary>
        /// Gets the 'Idle' thread.
        /// </summary>
        public static readonly EventThread Idle = new EventThread()
        {
            Tid = 1,
            Pid = 1,
            Uid = 0,
            User = "root",
            Process = "Idle"
        };


        /// <summary>
        /// Gets the custom thread.
        /// </summary>
        public static readonly EventThread Custom = new EventThread()
        {
            Tid = -1,
            Pid = 0,
            Uid = 0,
            User = "N/A",
            Process = "N/A"
        };


        /// <summary>
        /// Constructs a new thread info from trace thread & monitored process.
        /// </summary>
        /// <param name="thread"></param>
        /// <param name="monitoredProcess"></param>
        /// <returns></returns>
        public static EventThread FromTrace(TraceThread thread, TraceProcess monitoredProcess)
        {
            // PID 0 is the Idle thread
            if (thread.Process.ProcessID == 0)
                return EventThread.Idle;

            // Everything non-related we put to default
            if(thread.Process.ProcessID != monitoredProcess.ProcessID)
                return EventThread.System;

            return new EventThread()
            {
                Tid = thread.ThreadID,
                Pid = monitoredProcess.ProcessID,
                Uid = 1,
                Process = monitoredProcess.Name,
                User = "user"
            };
        }

        /// <summary>
        /// Constructs a new thread info from trace thread & monitored process.
        /// </summary>
        /// <param name="thread"></param>
        /// <param name="monitoredProcess"></param>
        /// <returns></returns>
        public static EventThread FromTrace(int tid, int pid, TraceProcess monitoredProcess)
        {
            // PID 0 is the Idle thread
            if (pid == 0)
                return EventThread.Idle;

            // Everything non-related we put to default
            if (pid != monitoredProcess.ProcessID)
                return EventThread.System;

            return new EventThread()
            {
                Tid = tid,
                Pid = pid,
                Uid = 1,
                Process = monitoredProcess.Name,
                User = "user"
            };
        }

        public override string ToString()
        {
            return this.Tid.ToString();
        }
        #endregion
    }
}
