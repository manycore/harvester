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
            this.Lock = (long)ev.PayloadValue(0);
            this.Flag = (long)ev.PayloadValue(1);
            this.Type = this.Flag == -1 ? LockAcquisitionType.Release : type;
            this.ThreadId = ev.ThreadID;
            this.ProcessId = ev.ProcessID;
            this.ProcessorNumber = ev.ProcessorNumber;
            this.TimeStamp = ev.TimeStamp;
            this.TimeStamp100ns = ev.TimeStamp100ns;
        }

        public readonly long Lock;
        public readonly long Flag;
        public readonly LockAcquisitionType Type;

        public readonly int ThreadId;
        public readonly int ProcessId;
        public readonly int ProcessorNumber;
        public readonly DateTime TimeStamp;
        public readonly long TimeStamp100ns;

        public override string ToString()
        {
            return String.Format("{0} ns; thread {1}; locks {2}, {3}; type {4}",  TimeStamp100ns, ThreadId, Lock, Flag, Type);
        }
    }

    public enum LockAcquisitionType
    {
        Success,
        Failure,
        Release
    }

}
