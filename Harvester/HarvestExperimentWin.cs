//#define OVERESTIMATE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.IO;
using System.Text.RegularExpressions;
using Harvester.Properties;
using Diagnostics.Tracing;
using System.Diagnostics;
using Harvester.Analysis;

namespace Harvester
{
    /// <summary>
    /// Represents the main class.
    /// </summary>
    public class HarvestExperimentWin : HarvestExperiment
    {
        #region Constructors
        public HarvestExperimentWin() : base(){}
        public HarvestExperimentWin(string name) : base(name){}
        #endregion

        /// <summary>
        /// Prepares the files in the experiment directory.
        /// </summary>
        /// <param name="processName">The process to analyze.</param>
        /// <param name="os">The operating system data collector.</param>
        /// <param name="pcm">The hardware counters data collector.</param>
        public override void Merge(string processName, HarvestProcess pcm, HarvestProcess os)
        {
            // Files
            var pcmCsv = Path.Combine(this.WorkingDir.FullName, "raw-pcm.csv");
            var etlZip = Path.Combine(this.WorkingDir.FullName, "raw-os.zip");
            var etlFile = Path.Combine(this.WorkingDir.FullName, "PerfMonitorOutput.etl");
            var etlxFile = Path.Combine(this.WorkingDir.FullName, "PerfMonitorOutput.etlx");

            // Move the files
            if(!File.Exists(pcmCsv))
                File.Move(pcm.Stdout.FullName, pcmCsv);
            if (!File.Exists(etlZip))
            {
                File.Move(Path.Combine(os.Executable.Directory.FullName, "PerfMonitorOutput.etl.zip"), etlZip);

                // Extract etl zip
                try
                {
                    ZipFile.ExtractToDirectory(etlZip, this.WorkingDir.FullName);
                }
                catch { }
            }

            // Load the etl and transform
            if (!File.Exists(etlxFile))
                etlxFile = TraceLog.CreateFromETL(etlFile);

            // Open it now
            var traceLog = TraceLog.OpenOrConvert(etlxFile);

            // Get the proces to monitor
            var process = traceLog.Processes
             .Where(p => p.Name.StartsWith(processName))
             .FirstOrDefault();

            // Get the threads
            var threads = process.Threads
                .ToArray();

            // Get all context switches
            var switches = traceLog.Events
                .Where(e => e.EventName.StartsWith("Thread/CSwitch"))
                .Select(sw => new ContextSwitch(sw))
                .ToArray();

            var majorFaults = traceLog.Events
                .Where(e => e.EventName.StartsWith("PageFault/HardPageFault"))
                .Select(pf => new PageFault(pf))
                .ToArray();

            var minorFaults = traceLog.Events
                .Where(e => e.EventName.StartsWith("PageFault/DemandZeroFault"))
                .Select(pf => new PageFault(pf))
                .ToArray();

            // Get all hardware counters
            var counters = TraceCounter.FromFile(pcmCsv, process.StartTime.Year, process.StartTime.Month, process.StartTime.Day)
                .ToArray();

            // Get the time
            var timeStart = new DateTime(Math.Max(process.StartTime.Ticks, counters.First().TIME.Ticks)); //counters.First().TIME;
            var timeEnd = new DateTime(Math.Min(process.EndTime.Ticks, counters.Last().TIME.Ticks)); ;//counters.Last().TIME;
            var timeSpan = timeEnd - timeStart;

            // Create a new experiment
            var experiment = new Experiment(traceLog, counters);

            // Upsample the experiment
            experiment.Upsample(processName);


            // Create output
            var output = new HarvestOutput();

            // A last switch per core
            var lastSwitch = new Dictionary<int, int>();
            for (int coreID = 0; coreID < counters[0].Core.Length; ++coreID)
                lastSwitch.Add(coreID, 0);


            // Compute the average from the run before the process
            var l1noise = 0.0;
            var l2noise = 0.0;
            var l3noise = 0.0;
            try
            {
                l1noise = counters.Where(t => t.TIME < process.StartTime).Select(c => c.L2HIT + c.L2MISS).Average();
                l2noise = counters.Where(t => t.TIME < process.StartTime).Select(c => c.L2MISS).Average();
                l3noise = counters.Where(t => t.TIME < process.StartTime).Select(c => c.L3MISS).Average();
            }
            catch{ } // Ignore, the noise will be just zero

            // The interval of upsampling
            const int span = 5;

            // Upsample at the specified interval
            for (int t = 0; t < timeSpan.TotalMilliseconds; t += span)
            {
                var analysisBegin = DateTime.Now;

                // The interval starting time
                var timeFrom = timeStart + TimeSpan.FromMilliseconds(t);
                var timeTo = timeStart + TimeSpan.FromMilliseconds(t + span);


                // Get corresponding hardware counters
                var hw = counters.Where(c => c.TIME <= timeTo).Last();

                // Get corresponding context switches
                var cs = switches
                    .Where(e => e.TimeStamp >= timeFrom && e.TimeStamp <= timeTo);

                // For each core
                for (int coreID = 0; coreID < hw.Core.Length; ++coreID)
                {
                    var hwcore = hw.Core[coreID];
                    var events = cs.Where(e => e.ProcessorNumber == coreID);
                        
                    // Get the cache misses
                    var l1miss = (hwcore.L2HIT + hwcore.L2MISS);
                    var l2miss = hwcore.L2MISS;
                    var l3miss = hwcore.L3MISS;

                    // Get the amount of cycles lost
                    var l2perf = hwcore.L2CLK;
                    var l3perf = hwcore.L3CLK;

                    // Get the instructions per cycle
                    var ipc = hwcore.IPC;

   
                    // We got some events, calculate the proportion
                    var fileTime = timeFrom.ToFileTime();

                    // A share for each thread.
                    var threadWork = new Dictionary<int, double>();
                    threadWork.Add(0, 0);
                    foreach (var thread in threads)
                        threadWork.Add(thread.ThreadID, 0);

                    double totalWork  = 0;
                    long timeMarker  = 0;
                    foreach (var sw in events)
                    {
                        // Old thread id & process id
                        var tid = sw.OldThreadId;
                        var pid = sw.OldProcessId;
                        var ntid = sw.NewThreadId;
                        var npid = sw.NewProcessId;
                        var state = sw.State;
                        var elapsed = sw.TimeStamp100ns - fileTime;

                        if (state != ThreadState.Running)
                        {
                            // Last time we started
                            timeMarker = elapsed;

                            // Currently running thread
                            lastSwitch[coreID] = npid != process.ProcessID ? 0 : ntid;
                        }
                        else
                        {
                            //Console.WriteLine("time={0}, {5}, pid={1} tid={2} npid={3} ntid={4} ", elapsed, pid, tid, npid, ntid, state);
                            var runningTime = elapsed - timeMarker;

                            if (pid != process.ProcessID)
                            {
                                // System
                                threadWork[0] += (runningTime / 10000);
                                totalWork += (runningTime / 10000);
                            }
                            else
                            {
                                // Our target thread
                                threadWork[tid] += runningTime;
                                totalWork += runningTime;
                            }
                        }
                    }

                    // If there was no switches during this period of time, take the last running
                    if (totalWork == 0)
                    {
                        var runningThread = lastSwitch[coreID];
                        threadWork[runningThread] = 10000;
                        totalWork = 10000;
                    }


                    // Compute the number of busy threads
                    int busyThreads = 0;
                    foreach (var threadID in threadWork.Keys)
                        if (threadID != 0 && threadWork[threadID] != 0)
                            busyThreads++;



                    // Compute a proportion
                    var systemShare = threadWork[0] / totalWork;
#if OVERESTIMATE
                    systemShare = 0;
                    totalWork = threadWork.Skip(1).Sum(w => w.Value);
#endif
                    
                    // Add proportion per thread
                    foreach (var threadID in threadWork.Keys)
                    {
                        if (threadID == 0)
                            continue;

                        var value = threadWork[threadID];
                        if (value != 0)
                        {
                            // Compute a proportion
                            var threadShare = value / totalWork;
      
                           
                            // Add system events
                            output.Add("l1miss", process.Name, "user", timeFrom.Ticks, Math.Round(threadShare * l1miss), threadID, process.ProcessID, coreID, 1);
                            output.Add("l2miss", process.Name, "user", timeFrom.Ticks, Math.Round(threadShare * l2miss), threadID, process.ProcessID, coreID, 1);
                            output.Add("l3miss", process.Name, "user", timeFrom.Ticks, Math.Round(threadShare * l3miss), threadID, process.ProcessID, coreID, 1);
                            output.Add("l2perf", process.Name, "user", timeFrom.Ticks, threadShare * l2perf, threadID, process.ProcessID, coreID, 1);
                            output.Add("l3perf", process.Name, "user", timeFrom.Ticks, threadShare * l3perf, threadID, process.ProcessID, coreID, 1);
                            output.Add("ipc",    process.Name, "user", timeFrom.Ticks, threadShare * ipc   , threadID, process.ProcessID, coreID, 1);
                            
                        }
                    }

                    // Add system events
                    output.AddSystem("l1miss", timeFrom.Ticks, Math.Round(systemShare * l1miss), coreID);
                    output.AddSystem("l2miss", timeFrom.Ticks, Math.Round(systemShare * l2miss), coreID);
                    output.AddSystem("l3miss", timeFrom.Ticks, Math.Round(systemShare * l3miss), coreID);
                    output.AddSystem("l2perf", timeFrom.Ticks, systemShare * l2perf, coreID);
                    output.AddSystem("l2perf", timeFrom.Ticks, systemShare * l3perf, coreID);
                    output.AddSystem("ipc",    timeFrom.Ticks, systemShare * ipc   , coreID);
                }


                try
                {
                    Console.WriteLine("ETA: {0}", TimeSpan.FromMilliseconds((((DateTime.Now - analysisBegin).TotalMilliseconds / span) * (timeSpan.TotalMilliseconds - t))).ToString(@"h\h\ m\m\ s\s"));
                }
                catch (Exception) { }
            }

            // Write the output
            output.Save(Path.Combine(this.WorkingDir.FullName, "output.csv"));
            output.WriteByThread(Path.Combine(this.WorkingDir.FullName, "outputByThread.csv"));

        }



    }
}
