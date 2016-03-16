using Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester.Analysis
{
    public class CoherencyProcessor : EventProcessor
    {
        /// <summary>
        /// Constructs a new processor for the provided data files.
        /// </summary>
        /// <param name="preprocessor">Preprocessor to use</param>
        public CoherencyProcessor(EventProcessor preprocessor) : base(preprocessor) { }

        /// <summary>
        /// Invoked when an analysis needs to be performed.
        /// </summary>
        protected override EventOutput OnAnalyze()
        {
            // Here we will store our results
            var output = new EventOutput(this.Process.Name, this.Start);

            // Process every frame
            foreach (var frame in this.Frames)
            {
                // Build some shortcuts
                var core = frame.Core;

                // Get corresponding hardware counters 
                var cn = frame.HwCounters;

                // Process every thread within this frame
                foreach (var thread in frame.Threads)
                {
                    // Get the multiplier for that thread
                    var multiplier = frame.GetOnCoreRatio(thread);

                    output.Add("l1Invalidations", frame, thread, Math.Round(multiplier * cn.L1Invalidations));
                    output.Add("l2Invalidations", frame, thread, Math.Round(multiplier * cn.L2Invalidations));

                    output.Add("l1miss", frame, thread, Math.Round(multiplier * cn.L1Misses));
                    output.Add("l2miss", frame, thread, Math.Round(multiplier * cn.L2Misses));
                }
            }


            // Return the results
            return output;
        }

    }
}
