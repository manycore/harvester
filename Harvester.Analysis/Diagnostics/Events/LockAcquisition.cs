using Diagnostics.Tracing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Harvester.Analysis
{

    public sealed class LockAcquisition 
    {
        public LockAcquisition(TraceEvent ev, LockAcquisitionType type)
        {
            // Old thread id & process id
            this.Lock0 = (long)ev.PayloadValue(0);
            this.Lock1 = (long)ev.PayloadValue(1);
            this.Type = type;
            this.ThreadId = ev.ThreadID;
            this.ProcessId = ev.ProcessID;
            this.ProcessorNumber = ev.ProcessorNumber;
            this.TimeStamp = ev.TimeStamp;
            this.TimeStamp100ns = ev.TimeStamp100ns;
        }

        public readonly long Lock0;
        public readonly long Lock1;
        public readonly LockAcquisitionType Type;

        public readonly int ThreadId;
        public readonly int ProcessId;
        public readonly int ProcessorNumber;
        public readonly DateTime TimeStamp;
        public readonly long TimeStamp100ns;

        public override string ToString()
        {
            return String.Format("{0} ns; thread {1}; locks {2}, {3}; type {4}",  TimeStamp100ns, ThreadId, Lock0, Lock1, Type);
        }
    }

    public enum LockAcquisitionType
    {
        Success,
        Failure
    }

}
