using Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester.Analysis
{
    public class LoadBalanceProcessor : EventProcessor
    {
        /// <summary>
        /// Constructs a new processor for the provided data files.
        /// </summary>
        /// <param name="events">The data file containing events.</param>
        /// <param name="counters">The data file containing harware coutnters.</param>
        public LoadBalanceProcessor(TraceLog events, TraceCounter[] counters): base(events, counters)
        {

        }

        /// <summary>
        /// Invoked when an analysis needs to be performed.
        /// </summary>
        protected override EventOutput OnAnalyze()
        {
            // Here we will store our results
            var output = new EventOutput(this.Process.Name);

            // Process every frame
            foreach (var frame in this.Frames)
            {
                // Build some shortcuts
                var core = frame.Core;

                // Get corresponding hardware counters 
                var cn = frame.HwCounters;

                var contextSwitches = this.TraceLog.Events
                    .Where(e => e.EventName == "Thread/CSwitch")
                    .Where(e => e.ProcessorNumber == core)
                    .Where(e => e.TimeStamp >= frame.Time)
                    .Where(e => e.TimeStamp <= frame.Time + frame.Duration)
                    .Count();

                output.Add("switch", frame, EventThread.Custom, contextSwitches);

                // Process every thread within this frame
                foreach (var thread in frame.Threads)
                {
                    // Get the number of cycles elapsed
                    var multiplier = frame.GetShare(thread);
                    var cycles = Math.Round(multiplier * cn.Cycles);
                    output.Add("cycles", frame, thread, cycles);

                    // Time in nanoseconds
                    output.Add("ready", frame, thread, frame.GetTime(thread, ThreadState.Ready) * 100);
                    output.Add("running", frame, thread, frame.GetTime(thread, ThreadState.Running) * 100);
                    output.Add("init", frame, thread, frame.GetTime(thread, ThreadState.Initialized) * 100);
                    output.Add("standby", frame, thread, frame.GetTime(thread, ThreadState.Standby) * 100);
                    output.Add("terminated", frame, thread, frame.GetTime(thread, ThreadState.Terminated) * 100);
                    output.Add("transition", frame, thread, frame.GetTime(thread, ThreadState.Transition) * 100);
                    output.Add("unknown", frame, thread, frame.GetTime(thread, ThreadState.Unknown) * 100);
                    output.Add("wait", frame, thread, frame.GetTime(thread, ThreadState.Wait) * 100);
                }
            }


            // Return the results
            return output;
        }

    }
}
