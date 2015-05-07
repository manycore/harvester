using Diagnostics.Tracing;
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
    /// Represents a base class that exports the data to a particular format.
    /// </summary>
    public abstract class EventExporter
    {
        /// <summary>
        /// Exports the data.
        /// </summary>
        /// <param name="source">The source output to export.</param>
        /// <param name="destination">The output destination.</param>
        public abstract void Export(EventOutput source, StreamWriter destination);

        /// <summary>
        /// Exports the data to a file.
        /// </summary>
        /// <param name="source">The source to export.</param>
        /// <param name="destination">The destination.</param>
        public void ExportToFile(EventOutput source, string destination)
        {
            using (var fs = File.Create(destination))
            using (var sw = new StreamWriter(fs))
            {
                this.Export(source, sw);
            }
        }

        /// <summary>
        /// Gets the name to export.
        /// </summary>
        public virtual string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the extension to export.
        /// </summary>
        public virtual string Extension
        {
            get { return "json"; }
        }
    }


}
