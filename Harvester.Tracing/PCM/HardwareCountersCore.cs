using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace Lab.Tracing
{
    /// <summary>
    /// Represents an entry for a particular core.
    /// </summary>
	public class HardwareCountersCore
	{
        //C0@S0	C0@S0	C0@S0	C0@S0	C0@S0	C0@S0	C0@S0	C0@S0	C0@S0	C0@S0
        //IPC	FREQ	AFREQ	L3MISS	L2MISS	L3HIT	L2HIT	L3CLK	L2CLK	EXEC

        /// <summary>
        /// Constructs a parsed entry for a particular core.
        /// </summary>
        public HardwareCountersCore()
        {

        }

        public double IPC;
        public double FREQ;
        public double AFREQ;
        public long L3MISS;
        public long L2MISS;
        public long L3HIT;
        public long L2HIT;
        public double L3CLK;
        public double L2CLK;
	}
}

