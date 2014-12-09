using Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester.Analysis
{
    /// <summary>
    /// Does a simple cache miss processing.
    /// </summary>
    public class SimpleProcessor : ThreadProcessor
    {
        #region Constructor
                /// <summary>
        /// Constructs a new processor for the provided data files.
        /// </summary>
        /// <param name="events">The data file containing events.</param>
        /// <param name="counters">The data file containing harware coutnters.</param>
        public SimpleProcessor(TraceLog events, TraceCounter[] counters): base(events, counters)
        {

        }
        #endregion

        /// <summary>
        /// Invoked when an analysis needs to be performed.
        /// </summary>
        protected override void OnAnalyze()
        {
            // Compute the average from the run before the process
            var l1noise = 0.0;
            var l2noise = 0.0;
            var l3noise = 0.0;
            try
            {
                l1noise = this.Counters.Where(t => t.TIME < this.Process.StartTime).Select(c => c.L2HIT + c.L2MISS).Average();
                l2noise = this.Counters.Where(t => t.TIME < this.Process.StartTime).Select(c => c.L2MISS).Average();
                l3noise = this.Counters.Where(t => t.TIME < this.Process.StartTime).Select(c => c.L3MISS).Average();
            }
            catch { } // Ignore, the noise will be just zero


            // Process every frame
            foreach(var frame in this.Frames)
            {
                // Build some shortcuts
                var core = frame.Core;
                var timeFrom = frame.Time;
                var timeTo   = frame.Time + frame.Duration;

                // Get corresponding hardware counters 
                var hw = this.GetCounters(core, timeFrom, timeTo);
                
            }
        }


        private TraceCounterCore GetCounters(int core, DateTime from, DateTime to)
        {
            // Get corresponding hardware counters
            var counters = this.Counters
                .Where(c => c.TIME >= from && c.TIME <= to)
                .Select(c => c.Core[core]);
            var count = counters.Count();

            // If we don't have anything, return zeroes
            var hw = new TraceCounterCore();
            if (count == 0)
                return hw;

            // Average or sum depending on the counter
            hw.IPC = counters.Select(c => c.IPC).Average();
            hw.FREQ = counters.Select(c => c.FREQ).Average();
            hw.AFREQ = counters.Select(c => c.AFREQ).Average();
            hw.L2MISS = counters.Select(c => c.L2MISS).Sum();
            hw.L3MISS = counters.Select(c => c.L2MISS).Sum();
            hw.L2HIT = counters.Select(c => c.L2HIT).Sum();
            hw.L3HIT = counters.Select(c => c.L2HIT).Sum();
            hw.L2CLK = counters.Select(c => c.L2CLK).Average();
            hw.L3CLK = counters.Select(c => c.L3CLK).Average();

            return hw;
        }
    }
}
