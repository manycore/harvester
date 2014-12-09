using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester.Analysis
{
    public class Thread : IEquatable<Thread>
    {
        /// <summary>
        /// Constructs a new thread container.
        /// </summary>
        public Thread(DateTime start, TimeSpan duration, TimeSpan interval, int tid, int pid, int cpu, int uid)
        {
            this.Values = new TimeSeries<double>(start, duration, interval);
            this.Tid = tid;
            this.Cpu = cpu;
            this.Uid = uid;
            this.Pid = pid;
        }

        /// <summary>
        /// The sampled timeline.
        /// </summary>
        public readonly TimeSeries<double> Values;

        /// <summary>
        /// The thread id of this particular thread.
        /// </summary>
        public readonly int Tid;

        /// <summary>
        /// The process id of this particular thread.
        /// </summary>
        public readonly int Pid;

        /// <summary>
        /// The cpu id of this particular thread.
        /// </summary>
        public readonly int Cpu;

        /// <summary>
        /// The user id of this particular thread.
        /// </summary>
        public readonly int Uid;

        /// <summary>
        /// Checks the equality.
        /// </summary>
        public bool Equals(Thread other)
        {
            return other.Pid == this.Pid && other.Tid == this.Tid && other.Cpu == this.Cpu && other.Uid == this.Uid;
        }

        /// <summary>
        /// Gets the hashcode for this thread.
        /// </summary>
        public override int GetHashCode()
        {
            return this.Pid ^ this.Tid ^ this.Cpu ^ this.Uid;
        }
    }
}
