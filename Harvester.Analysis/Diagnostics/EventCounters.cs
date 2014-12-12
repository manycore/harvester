using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester.Analysis
{
    /// <summary>
    /// Represents the combined event counters.
    /// </summary>
    public class EventCounters
    {
        /// <summary>
        /// Gets or sets the instructions per cycle ratio.
        /// </summary>
        public double IPC;

        /// <summary>
        /// Gets or sets the number of L1 cache misses.
        /// </summary>
        public long L1Misses;

        /// <summary>
        /// Gets or sets the number of L2 cache misses.
        /// </summary>
        public long L2Misses;

        /// <summary>
        /// Gets or sets the number of L3 cache misses.
        /// </summary>
        public long L3Misses;

        /// <summary>
        /// Gets or sets the number of L3 cache hits.
        /// </summary>
        public long L3Hits;

        /// <summary>
        /// Gets or sets the number of L2 cache hits.
        /// </summary>
        public long L2Hits;

        /// <summary>
        /// Gets or sets the L3 clock impact.
        /// </summary>
        public double L3Clock;

        /// <summary>
        /// Gets or sets the L2 clock impact.
        /// </summary>
        public double L2Clock;

        /// <summary>
        /// Gets or sets the number of minor page faults.
        /// </summary>
        public long MinorPageFaults;

        /// <summary>
        /// Gets or sets the number of major page faults.
        /// </summary>
        public long MajorPageFaults;
    }


}
