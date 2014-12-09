using Diagnostics.Tracing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester.Analysis
{
    public class Experiment
    {
        public readonly TraceLog TraceLog;
        public readonly TraceCounter[] Counters;
        public readonly Dictionary<int, ContextSwitch> LastSwitch =
            new Dictionary<int, ContextSwitch>();

        public DateTime Start;
        public DateTime End;
        public TimeSpan Duration;
        public TimeSpan Interval;
        public int Count;
        public int CoreCount;
        public TraceProcess Process;
        public TraceThread[] Threads;

        public Experiment(TraceLog events, TraceCounter[] counters)
        {
            // Add our data sources
            this.TraceLog = events;
            this.Counters = counters;
            this.CoreCount = this.Counters[0].Core.Length;

            // A last switch per core
            for (int i = 0; i < this.CoreCount; ++i)
                this.LastSwitch.Add(i, null);
        }

        /// <summary>
        /// Gets the sampling frames. This is used to analyze what the threads were doing 
        /// and splits the data into workable fixed intervals.
        /// </summary>
        /// <param name="processName">The process to analyze.</param>
        /// <param name="interval">The interval (in milliseconds) of a frame.</param>
        /// <returns></returns>
        public ThreadFrame[] GetFrames(string processName, ushort interval)
        {
            // Get the proces to monitor
            this.Process = this.TraceLog.Processes
             .Where(p => p.Name.StartsWith(processName))
             .FirstOrDefault();

            // Get the threads
            this.Threads = this.Process.Threads
                .ToArray();

            // Define the timeframe of the experiment
            this.Start = new DateTime(Math.Max(this.Process.StartTime.Ticks, this.Counters.First().TIME.Ticks));
            this.End = new DateTime(Math.Min(this.Process.EndTime.Ticks, this.Counters.Last().TIME.Ticks));
            this.Duration = this.End - this.Start;
            this.Interval = TimeSpan.FromMilliseconds(interval);
            this.Count = (int)Math.Ceiling(this.Duration.TotalMilliseconds / this.Interval.TotalMilliseconds);

            Console.WriteLine("Analysis: Analyzing {0} process with {1} threads.", this.Process.Name, this.Threads.Length);
            Console.WriteLine("Analysis: duration = {0}", this.Duration);
            Console.WriteLine("Analysis: interval = {0} ms.", this.Interval.TotalMilliseconds);
            Console.WriteLine("Analysis: #samples = {0}", Count);
            Console.WriteLine("Analysis: #cores = {0}", CoreCount);
            
            // Get all context switches
            var switches = this.TraceLog.Events
                .Where(e => e.EventName.StartsWith("Thread/CSwitch"))
                .Select(sw => new ContextSwitch(sw))
                .ToArray();

            // The list for our results
            var result = new List<ThreadFrame>();

            // Upsample at the specified interval
            for (int i = 0; i < this.Count; ++i)
            {
                // Current time
                var t = (int)this.Interval.TotalMilliseconds * i;
                Console.WriteLine("Analysis: Analyzing frame #{0}...", i);

                // The interval starting time
                var timeFrom = this.Start + TimeSpan.FromMilliseconds(t);
                var timeTo = this.Start + TimeSpan.FromMilliseconds(t) + this.Interval;

                // Get corresponding hardware counters
                var hw = this.Counters
                    .Where(c => c.TIME >= timeFrom && c.TIME <= timeTo);

                // For each core
                for (int core = 0; core < this.CoreCount; ++core)
                {
                    // Get corresponding context switches that happened on that particular core in the specified time frame
                    var cs = switches
                        .Where(e => e.TimeStamp >= timeFrom && e.TimeStamp <= timeTo)
                        .Where(e => e.ProcessorNumber == core)
                        .OrderBy(e => e.TimeStamp100ns);
                    // Console.WriteLine("Analysis: t = {0}, core = {1}, #hw = {2}, #cs = {3}", t, core, hw.Count(), cs.Count());

                    // Get an individual frame
                    result.Add(
                        GetFrame(timeFrom, core, cs, hw)
                        );
                }
            }

            // Return the resulting frames
            return result.ToArray();
        }

        /// <summary>
        /// Gets one frame.
        /// </summary>
        private ThreadFrame GetFrame(DateTime time, int core, IEnumerable<ContextSwitch> switches, IEnumerable<TraceCounter> counters)
        {
            // We got some events, calculate the proportion
            var fileTime = time.ToFileTime();
            var process  = this.Process.ProcessID;
            var maxTime  = (double)(this.Interval.TotalMilliseconds * 10000);

            // Construct a new frame
            var frame = new ThreadFrame(time, this.Interval, core);
            var previous = 0L;

            foreach (var sw in switches)
            {
                // Old thread id & process id
                var tid = sw.OldThreadId;
                var pid = sw.OldProcessId;
                var ntid = sw.NewThreadId;
                var npid = sw.NewProcessId;
                var state = sw.State;

                var current = sw.TimeStamp100ns - fileTime;
                var elapsed = current - previous;
                previous = current;

                // What's the current running thread?
                this.LastSwitch[core] = sw;
                //Console.WriteLine(sw);

                // Put everything else in 0
                if (pid != process)
                    tid = 0;

                // Add to our set
                frame.Increment(tid, state, elapsed);
            }

            // If there was no switches during this period of time, take the last running
            if (frame.Total == 0)
            {
                var sw = this.LastSwitch[core];
                var thread = sw.NewProcessId != process ? 0 : sw.NewThreadId;
                frame.Increment(thread, sw.State, maxTime);
            }

            //Console.WriteLine(frame.ToString());

            return frame;
        }

    }
}
