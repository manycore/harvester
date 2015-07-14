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
    /// Represents a set of threads with time per state.
    /// </summary>
    public sealed class EventFrame
    {
        #region Constructor & Properties
        private readonly Dictionary<EventThread, double[]> Map
            = new Dictionary<EventThread, double[]>();

        private readonly Dictionary<EventThread, double> OnCoreMap
            = new Dictionary<EventThread, double>();

        /// <summary>
        /// Constructs a new frame.
        /// </summary>
        /// <param name="time">the start time of the frame</param>
        /// <param name="duration">the duration of the frame</param>
        public EventFrame(DateTime time, TimeSpan duration, int core)
        {
            this.Time = time;
            this.Duration = duration;
            this.Core = core;

            // We always add a default thread and the idle thread
            this.Map.Add(EventThread.System, new double[8]);
            this.Map.Add(EventThread.Idle, new double[8]);
        }

        /// <summary>
        /// The core to which the frame belongs.
        /// </summary>
        public int Core
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the hardware counters for this frame.
        /// </summary>
        public EventHwCounters HwCounters
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the page fault events within this frame.
        /// </summary>
        public PageFault[] PageFaults
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the start time of the frame.
        /// </summary>
        public DateTime Time
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the duration of the frame.
        /// </summary>
        public TimeSpan Duration
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the grand total.
        /// </summary>
        public double Total
        {
            get { return Map.Sum(t => t.Value.Sum()); }
        }

        /// <summary>
        /// Gets the threads that are contained within this time set.
        /// </summary>
        public EventThread[] Threads
        {
            get { return this.Map.Keys.ToArray(); }
        }
        #endregion

        #region Public Members
        /// <summary>
        /// Increments the time spend switched on.
        /// </summary>
        /// <param name="thread">The thread to increment.</param>
        /// <param name="elapsed">The time spent in that state.</param>
        public void IncrementOnCoreTime(EventThread thread, double elapsed)
        {
            if (!this.OnCoreMap.ContainsKey(thread))
                this.OnCoreMap.Add(thread, 0);

            this.OnCoreMap[thread] += elapsed;
        }

        /// <summary>
        /// Increments the time for a particular thread/state combination.
        /// </summary>
        /// <param name="thread">The thread to increment.</param>
        /// <param name="state">The state of the thread.</param>
        /// <param name="elapsed">The time spent in that state.</param>
        public void IncrementTime(EventThread thread, ThreadState state, double elapsed)
        {
            if (!this.Map.ContainsKey(thread))
                this.Map.Add(thread, new double[8]);

            this.Map[thread][(int)state] += elapsed;
        }

        /// <summary>
        /// Prints out the timings into a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach(var thread in this.Threads)
            {
                sb.Append(this.GetOnCoreRatio(thread).ToString("N2").PadRight(6));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Prints out the timings into a table.
        /// </summary>
        /// <returns></returns>
        public string ToTable()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Thread    Ready     Run       StandBy   Wait      ");
            foreach (var thread in this.Map.Keys)
            {
                sb.Append(thread.ToString().PadRight(10, ' '));
                sb.Append(this.Map[thread][(int)ThreadState.Ready].ToString().PadRight(10, ' '));
                sb.Append(this.Map[thread][(int)ThreadState.Running].ToString().PadRight(10, ' '));
                sb.Append(this.Map[thread][(int)ThreadState.Standby].ToString().PadRight(10, ' '));
                sb.Append(this.Map[thread][(int)ThreadState.Wait].ToString().PadRight(10, ' '));
                sb.AppendLine();
            }

            sb.AppendLine("Grand Total: " + Total);

            return sb.ToString();
        }
        #endregion

        #region Query Members
        /// <summary>
        /// Gets the thread time for a particular thread.
        /// </summary>
        /// <param name="thread">The thread to query.</param>
        /// <param name="state">The state to query.</param>
        /// <returns>The time elapsed in the frame in that state.</returns>
        public double GetTime(EventThread thread, ThreadState state)
        {
            if (!this.Map.ContainsKey(thread))
                throw new ArgumentOutOfRangeException("Thread #" + thread + " is not contained within the set.");
            return this.Map[thread][(int)state];
        }

        /// <summary>
        /// Gets the thread time for a particular thread.
        /// </summary>
        /// <param name="thread">The thread to query.</param>
        /// <returns>The time elapsed in the frame in any state.</returns>
        public double GetTime(EventThread thread)
        {
            if (!this.Map.ContainsKey(thread))
                throw new ArgumentOutOfRangeException("Thread #" + thread + " is not contained within the set.");
            return this.Map[thread].Sum();
        }

        /// <summary>
        /// Gets the thread time for a particular thread.
        /// </summary>
        /// <param name="thread">The thread to query.</param>
        /// <returns>The time elapsed in the frame in any state.</returns>
        public double GetOnCoreRatio(EventThread thread)
        {
            if (!this.OnCoreMap.ContainsKey(thread))
                return 0;

            // Get the grand total of the time
            var total = this.OnCoreMap.Sum(t => t.Value);

            // Return the current on-core ratio.
            return this.OnCoreMap[thread] / total;
        }
        #endregion
    }

}
