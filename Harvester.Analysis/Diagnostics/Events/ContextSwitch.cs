using Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Harvester.Analysis
{

    public sealed class ContextSwitch 
    {
        public ContextSwitch(TraceEvent sw)
        {
            // Old thread id & process id
            this.OldThreadId = (int)sw.PayloadValue(0);
            this.OldProcessId = (int)sw.PayloadValue(1);
            this.NewThreadId = (int)sw.PayloadValue(3);
            this.NewProcessId = (int)sw.PayloadValue(4);
            this.State = (ThreadState)sw.PayloadValue(13);
            this.ProcessorNumber = sw.ProcessorNumber;
            this.TimeStamp = sw.TimeStamp;
            this.TimeStamp100ns = sw.TimeStamp100ns;

            // Multiply by 10 to adjust the time as 100ns
            this.NewThreadWaitTime = (int)sw.PayloadValue(15) * 10;
        }

        public readonly int OldThreadId;
        public readonly int OldProcessId;
        public readonly int NewThreadId;
        public readonly int NewProcessId;
        public readonly int ProcessorNumber;
        public readonly ThreadState State;
        public readonly DateTime TimeStamp;
        public readonly long TimeStamp100ns;
        public readonly int NewThreadWaitTime;

        public override string ToString()
        {
            return String.Format("{0}\t{5}ns:\t({1}, {2}) -> ({3}, {4}) wait: {6}", State, OldThreadId, OldProcessId, NewThreadId, NewProcessId, TimeStamp100ns, NewThreadWaitTime);
        }
    }


}
