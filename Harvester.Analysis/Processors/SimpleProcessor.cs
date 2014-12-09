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
            foreach(var frame in this.Frames)
            {
                // Build some shortcuts
                var core = frame.Core;
                var timeFrom = frame.Time;
                var timeTo   = frame.Time + frame.Duration;

                // Get corresponding hardware counters (for all the cores)
                var hw = this.Counters
                    .Where(c => c.TIME >= timeFrom && c.TIME <= timeTo);

                
            }
        }
    }
}
