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
    public class NewExporter : EventExporter
    {
        /// <summary>
        /// Default JSON Exporter.
        /// </summary>
        public static readonly NewExporter Default = new NewExporter();

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
                .ToArray();

            var min = new DateTime(source.Select(e => e.Time).Min());
            var max = new DateTime(source.Select(e => e.Time).Max());

            // The output and global values
            var output = new JsonOutput();
            output.Name = source.ProcessName;

            var sb = new StringBuilder();
            foreach(var e in source)
            {
                sb.AppendLine(e.Time + ", " + e.Tid + ", " + e.Cpu + ", " + e.Value);
            }


            // Write 
            destination.Write(sb.ToString());
        }
    }


}
