using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester.Analysis
{
 

    /// <summary>
    /// Represents a single entry in an output csv file.
    /// </summary>
    public class EventEntry
    {
        /// <summary>
        /// The type of the event.
        /// </summary>
        [JsonProperty("type")]
        public string Type;

        /// <summary>
        /// The program name
        /// </summary>
        [JsonProperty("program")]
        public string Program;

        /// <summary>
        /// The user name.
        /// </summary>
        [JsonProperty("user")]
        public string User;

        /// <summary>
        /// The time of the event.
        /// </summary>
        [JsonProperty("time")]
        public long Time;

        /// <summary>
        /// The measured value of the event.
        /// </summary>
        [JsonProperty("value")]
        public double Value;

        /// <summary>
        /// The thread number of the event.
        /// </summary>
        [JsonProperty("tid")]
        public int Tid;

        /// <summary>
        /// The process number of the event.
        /// </summary>
        [JsonProperty("pid")]
        public int Pid;

        /// <summary>
        /// The processor number of the event.
        /// </summary>
        [JsonProperty("cid")]
        public int Cpu;

        /// <summary>
        /// The user number of the event.
        /// </summary>
        [JsonProperty("uid")]
        public int Uid;

        /// <summary>
        /// Converts the entry to a CSV string.
        /// </summary>
        /// <returns>The string in a CSV format.</returns>
        public string ToCsvString()
        {
            return String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}", Time, User, Program, Tid, Pid, Cpu, Uid, Type, Value);
        }

    }

}