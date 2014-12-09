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
        public readonly Dictionary<int, int> LastSwitch =
            new Dictionary<int, int>();

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

            // A last switch per core
            for (int coreID = 0; coreID < counters[0].Core.Length; ++coreID)
                this.LastSwitch.Add(coreID, 0);
        }

        public void Upsample(string processName)
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
            this.Interval = TimeSpan.FromMilliseconds(5);
            this.Count = (int)Math.Ceiling(this.Duration.TotalMilliseconds / this.Interval.TotalMilliseconds);
            this.CoreCount = this.Counters[0].Core.Length;


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

            // Upsample at the specified interval
            for (int i = 0; i < this.Count; ++i)
            {
                // Current time
                var t = (int)this.Interval.TotalMilliseconds * i;

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


                    //Console.WriteLine("Analysis: t = {0}, core = {1}, #hw = {2}, #cs = {3}", t, core, hw.Count(), cs.Count());

                    GetProportion(timeFrom, core, cs, hw);
                }

            }
        }

        private Dictionary<int, double> GetProportion(DateTime time, int core, IEnumerable<ContextSwitch> switches, IEnumerable<TraceCounter> counters)
        {
            // We got some events, calculate the proportion
            var fileTime = time.ToFileTime();
            var process  = this.Process.ProcessID;
            var maxTime  = (double)(this.Interval.TotalMilliseconds * 10000);

            var set = new ThreadSet();
            var threadWork = new Dictionary<int, double>();
            threadWork.Add(0, 0);
            foreach (var thread in this.Threads)
                threadWork.Add(thread.ThreadID, 0);

            double totalWork = 0;
            long timeMarker = 0;

            long previous = 0;

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


                set.Add(tid, state, elapsed);
                //Console.WriteLine(sw);
                
                /*if (state != ThreadState.Running)
                {
                    
                    // Last time we started
                    timeMarker = previous;

                    // Currently running thread
                    this.LastSwitch[core] = npid != process ? 0 : ntid;
                }
                else
                {
                    var runningTime = previous - timeMarker;
                    set.Add(tid, state, runningTime);

                    //Console.WriteLine("time={0}, run={6}, {5}, pid={1} tid={2} npid={3} ntid={4} ", elapsed, pid, tid, npid, ntid, state, runningTime);

                    if (pid != process)
                    {
                        // System
                        threadWork[0] += runningTime;
                        totalWork += runningTime;
                    }
                    else
                    {
                        // Our target thread
                        threadWork[tid] += runningTime;
                        totalWork += runningTime;
                    }
                }*/
            }

            // If there was no switches during this period of time, take the last running
            if (totalWork == 0)
            {
                var runningThread = this.LastSwitch[core];
                threadWork[runningThread] = maxTime;
                totalWork = maxTime;
            }

            // Calculate a percentage instead of cpu time
            //var kvp = threadWork.ToArray();
            //foreach (var kv in kvp)
            //    threadWork[kv.Key] = kv.Value / totalWork;

            Console.WriteLine(threadWork.Values
                    .Select(v => v.ToString("N2"))
                    .Aggregate((a, b) => a + ", " + b)
                );

            Console.WriteLine(set.ToString());

            return threadWork;
        }
    }
}
