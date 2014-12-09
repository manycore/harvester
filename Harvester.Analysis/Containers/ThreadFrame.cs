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
    public sealed class ThreadFrame
    {
        #region Properties
        private readonly Dictionary<int, double[]> Map = new Dictionary<int, double[]>();

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
        public int[] Threads
        {
            get { return this.Map.Keys.ToArray(); }
        }
        #endregion

        #region Public Members
        /// <summary>
        /// Increments the time for a particular thread/state combination.
        /// </summary>
        /// <param name="thread">The thread to increment.</param>
        /// <param name="state">The state of the thread.</param>
        /// <param name="elapsed">The time spent in that state.</param>
        public void Increment(int thread, ThreadState state, double elapsed)
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
            sb.AppendLine("Thread  Init    Ready   Run     StandBy Term    Wait    Trans   Unkn");
            foreach(var thread in this.Map.Keys)
            {
                sb.Append(thread.ToString().PadRight(8, ' '));
                for (int i = 0; i < 8; ++i)
                {
                    sb.Append(this.Map[thread][i].ToString().PadRight(8, ' '));
                }
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
        public double GetTime(int thread, ThreadState state)
        {
            if (!this.Map.ContainsKey(thread))
                throw new ArgumentOutOfRangeException("Thread #" + thread + " is not contained within the set.");
            return this.Map[thread][(int)state];
        }

        /// <summary>
        /// Gets the percentage of time for a particular thread.
        /// </summary>
        /// <param name="thread">The thread to query.</param>
        /// <param name="state">The state to query.</param>
        /// <returns>The relative time elapsed in the frame in that state.</returns>
        public double GetShare(int thread, ThreadState state)
        {
            return this.GetTime(thread, state) / this.Total;
        }

        /// <summary>
        /// Gets the thread time for a particular thread.
        /// </summary>
        /// <param name="thread">The thread to query.</param>
        /// <returns>The time elapsed in the frame in any state.</returns>
        public double GetTime(int thread)
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
        public double GetShare(int thread)
        {
            return this.GetTime(thread) / this.Total;
        }
        #endregion
    }
}
