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
    public class JsonExporter : EventExporter
    {
        /// <summary>
        /// Default JSON Exporter.
        /// </summary>
        public static readonly JsonExporter Default = new JsonExporter();

        /// <summary>
        /// Exports the data.
        /// </summary>
        /// <param name="source">The source output to export.</param>
        /// <param name="destination">The output destination.</param>
        public override void Export(EventOutput source, StreamWriter destination)
        {
            destination.Write(JsonConvert.SerializeObject(source, Formatting.Indented));
        }
    }


}
