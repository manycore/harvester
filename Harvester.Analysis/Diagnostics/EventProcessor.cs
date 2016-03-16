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
    /// <summary>
    /// Represents a base class that can performm resampling and transforms
    /// the trace & hardware counters data into queryable data.
    /// </summary>
    public abstract class EventProcessor
    {
        #region Constructors
        protected readonly TraceLog TraceLog;
        protected readonly TraceCounter[] Counters;

        protected DateTime Start;
        protected DateTime End;
        protected TimeSpan Duration;
        protected TimeSpan Interval;
        protected int Count;
        protected int CoreCount;
        protected TraceProcess Process;
        protected TraceThread[] Threads;
        protected EventFrame[] Frames;
        protected PageFault[] Faults;
        protected ContextSwitch[] Switches;
        protected ThreadLifetime[] Lifetimes;
        protected LockAcquisition[] LockAcquisitions;

        // Cache
        private readonly Dictionary<int, ContextSwitch> LookupLastSwitch =
            new Dictionary<int, ContextSwitch>();
        private readonly Dictionary<int, List<ContextSwitch>> LookupSwitchByThread =
            new Dictionary<int, List<ContextSwitch>>();


        /// <summary>
        /// Constructs a new processor for the provided data files.
        /// </summary>
        /// <param name="events">The data file containing events.</param>
        /// <param name="counters">The data file containing harware coutnters.</param>
        public EventProcessor(TraceLog events, TraceCounter[] counters)
        {
            // Add our data sources
            this.TraceLog = events;
            this.Counters = counters;
            this.CoreCount = this.Counters.Max(c => c.Core) + 1;

            // A last switch per core
            for (int i = 0; i < this.CoreCount; ++i)
                this.LookupLastSwitch.Add(i, null);
        }

        /// <summary>
        /// Constructs a new processor for the provided data files.
        /// </summary>
        /// <param name="preprocessor">The preprocessor to use (copies everything)</param>
        public EventProcessor(EventProcessor preprocessor)
        {
            // Copy stuff we already calculated
            this.TraceLog = preprocessor.TraceLog;
            this.Counters = preprocessor.Counters;
            this.Start = preprocessor.Start;
            this.End = preprocessor.End;
            this.Duration = preprocessor.Duration;
            this.Interval = preprocessor.Interval;
            this.Count = preprocessor.Count;
            this.CoreCount = preprocessor.CoreCount;
            this.Process = preprocessor.Process;
            this.Threads = preprocessor.Threads;
            this.Frames = preprocessor.Frames;
            this.Faults = preprocessor.Faults;
            this.Switches = preprocessor.Switches;
            this.Lifetimes = preprocessor.Lifetimes;
            this.LockAcquisitions = preprocessor.LockAcquisitions;

            // A last switch per core
            for (int i = 0; i < this.CoreCount; ++i)
                this.LookupLastSwitch.Add(i, null);
        }
        #endregion

        #region Frame Members
        /// <summary>
        /// Gets the sampling frames. This is used to analyze what the threads were doing 
        /// and splits the data into workable fixed intervals.
        /// </summary>
        /// <param name="processName">The process to analyze.</param>
        /// <param name="interval">The interval (in milliseconds) of a frame.</param>
        /// <returns></returns>
        private EventFrame[] GetFrames(string processName, ushort interval)
        {
            // Get the pid only
            var pid = this.TraceLog.Processes
                .Where(p => p.Name.StartsWith(processName))
                .FirstOrDefault()
                .ProcessID;
      
            // Mark of the start inside the process
            var benchmarkBegin = DateTime.MinValue;
            { 
                var evt = TraceLog.Events
                    .Where(e => e.ProcessID == pid)
                    .Where(e => e.EventName.Contains("BenchmarkBegin"))
                    .FirstOrDefault();
                if(evt != null)
                    benchmarkBegin = evt.TimeStamp;
            }

            // Mark of the end inside the process
            var benchmarkEnd = DateTime.MinValue;
            {
                var evt = TraceLog.Events
                    .Where(e => e.ProcessID == pid)
                    .Where(e => e.EventName.Contains("BenchmarkEnd"))
                    .FirstOrDefault();
                if (evt != null)
                    benchmarkEnd = evt.TimeStamp;
            }

            // Look the process info
            this.Process = this.TraceLog.Processes
                .Where(p => p.Name.StartsWith(processName))
                .FirstOrDefault();

            // Get the threads
            this.Threads = this.Process.Threads
                .ToArray();

            // Define the timeframe of the experiment
            this.Start = benchmarkBegin == DateTime.MinValue 
                ? new DateTime(Math.Max(this.Process.StartTime.Ticks, this.Counters.First().Time.Ticks))
                : benchmarkBegin;
            this.End = benchmarkEnd == DateTime.MinValue 
                ? new DateTime(Math.Min(this.Process.EndTime.Ticks, this.Counters.Last().Time.Ticks))
                : benchmarkEnd;

            this.Duration = this.End - this.Start;
            this.Interval = TimeSpan.FromMilliseconds(interval);
            this.Count = (int)Math.Ceiling(this.Duration.TotalMilliseconds / this.Interval.TotalMilliseconds);

            Console.WriteLine("Analysis: Analyzing {0} process with {1} threads.", this.Process.Name, this.Threads.Length);
            Console.WriteLine("Analysis: duration = {0}", this.Duration);
            Console.WriteLine("Analysis: #cores = {0}", CoreCount);
            Console.WriteLine("Analysis: Creating #{0} frames for {1}ms. interval...", this.Count, this.Interval.TotalMilliseconds);

            // Get all context switches
            var safeWindow = TimeSpan.FromSeconds(1);
            this.Switches = this.TraceLog.Events
                .Where(e => e.EventName.StartsWith("Thread/CSwitch"))
                .Where(e => e.TimeStamp > this.Start - safeWindow)
                .Where(e => e.TimeStamp < this.End + safeWindow)
                .Select(sw => new ContextSwitch(sw))
                .OrderBy(sw => sw.TimeStamp100ns)
                .ToArray();

            // Get all lifetimes
            this.Lifetimes = this.TraceLog.Events
                .Where(e => e.EventName.StartsWith("Thread/Start") || e.EventName.StartsWith("Thread/End"))
                .Where(e => e.TimeStamp > this.Start - safeWindow)
                .Where(e => e.TimeStamp < this.End + safeWindow)
                .Select(e => new ThreadLifetime(e, e.EventName.StartsWith("Thread/Start") ? ThreadLifetimeType.Start : ThreadLifetimeType.End))
                .OrderBy(sw => sw.TimeStamp100ns)
                .ToArray();

            // Gets all page faults 
            this.Faults = this.TraceLog.Events
                .Where(e => e.EventName.StartsWith("PageFault"))
                .Select(e => new PageFault(e))
                .ToArray();

            // Gets all lock acquisition events
            this.LockAcquisitions = this.TraceLog.Events
                .Where(e => e.EventName.EndsWith("LockSuccess") || e.EventName.EndsWith("LockFailure"))
                .Where(e => e.TimeStamp > this.Start - safeWindow)
                .Where(e => e.TimeStamp < this.End + safeWindow)
                .Select(e => new LockAcquisition(e, e.EventName.EndsWith("LockSuccess") ? LockAcquisitionType.Success : LockAcquisitionType.Failure))
                .OrderBy(sw => sw.TimeStamp100ns)
                .ToArray();

            // The list for our results
            var result = new List<EventFrame>();

            // Build a by-thread lookup cache
            for (int i = 0; i < this.Switches.Length;++i)
            {
                var sw = this.Switches[i];
                if (!this.LookupSwitchByThread.ContainsKey(sw.OldThreadId))
                    this.LookupSwitchByThread[sw.OldThreadId] = new List<ContextSwitch>();
                this.LookupSwitchByThread[sw.OldThreadId].Add(sw);
            }

            // Upsample at the specified interval
            var elapsed = 0d;
            for (int i = 0; i < this.Count; ++i)
            {
                // Current time
                var t = (int)this.Interval.TotalMilliseconds * i;

                // The interval starting time
                var timeFrom = this.Start + TimeSpan.FromMilliseconds(t);
                var timeTo = this.Start + TimeSpan.FromMilliseconds(t) + this.Interval;

                // For each core
                for (int core = 0; core < this.CoreCount; ++core)
                {
                    // Print out status
                    var begin = DateTime.Now;
                    var fid = (i * this.CoreCount + core + 1);
                    var fmx = (this.Count * this.CoreCount);
                    var eta = (elapsed / fid ) * (fmx - fid);
                    Console.WriteLine("[{0}] Progress: {1}/{2} ETA: {3}",
                        this.Process.Name,
                        fid, fmx,
                        TimeSpan.FromMilliseconds(eta).ToString());

                    // Get corresponding context switches that happened on that particular core in the specified time frame
                    var cs = this.Switches
                        .Where(e => e.TimeStamp >= timeFrom && e.TimeStamp <= timeTo)
                        .Where(e => e.ProcessorNumber == core)
                        .OrderBy(e => e.TimeStamp100ns);
                    // Console.WriteLine("Analysis: t = {0}, core = {1}, #hw = {2}, #cs = {3}", t, core, hw.Count(), cs.Count());

                    // Get an individual frame
                    var frame = GetFrame(timeFrom, core, cs);
                    //Console.WriteLine(frame.ToTable());
                    result.Add(frame);

                    // Calculate time spent analysing this frame
                    elapsed += (DateTime.Now - begin).TotalMilliseconds;
                }

            }

            // Return the resulting frames
            return result.ToArray();
        }

        /// <summary>
        /// Gets one frame.
        /// </summary>
        private EventFrame GetFrame(DateTime time, int core, IEnumerable<ContextSwitch> switches)
        {
            //Console.WriteLine("\t Get frame");
            // We got some events, calculate the proportion
            var fileTime = time.ToFileTime();
            var process  = this.Process.ProcessID;
            var maxTime  = (double)(this.Interval.TotalMilliseconds * 10000);

            // Construct a new frame
            var frame = new EventFrame(time, this.Interval, core);
            var previous = 0L;



            foreach (var sw in switches)
            {
                // Old thread id & process id
                var oldThread = EventThread.FromTrace(sw.OldThreadId, sw.OldProcessId, this.Process);
                var newThread = EventThread.FromTrace(sw.NewThreadId, sw.NewProcessId, this.Process);
                var state = sw.State;

                // Set the time
                var current = sw.TimeStamp100ns - fileTime;
                var elapsed = current - previous;
                previous = current;

                // What's the current running thread?
                this.LookupLastSwitch[core] = sw;
    
                // How much time a thread switching in spent switched out?                
                var lastSwitchOut = this.LookupSwitchByThread[sw.NewThreadId]
                    .Where(cs => cs.OldProcessId == sw.NewProcessId)
                    .Where(cs => cs.TimeStamp100ns >= fileTime && cs.TimeStamp100ns < sw.TimeStamp100ns)
                    .LastOrDefault();

                // We didn't find it in our interval, find the absolute last
                if(lastSwitchOut == null)
                {
                    lastSwitchOut = this.LookupSwitchByThread[sw.NewThreadId]
                        .Where(cs => cs.OldProcessId == sw.NewProcessId)
                        .Where(cs => cs.TimeStamp100ns < sw.TimeStamp100ns)
                        .LastOrDefault();
                }

                if (lastSwitchOut != null)
                {
                    var switchOutTime = Math.Min((double)(sw.TimeStamp100ns - lastSwitchOut.TimeStamp100ns), maxTime);
                    switch(lastSwitchOut.State)
                    {
                        case ThreadState.Wait:
                        case ThreadState.Ready:
                        case ThreadState.Standby:
                            frame.IncrementTime(newThread, lastSwitchOut.State, switchOutTime);
                            break;
                    }
                }

                // Add to our frame
                frame.IncrementTime(oldThread, ThreadState.Running, elapsed);

                // Increment on-core time
                frame.IncrementOnCoreTime(oldThread, elapsed);
            }

            // If there was no switches during this period of time, take the last running
            if (frame.Total == 0)
            {
                var sw = this.LookupLastSwitch[core];
                var thread = EventThread.FromTrace(sw.NewThreadId, sw.NewProcessId, this.Process);

                // Add to our frame
                frame.IncrementTime(thread, ThreadState.Running, maxTime);

                // Increment on-core time
                frame.IncrementOnCoreTime(thread, maxTime);
            }

            // Get corresponding hardware counters 
            frame.HwCounters = this.GetCounters(core, time, time + this.Interval);

            // Get the page faults within this frame
            frame.PageFaults = this.GetPageFaults(core, time, time + this.Interval);
            //Console.WriteLine("\t Returning frame");
            return frame;
        }

        
        protected virtual PageFault[] GetPageFaults(int core, DateTime from, DateTime to)
        {
            return this.Faults
                .Where(e => e.Process == this.Process.ProcessID)
                .Where(e => e.TimeStamp >= from && e.TimeStamp <= to)
                .Where(e => e.ProcessorNumber == core)
                .ToArray();
        }


        /// <summary>
        /// Gets the counters for the specified core and time period.
        /// </summary>
        /// <param name="core">The core number.</param>
        /// <param name="from">The start time.</param>
        /// <param name="to">The end time.</param>
        /// <returns>The counters for that period.</returns>
        protected virtual EventHwCounters GetCounters(int core, DateTime from, DateTime to)
        {
 
            // Get corresponding hardware counters
            var hardware = this.Counters
                .Where(c => c.Time >= from && c.Time <= to)
                .Where(c => c.Core == core)
                .ToArray();

            // The total duration of the hw counter samples we have
            var duration = hardware
                .Where(c => c.Type == TraceCounterType.IPC)
                .Select(c => c.Duration / 1000)
                .Sum();

            var hwcount = hardware.Count();
            
            // If we don't have anything, return zeroes
            var counters = new EventHwCounters();


            // If we harve hardware counters
            if (hwcount == 0)
                return counters;

            // Absolute numbers
            counters.Cycles   = (long)((hardware.Where(c => c.Type == TraceCounterType.Cycles).Select(c => c.Value).Sum() / duration) * this.Interval.TotalMilliseconds);
            counters.L2Misses = (long)((hardware.Where(c => c.Type == TraceCounterType.L2Miss).Select(c => c.Value).Sum() / duration) * this.Interval.TotalMilliseconds);
            counters.L3Misses = (long)((hardware.Where(c => c.Type == TraceCounterType.L3Miss).Select(c => c.Value).Sum() / duration) * this.Interval.TotalMilliseconds);
            counters.L2Hits   = (long)((hardware.Where(c => c.Type == TraceCounterType.L2Hit).Select(c => c.Value).Sum() / duration) * this.Interval.TotalMilliseconds);
            counters.L3Hits   = (long)((hardware.Where(c => c.Type == TraceCounterType.L3Hit).Select(c => c.Value).Sum() / duration) * this.Interval.TotalMilliseconds);

            // Average or sum depending on the counter
            counters.IPC = hardware.Where(c => c.Type == TraceCounterType.IPC).Select(c => c.Value).Average();
            counters.L2Clock = hardware.Where(c => c.Type == TraceCounterType.L2Clock).Select(c => c.Value).Average();
            counters.L3Clock = hardware.Where(c => c.Type == TraceCounterType.L3Clock).Select(c => c.Value).Average();

            // Computed
            counters.L1Misses = counters.L2Misses + counters.L2Hits;

            // The duration of the TLB events
            duration = hardware
                .Where(c => c.Type == TraceCounterType.TLBMiss)
                .Select(c => c.Duration / 1000)
                .Sum();

            counters.TLBMisses = (long)((hardware.Where(c => c.Type == TraceCounterType.TLBMiss).Select(c => c.Value).Sum() / duration) * this.Interval.TotalMilliseconds);
            counters.TLBClock = hardware.Where(c => c.Type == TraceCounterType.TLBClock).Select(c => c.Value).Average();

            counters.L1Invalidations = (long)((hardware.Where(c => c.Type == TraceCounterType.L1Invalidation).Select(c => c.Value).Sum() / duration) * this.Interval.TotalMilliseconds);
            counters.L2Invalidations = (long)((hardware.Where(c => c.Type == TraceCounterType.L2Invalidation).Select(c => c.Value).Sum() / duration) * this.Interval.TotalMilliseconds);
            counters.DRAMBandwidth = (long)((hardware.Where(c => c.Type == TraceCounterType.DramBW).Select(c => c.Value).Sum() / duration) * this.Interval.TotalMilliseconds);


            if (core == CoreCount - 1)
            {
                counters.BytesReadFromMC = (long)((hardware.Where(c => c.Type == TraceCounterType.BytesReadFromMC).Select(c => c.Value).Sum() / duration) * this.Interval.TotalMilliseconds);
                counters.BytesWrittenToMC = (long)((hardware.Where(c => c.Type == TraceCounterType.BytesWrittenToMC).Select(c => c.Value).Sum() / duration) * this.Interval.TotalMilliseconds);
                counters.IncomingQPI = (long)((hardware.Where(c => c.Type == TraceCounterType.IncomingQPI).Select(c => c.Value).Sum() / duration) * this.Interval.TotalMilliseconds);
                counters.OutgoingQPI = (long)((hardware.Where(c => c.Type == TraceCounterType.OutgoingQPI).Select(c => c.Value).Sum() / duration) * this.Interval.TotalMilliseconds);
            }

            return counters;
        }
        #endregion

        #region Analyze Members
        /// <summary>
        /// Invoked when an analysis needs to be performed.
        /// </summary>
        /// <returns>The ouptut of the analysis.</returns>
        protected abstract EventOutput OnAnalyze();

        /// <summary>
        /// Analyzes the process
        /// </summary>
        /// <param name="processName"></param>
        /// <param name="interval"></param>
        public EventOutput Analyze(string processName, ushort interval)
        {
            // First we need to gather frames
            if (this.Frames == null)
            {
                Console.WriteLine("Analysis: Preprocessing...");
                this.Frames = this.GetFrames(processName, interval);
            }

            Console.WriteLine("Analysis: Performing analysis...");
            return this.OnAnalyze();
        }
        #endregion
    }


}
