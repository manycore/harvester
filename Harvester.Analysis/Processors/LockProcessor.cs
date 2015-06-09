using Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester.Analysis
{
    public class LockProcessor : EventProcessor
    {
        /// <summary>
        /// Constructs a new processor for the provided data files.
        /// </summary>
        /// <param name="preprocessor">Preprocessor to use</param>
        public LockProcessor(EventProcessor preprocessor) : base(preprocessor) { }

        /// <summary>
        /// Invoked when an analysis needs to be performed.
        /// </summary>
        protected override EventOutput OnAnalyze()
        {
            // Here we will store our results
            var output = new EventOutput(this.Process.Name);

            // This is for mapping pointers to a lock number
            var locks = new Dictionary<long, int>();
            var locki = 0;

            // Export every event
            foreach (var ev in this.LockAcquisitions)
            {
                if (!locks.ContainsKey(ev.Lock0))
                    locks[ev.Lock0] = ++locki;
                if (!locks.ContainsKey(ev.Lock1))
                    locks[ev.Lock1] = ++locki;

                output.Add(ev.Type.ToString().ToLowerInvariant(), this.Process.Name, "", ev.TimeStamp, locks[ev.Lock0], ev.ThreadId, ev.ProcessId, ev.ProcessorNumber, 0);
                output.Add(ev.Type.ToString().ToLowerInvariant(), this.Process.Name, "", ev.TimeStamp, locks[ev.Lock1], ev.ThreadId, ev.ProcessId, ev.ProcessorNumber, 0);
            }

            // Return the results
            return output;
        }

    }
}
