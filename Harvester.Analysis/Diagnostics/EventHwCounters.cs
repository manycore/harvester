﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester.Analysis
{
    /// <summary>
    /// Represents the combined event counters.
    /// </summary>
    public class EventHwCounters
    {
        /// <summary>
        /// Gets or sets the instructions per cycle ratio.
        /// </summary>
        public double IPC;

        /// <summary>
        /// Gets or sets the cycles.
        /// </summary>
        public long Cycles;

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
        /// Gets or sets the number of TLB misses.
        /// </summary>
        public double TLBMisses;

        /// <summary>
        /// Gets or sets the TLB clock impact.
        /// </summary>
        public double TLBClock;

        /// <summary>
        /// Gets or sets the number of L1 cache invalidations
        /// </summary>
        public double L1Invalidations;

        /// <summary>
        /// Gets or sets the number of L2 cache invalidations
        /// </summary>
        public double L2Invalidations;

        /// <summary>
        /// Gets or sets the DRAM Bandwidth in bytes
        /// </summary>
        public double DRAMBandwidth;

        /// <summary>
        /// Gets or sets the number of bytes read from the memory controller by the system
        /// </summary>
        public double BytesReadFromMC;

        /// <summary>
        /// Gets or sets the number of bytes written to the memory controller by the system
        /// </summary>
        public double BytesWrittenToMC;

        /// <summary>
        /// Gets or sets the Incoming QPI
        /// </summary>
        public double IncomingQPI;

        /// <summary>
        /// Gets or sets the Outgoing QPI
        /// </summary>
        public double OutgoingQPI;
    }


}
