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
    public class SimpleProcessor : EventProcessor
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
        protected override EventOutput OnAnalyze()
        {
            // Here we will store our results
            var output  = new EventOutput();
            
            // Process every frame
            foreach(var frame in this.Frames)
            {
                // Build some shortcuts
                var core = frame.Core;
                
                // Get corresponding hardware counters 
                var hw = frame.Counters;

                // Process every thread within this frame
                foreach (var thread in frame.Threads)
                {
                    // Get the multiplier for that thread
                    var multiplier = frame.GetShare(thread);


                    output.Add("l1miss", frame, thread, Math.Round(multiplier * (hw.L2HIT + hw.L2MISS)));
                    //output.Add("l2miss", frame, thread, Math.Round(multiplier * hw.L2MISS));
                    //output.Add("l3miss", frame, thread, Math.Round(multiplier * hw.L3MISS));

                    //output.Add("l2perf", frame, thread, hw.L2CLK);
                    //output.Add("l3perf", frame, thread, hw.L3CLK);

                    //output.Add("ipc", frame, thread, hw.IPC);

                }
     
            }


            // Return the results
            return output;
        }


    }
}
