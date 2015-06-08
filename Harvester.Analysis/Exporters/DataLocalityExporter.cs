using Diagnostics.Tracing;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester.Analysis
{
    /// <summary>
    /// Exports the data to JSON format per thread.
    /// </summary>
    public class DataLocalityExporter : EventExporter
    {
        /// <summary>
        /// Default JSON Exporter.
        /// </summary>
        public static readonly DataLocalityExporter Default = new DataLocalityExporter();

        /// <summary>
        /// Gets the export extension.
        /// </summary>
        public override string Extension
        {
            get { return "dl.json"; }
        }

        /// <summary>
        /// Exports the data.
        /// </summary>
        /// <param name="source">The source output to export.</param>
        /// <param name="destination">The output destination.</param>
        public override void Export(EventOutput source, StreamWriter destination)
        {
            var threads = source.Select(e => e.Tid).Distinct().ToArray();
            var types = source.Select(e => e.Type)
                .Distinct()
                .Where(t => t.EndsWith("perf") || t == "ipc" || t == "time")
                .ToArray();
            var min = new DateTime(source.Select(e => e.Time).Min());
            var max = new DateTime(source.Select(e => e.Time).Max());

            // The output and global values
            var output = new JsonOutput();
            output.Name = source.ProcessName;

     
            foreach(var tid in threads)
            {
                var thread = new JsonThread(tid);
                var runtime = new List<double>();
                foreach(var type in types)
                {
                    var measure = new JsonMeasure(type.Replace("perf", String.Empty).ToUpper());
                    foreach (var timeGroup in source.GroupBy(e => e.Time))
                    {
                        // Get the values
                        var values = timeGroup
                            .Where(t => t.Tid == tid && t.Type == type)
                            .Select(e => e.Value)
                            .ToArray();

                        var average = values.Length == 0 ? 0 : values.Average();
                        if (type.EndsWith("perf") || type == "ipc")
                            average *= 100;

                        if (type == "time")
                            runtime.Add(average);
                        else
                            measure.Data.Add(Math.Min(Math.Round(average, 2), 100));
                    }

                    // Add the thread to the output
                    if(measure.Data.Count > 0)
                        thread.Measures.Add(measure);
                }

                // Calculate the lifetime
                thread.RuntimeAvg = runtime.Average() * 100;

                // Add the thread to the output
                output.Threads.Add(thread);
            }

            // Only threads of the process we monitor
            var ownThreads = source.Where(t => t.Pid != 0).ToArray();
            var summableTypes = source
                .Select(e => e.Type)
                .Distinct()
                .Where(t => !t.EndsWith("perf") && !t.EndsWith("ipc") && !t.EndsWith("time"))
                .ToArray();

            // Various variables
            output.Info["duration"] = (max - min).TotalMilliseconds;
            output.Info["frames"] = output.Threads.First().Measures.First().Data.Count;

            foreach (var type in summableTypes)
            {
                output.Info[type] = ownThreads
                    .Where(t => t.Type == type)
                    .Select(t => t.Value)
                    .Sum();
            }
            

            // Sort by runtime and remove the '0' thread
            output.Threads = output.Threads
                .Where(t => t.Id != "0")
                .OrderBy(t => t.RuntimeAvg)
                .ToList();

            // Write 
            destination.WriteLine(JsonConvert.SerializeObject(output));
        }
    }


    #region Json Data Model
    class JsonOutput
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("info")]
        public Dictionary<string, object> Info = new Dictionary<string, object>();

        [JsonProperty("threads")]
        public List<JsonThread> Threads = new List<JsonThread>();
    }

    class JsonThread
    {
        public JsonThread(int id)
        {
            this.Id = id.ToString();
        }

        [JsonProperty("id")]
        public string Id;

        [JsonProperty("runtimeAvg")]
        public double RuntimeAvg;

        [JsonProperty("measures")]
        public List<JsonMeasure> Measures = new List<JsonMeasure>();
    }

    class JsonMeasure
    {
        public JsonMeasure(string name)
        {
            this.Name = name;
        }

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("data")]
        public List<double> Data = new List<double>();
    }
    #endregion
}
