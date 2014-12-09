using Diagnostics.Tracing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Harvester.Analysis
{
    /// <summary>
    /// Represents a particular time window..
    /// </summary>
    public sealed class TimeSeries<TValue>
    {
        private ConcurrentDictionary<string, TValue>[] Series;
        private DateTime Start;
        private TimeSpan Duration;
        private TimeSpan Interval;

        public TimeSeries(DateTime start, TimeSpan duration, TimeSpan interval)
        {
            // How many in total?
            var count = (int)Math.Ceiling(duration.TotalMilliseconds / interval.TotalMilliseconds);

            // Set the starting time
            this.Start = start;
            this.Duration = duration;
            this.Interval = interval;

            // Initialize all the values that will be used to hold samples
            this.Series = new ConcurrentDictionary<string,TValue>[count];
            for(int i=0; i < count; ++i)
                this.Series[i] = new ConcurrentDictionary<string,TValue>();
        }


        /// <summary>
        /// Gets everything at the specified index.
        /// </summary>
        /// <param name="index">The index to look for.</param>
        /// <returns>The key/value pairs at the specified index.</returns>
        public IDictionary<string, TValue> this[int index]
        {
            get { return Series[index]; }
        }

        /// <summary>
        /// Gets everything at the specified time.
        /// </summary>
        /// <param name="time">The time to look for.</param>
        /// <returns>The key/value pairs at the specified index.</returns>
        public IDictionary<string, TValue> this[DateTime time]
        {
            get 
            {
                // Calculate the index
                var index = (int)Math.Floor((time - this.Start).TotalMilliseconds / this.Interval.TotalMilliseconds);

                // Get by the index
                return Series[index];
            }
        }
    }


}
