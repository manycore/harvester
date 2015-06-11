using Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester.Analysis
{
    public class SwitchProcessor : EventProcessor
    {
        /// <summary>
        /// Constructs a new processor for the provided data files.
        /// </summary>
        /// <param name="preprocessor">Preprocessor to use</param>
        public SwitchProcessor(EventProcessor preprocessor) : base(preprocessor) { }

        /// <summary>
        /// Invoked when an analysis needs to be performed.
        /// </summary>
        protected override EventOutput OnAnalyze()
        {
            // Here we will store our results
            var output = new EventOutput(this.Process.Name, this.Start);
            var processes = this.TraceLog.Processes
                .Select(p => new
                {
                    Name = p.Name,
                    Id = p.ProcessID
                })
                .ToArray();

            // Get the context switches
            var contextSwitches = this.Switches
                .Where(e => e.TimeStamp >= this.Start)
                .Where(e => e.TimeStamp <= this.End)
                .ToArray();

            foreach(var sw in contextSwitches)
            {
                var program = processes
                    .Where(p => p.Id == sw.NewProcessId)
                    .First();

                output.Add("sw", program.Name, "", sw.TimeStamp, 1, sw.NewThreadId, sw.NewProcessId, sw.ProcessorNumber, 0);
            }

            // Only export relevant processes
            var lifetimes = this.Lifetimes
                .Where(e => e.ProcessId == this.Process.ProcessID);
            foreach(var lt in lifetimes)
            {
                output.Add(lt.State.ToString().ToLowerInvariant(),
                    this.Process.Name, "", lt.TimeStamp, 1, lt.ThreadId, lt.ProcessId, lt.ProcessorNumber, 0);
            }

            // Return the results
            return output;
        }

    }
}
