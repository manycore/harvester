using Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester.Analysis
{
    public class MemoryProcessor : EventProcessor
    {
        /// <summary>
        /// Constructs a new processor for the provided data files.
        /// </summary>
        /// <param name="preprocessor">Preprocessor to use</param>
        public MemoryProcessor(EventProcessor preprocessor) : base(preprocessor) { }

        /// <summary>
        /// Invoked when an analysis needs to be performed.
        /// </summary>
        protected override EventOutput OnAnalyze()
        {
            // Here we will store our results
            var output = new EventOutput(this.Process.Name, this.Start);

            // Get the largest core by iterating through frames and getting the largest frame.core. Use the .Where(e==>e.core) stuff!
            //var maxCoreCount = this.Counters.Select(c => c.Core).Max();
            var maxCoreCount = 7;

            // Process every frame
            foreach (var frame in this.Frames)
            {
                // Build some shortcuts
                var core = frame.Core;
                            
                // Get corresponding hardware counters 
                var cn = frame.HwCounters;

                output.Add("drambw", frame, EventThread.Custom, cn.DRAMBandwidth);

                if( core == maxCoreCount )
                {
                    output.Add("MC_read", frame, EventThread.Custom, cn.BytesReadFromMC);
                    output.Add("MC_write", frame, EventThread.Custom, cn.BytesWrittenToMC);
                }

                /*var contextSwitches = this.TraceLog.Events
                    .Where(e => e.EventName == "Thread/CSwitch")
                    .Where(e => e.ProcessorNumber == core)
                    .Where(e => e.ProcessID == this.Process.ProcessID)
                    .Where(e => e.TimeStamp >= frame.Time)
                    .Where(e => e.TimeStamp <= frame.Time + frame.Duration)
                    .Count();

                output.Add("switch", frame, EventThread.Custom, contextSwitches);*/

                // Process every thread within this frame
                /*
                foreach (var thread in frame.Threads)
                {
                    // Get the number of cycles elapsed
                    var multiplier = frame.GetOnCoreRatio(thread);
                    var cycles = Math.Round(multiplier * cn.Cycles);
                    output.Add("cycles", frame, thread, cycles);

                    // Time in milliseconds
                    output.Add("ready", frame, thread, frame.GetTime(thread, ThreadState.Ready) / 10000);
                    output.Add("running", frame, thread, frame.GetTime(thread, ThreadState.Running) / 10000);
                    output.Add("standby", frame, thread, frame.GetTime(thread, ThreadState.Standby) / 10000);
                    output.Add("wait", frame, thread, frame.GetTime(thread, ThreadState.Wait) / 10000);
                }
                */
            }


            // Return the results
            return output;
        }

    }
}
