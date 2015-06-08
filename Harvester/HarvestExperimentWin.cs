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
using Analyzer = System.Collections.Generic.KeyValuePair<Harvester.Analysis.EventProcessor, Harvester.Analysis.EventExporter>;

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
        /// <param name="name">The friendly name to use.</param>
        /// <param name="os">The operating system data collector.</param>
        /// <param name="pcm">The hardware counters data collector.</param>
        public override void Merge(string processName, string name, HarvestProcess pcm, HarvestProcess os)
        {
            // Files
            var pcmCsv = Path.Combine(this.WorkingDir.FullName, "raw-pcm.csv");
            var etlZip = Path.Combine(this.WorkingDir.FullName, "raw-os.zip");
            var etlFile = Path.Combine(this.WorkingDir.FullName, "PerfMonitorOutput.etl");
            var etlxFile = Path.Combine(this.WorkingDir.FullName, "PerfMonitorOutput.etlx");

            // Move the files
            if(!File.Exists(pcmCsv))
                File.Copy(pcm.Output.FullName, pcmCsv);
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

            var hwe = traceLog.Events
                .Where(e => e.ProcessID == process.ProcessID)
                .Select(e => e.EventName);

            var events = traceLog.Events.ToArray();

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

            // Create a new experiment
            var analyzers = new Analyzer[]{
                new Analyzer(new DataLocalityProcessor(traceLog, counters), DataLocalityExporter.Default),
                new Analyzer(new StateProcessor(traceLog, counters), new JsonExporter() { Name = "states" }),
                new Analyzer(new SwitchProcessor(traceLog, counters), new JsonExporter() { Name = "switches" })
            };

            // 50 ms window
            const int window = 50;
            foreach (var analyzer in analyzers)
            {
                var processor = analyzer.Key;
                var exporter = analyzer.Value;

                Console.WriteLine("Processing: " + process.GetType().Name);
                var output = processor.Analyze(processName, window);

                // Write the output
                output.Save(Path.Combine(this.WorkingDir.FullName, "output.csv"));
                output.WriteByThread(Path.Combine(this.WorkingDir.FullName, "outputByThread.csv"));

                // Export
                var fname = exporter.Name == null 
                    ? name.ToLower()
                    : String.Format("{0}.{1}", name.ToLower(), exporter.Name);

                exporter.ExportToFile(output, Path.Combine(this.WorkingDir.FullName, fname + "." + exporter.Extension));
            }

        }



    }
}
