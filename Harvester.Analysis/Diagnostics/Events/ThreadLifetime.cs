using Diagnostics.Tracing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Harvester.Analysis
{

    public sealed class ThreadLifetime 
    {
        public ThreadLifetime(TraceEvent sw, ThreadLifetimeType state)
        {
            // Old thread id & process id
            this.ProcessorNumber = sw.ProcessorNumber;
            this.TimeStamp = sw.TimeStamp;
            this.TimeStamp100ns = sw.TimeStamp100ns;
            this.ThreadId = sw.ThreadID;
            this.ProcessId = sw.ProcessID;
            this.State = state;
        }

        public readonly int ThreadId;
        public readonly int ProcessId;
        public readonly int ProcessorNumber;
        public readonly ThreadLifetimeType State;
        public readonly DateTime TimeStamp;
        public readonly long TimeStamp100ns;

        public override string ToString()
        {
            return String.Format("[{0}] \t tid: {1} pid: {2}", State, this.ThreadId, this.ProcessId);
        }
    }

    public enum ThreadLifetimeType
    {
        Start,
        End
    }
}
