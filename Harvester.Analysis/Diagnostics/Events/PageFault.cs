using Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Harvester.Analysis
{

    public sealed class PageFault 
    {
        public PageFault(TraceEvent ev)
        {
            switch (ev.EventName)
            {
                case "PageFault/DemandZeroFault": this.Type = PageFaultType.Minor; break;
                case "PageFault/HardPageFault": this.Type = PageFaultType.Major; break;
                case "PageFault/HardFault": this.Type = PageFaultType.Major; break;

                default: this.Type = PageFaultType.Unknown; break;
            }

            this.TimeStamp = ev.TimeStamp;
            this.ProcessorNumber = ev.ProcessorNumber;
            this.Process = ev.ProcessID;
            this.Thread = ev.ThreadID;
        }

        /// <summary>
        /// Gets or sets the type of the page fault.
        /// </summary>
        public readonly PageFaultType Type;

        /// <summary>
        /// Gets the timestamp of the event.
        /// </summary>
        public readonly DateTime TimeStamp;

        /// <summary>
        /// Gets the processor number of the event.
        /// </summary>
        public readonly int ProcessorNumber;

        /// <summary>
        /// Gets the process of the event.
        /// </summary>
        public readonly int Process;

        /// <summary>
        /// Gets the thread of the event.
        /// </summary>
        public readonly int Thread;
    }

    /// <summary>
    /// The type of the page fault.
    /// </summary>
    public enum PageFaultType
    {
        /// <summary>
        /// Unknown type of page fault.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Minor (demand zero) page fault.
        /// </summary>
        Minor = 1,

        /// <summary>
        /// Major (hard) page fault.
        /// </summary>
        Major = 2
    }
}
