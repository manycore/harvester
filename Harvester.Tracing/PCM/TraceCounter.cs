using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

namespace Diagnostics.Tracing
{
    public class TraceCounter
    {
        /// <summary>
        /// Constructs a parsed entry.
        /// </summary>
        private TraceCounter(string[] line, int year, int month, int day, DateTime previousTime)
        {
            // The lenght of system-wide entry
            int systemLength = 20;

            // The lenght of the core entry
            int coreLength = 10;

            // Calculate the number of cores we have in the file
            int cores = (line.Length - systemLength) / coreLength;
            this.Core = new TraceCounterCore[cores];

            // Parse system-wide
            // TIME	EXEC	IPC	FREQ	AFREQ	L3MISS	L2MISS	L3HIT	L2HIT	L3CLK	L2CLK	READ	WRITE	INST	ACYC	TICKS	IPC	INST	MAXIPC

            this.TIME = ParseTime(line[0], year, month, day);

            this.EXEC = Double.Parse(line[1], CultureInfo.InvariantCulture);
            this.IPC = Double.Parse(line[2], CultureInfo.InvariantCulture);
            this.FREQ = Double.Parse(line[3], CultureInfo.InvariantCulture);
            this.AFREQ = Double.Parse(line[4], CultureInfo.InvariantCulture);
            this.L3MISS = Int64.Parse(line[5], CultureInfo.InvariantCulture);
            this.L2MISS = Int64.Parse(line[6], CultureInfo.InvariantCulture);
            this.L3HIT = Int64.Parse(line[7], CultureInfo.InvariantCulture);
            this.L2HIT = Int64.Parse(line[8], CultureInfo.InvariantCulture);
            this.L3CLK = Double.Parse(line[9], CultureInfo.InvariantCulture);
            this.L2CLK = Double.Parse(line[10], CultureInfo.InvariantCulture);
            this.READ = Double.Parse(line[11], CultureInfo.InvariantCulture);
            this.WRITE = Double.Parse(line[12], CultureInfo.InvariantCulture);
            this.INST = Int64.Parse(line[13], CultureInfo.InvariantCulture);
            this.ACYC = Int64.Parse(line[14], CultureInfo.InvariantCulture);
            this.TICKS = Int64.Parse(line[15], CultureInfo.InvariantCulture);
            this.IPC2 = Double.Parse(line[16], CultureInfo.InvariantCulture);
            this.RINST = Double.Parse(line[17], CultureInfo.InvariantCulture);
            this.MAXIPC = Int64.Parse(line[18], CultureInfo.InvariantCulture);


            for (int i = 0; i < cores; ++i)
            {
                int offset = systemLength + (i * coreLength);
                this.Core[i] = new TraceCounterCore();
                this.Core[i].IPC = Double.Parse(line[offset], CultureInfo.InvariantCulture);
                this.Core[i].FREQ = Double.Parse(line[offset + 1], CultureInfo.InvariantCulture);
                this.Core[i].AFREQ = Double.Parse(line[offset + 2], CultureInfo.InvariantCulture);
                this.Core[i].L3MISS = Int64.Parse(line[offset + 3], CultureInfo.InvariantCulture);
                this.Core[i].L2MISS = Int64.Parse(line[offset + 4], CultureInfo.InvariantCulture);
                this.Core[i].L3HIT = Int64.Parse(line[offset + 5], CultureInfo.InvariantCulture);
                this.Core[i].L2HIT = Int64.Parse(line[offset + 6], CultureInfo.InvariantCulture);
                this.Core[i].L3CLK = Double.Parse(line[offset + 7], CultureInfo.InvariantCulture);
                this.Core[i].L2CLK = Double.Parse(line[offset + 8], CultureInfo.InvariantCulture);
            }

            // Calculate the duration
            this.Duration = previousTime != DateTime.MinValue
                ? (this.TIME - previousTime).TotalMilliseconds
                : 2d;
        }

        public readonly DateTime TIME;
        public readonly double EXEC;
        public readonly double IPC;
        public readonly double FREQ;
        public readonly double AFREQ;
        public readonly long L3MISS;
        public readonly long L2MISS;
        public readonly long L3HIT;
        public readonly long L2HIT;
        public readonly double L3CLK;
        public readonly double L2CLK;
        public readonly double READ;
        public readonly double WRITE;
        public readonly long INST;
        public readonly long ACYC;
        public readonly long TICKS;
        public readonly double IPC2;
        public readonly double RINST;
        public readonly long MAXIPC;

        /// <summary>
        /// The duration over which the counters were calculated, in milliseconds
        /// </summary>
        public double Duration;

        /// <summary>
        /// The specific counters for each core.
        /// </summary>
        public readonly TraceCounterCore[] Core;

        /// <summary>
        /// Read the hardware counters from file.
        /// </summary>
        public static IEnumerable<TraceCounter> FromFile(string path, int year, int month, int day)
        {
            var foundBeginning = false;
            var previousTime = DateTime.MinValue;
            using (var reader = new StreamReader(path, Encoding.UTF8))
            {
                string line; 
                var idx = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    ++idx;
                    if (!foundBeginning)
                    {
                        // Make sure we found a beginning
                        foundBeginning = line.StartsWith("TIME");
                        continue;
                    }

                    // Parse one line
                    var csv = line.Split(';');
                    yield return new TraceCounter(csv, year, month, day, previousTime);

                    // Set the previous time
                    previousTime = ParseTime(csv[0], year, month, day);
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
}

