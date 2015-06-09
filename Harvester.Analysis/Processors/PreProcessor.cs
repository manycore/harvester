using Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester.Analysis
{
    /// <summary>
    /// Does initial preprocessing.
    /// </summary>
    public class PreProcessor : EventProcessor
    {
        #region Constructor
        /// <summary>
        /// Constructs a new processor for the provided data files.
        /// </summary>
        /// <param name="events">The data file containing events.</param>
        /// <param name="counters">The data file containing harware coutnters.</param>
        public PreProcessor(TraceLog events, TraceCounter[] counters)
            : base(events, counters)
        {

        }
        #endregion

        /// <summary>
        /// Invoked when an analysis needs to be performed.
        /// </summary>
        protected override EventOutput OnAnalyze()
        {
            return null;
        }

    }
}
