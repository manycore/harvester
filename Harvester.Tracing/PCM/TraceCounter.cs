using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

namespace Diagnostics.Tracing
{
    public class TraceCounter
    {
        public TraceCounter(int core, DateTime time, double duration, TraceCounterType type, double value)
        {
            this.Core = core;
            this.Time = time;
            this.Duration = duration;
            this.Type = type;
            this.Value = value;
        }

        /// <summary>
        /// The core number for this event counter
        /// </summary>
        public int Core;

        /// <summary>
        /// The time of this event counter
        /// </summary>
        public DateTime Time;

        /// <summary>
        /// The type of the counter
        /// </summary>
        public TraceCounterType Type;

        /// <summary>
        /// The value of the counter
        /// </summary>
        public double Value;

        /// <summary>
        /// The duration over which the counters were calculated, in milliseconds
        /// </summary>
        public double Duration;

        public override string ToString()
        {
            return string.Format("{0}: {1}", Type, Value);
        }

        /// <summary>
        /// Read the hardware counters from file.
        /// </summary>
        public static IEnumerable<TraceCounter> FromFile(string path, int year, int month, int day)
        {
            Console.WriteLine("Entering FromFile...");
            Console.Out.Flush();
            using (var reader = new StreamReader(path, Encoding.UTF8))
            {
                string text; 
                while ((text = reader.ReadLine()) != null)
                {
                    // Parse one line
                    if (text.StartsWith("BEGIN"))
                        continue;
                    var line = text.Split(';');
                    if (line.Length < 3)
                        continue;

                    //Console.Write("Reading line:");
                    //foreach (string c in line) { Console.Write(c + " | "); }
                    //Console.Out.Flush();

                    // Get the time and the duration
                    //Console.WriteLine("Parsing date...");
                    var time = ParseTime(line[0], year, month, day);
                    //Console.WriteLine("Parsing duration...");
                    var duration = Double.Parse(line[1], CultureInfo.InvariantCulture);

                    // We should start reading at this offset
                    var offset = 3;

                    // Events that represent cache
                    if (line[2] == "CACHE")
                    {
                        // The lenght of the core entry
                        //var recordLength = 8;
                        var recordLength = 11; // Increased counter to accommodate missing data items
                        var coreCount = (int)Math.Floor((double)(line.Length - offset) / recordLength);
                        for (int core = 0; core < coreCount; ++core)
                        {
                            var i = offset + (core * recordLength);

                            //Console.WriteLine("Parsing record for core " + core.ToString() + "...");
                            //Console.Out.Flush();
                            // Get the values for the cache
                            var ipc     = Double.Parse(line[i + 0], CultureInfo.InvariantCulture);
                            var clock   = Double.Parse(line[i + 1], CultureInfo.InvariantCulture);
                            var l3miss  = Double.Parse(line[i + 2], CultureInfo.InvariantCulture);
                            var l2miss  = Double.Parse(line[i + 3], CultureInfo.InvariantCulture);
                            var l3hit   = Double.Parse(line[i + 4], CultureInfo.InvariantCulture);
                            var l2hit   = Double.Parse(line[i + 5], CultureInfo.InvariantCulture);
                            var l3clock = Double.Parse(line[i + 6], CultureInfo.InvariantCulture);
                            var l2clock = Double.Parse(line[i + 7], CultureInfo.InvariantCulture);
                            var l2invalidation = Double.Parse(line[i + 8], CultureInfo.InvariantCulture);
                            var l1invalidation = Double.Parse(line[i + 9], CultureInfo.InvariantCulture);
                            var drambw = Double.Parse(line[i + 10], CultureInfo.InvariantCulture);

                            yield return new TraceCounter(core, time, duration, TraceCounterType.IPC, ipc);
                            yield return new TraceCounter(core, time, duration, TraceCounterType.Cycles, clock);
                            yield return new TraceCounter(core, time, duration, TraceCounterType.L3Miss, l3miss);
                            yield return new TraceCounter(core, time, duration, TraceCounterType.L2Miss, l2miss);
                            yield return new TraceCounter(core, time, duration, TraceCounterType.L3Hit, l3hit);
                            yield return new TraceCounter(core, time, duration, TraceCounterType.L2Hit, l2hit);
                            yield return new TraceCounter(core, time, duration, TraceCounterType.L3Clock, l3clock);
                            yield return new TraceCounter(core, time, duration, TraceCounterType.L2Clock, l2clock);
                            yield return new TraceCounter(core, time, duration, TraceCounterType.L2Invalidation, l2invalidation);
                            yield return new TraceCounter(core, time, duration, TraceCounterType.L1Invalidation, l1invalidation);
                            yield return new TraceCounter(core, time, duration, TraceCounterType.DramBW, drambw);
                            
                        }

                        /*
                        for (int recOffset = -4; recOffset < 0; recOffset++)
                        {
                            var line[line.Length + recOffset];
                        }
                        */
                    }

                    // Events that represent TLB
                    if (line[2] == "TLB")
                    {
                        // The lenght of the core entry
                        var recordLength = 4;
                        var coreCount = (int)Math.Floor((double)(line.Length - offset) / recordLength); 
                        for (int core = 0; core < coreCount; ++core)
                        {
                            var i = offset + (core * recordLength);

                            // Get the values for the tlb
                            var ipc      = Double.Parse(line[i + 0], CultureInfo.InvariantCulture);
                            var clock    = Double.Parse(line[i + 1], CultureInfo.InvariantCulture);
                            var tlbMiss  = Double.Parse(line[i + 2], CultureInfo.InvariantCulture);
                            var tlbClock = Double.Parse(line[i + 3], CultureInfo.InvariantCulture);

                            yield return new TraceCounter(core, time, duration, TraceCounterType.IPC, ipc);
                            yield return new TraceCounter(core, time, duration, TraceCounterType.Cycles, clock);
                            yield return new TraceCounter(core, time, duration, TraceCounterType.TLBClock, tlbClock);
                            yield return new TraceCounter(core, time, duration, TraceCounterType.TLBMiss, tlbMiss);
                        }
                    }



                    //yield return new TraceCounter(csv, year, month, day);
                }
            }
        }

        /// <summary>
        /// Parses the time in the CSV file.
        /// </summary>
        private static DateTime ParseTime(string text, int year, int month, int day)
        {
            var time = text.Split(':');
            return new DateTime(year, month, day,
                int.Parse(time[0]), int.Parse(time[1]), int.Parse(time[2]), int.Parse(time[3]));
        }


    }

    public enum TraceCounterType
    {
        IPC,
        Cycles,
        L2Miss,
        L3Miss,
        L2Hit,
        L3Hit,
        L2Clock,
        L3Clock,
        TLBMiss,
        TLBClock,
        L2Invalidation,
        L1Invalidation,
        DramBW
    }

}

