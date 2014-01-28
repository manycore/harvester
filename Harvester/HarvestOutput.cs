using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester
{
    public class HarvestOutput : List<HarvestOutputEntry>
    {
        /// <summary>
        /// Adds a single entry.
        /// </summary>
        public void Add(string type, string program, string user, long time,double value, int tid, int pid, int cpu, int uid)
        {
            this.Add(new HarvestOutputEntry(type, program, user, time, value, tid, pid, cpu, uid));
        }

        /// <summary>
        /// Adds en entry for the system
        /// </summary>
        public void AddSystem(string type, long time, double value, int cpu)
        {
            this.Add(new HarvestOutputEntry(type, "system", "root", time, value, 0, 0, cpu, 0));

        }

        /// <summary>
        /// Saves the output in a CSV format.
        /// </summary>
        /// <param name="path">The file to save into.</param>
        public void Save(string path)
        {
            // Write all lines as a csv file
            File.WriteAllLines(path, this.Select(e => e.ToCsvString()));
        }

        /// <summary>
        /// Writes the output to a csv file.
        /// </summary>
        /// <param name="path"></param>
        public void WriteByThread(string path)
        {
            var writer = new StringBuilder();
            var threads = this.Select(e => e.Tid).Distinct().ToArray();
            var types = this.Select(e => e.Type).Distinct().ToArray();

            // Write a header
            writer.Append("Time;");
            foreach (var thread in threads)
                foreach(var type in types)
                    writer.AppendFormat("{0}@{1};",type, thread);
            writer.AppendLine();

            // Write values
            foreach (var timeGroup in this.GroupBy(e => e.Time))
            {
                writer.AppendFormat("{0};", timeGroup.Key);
                foreach (var thread in threads)
                    foreach (var type in types)
                        writer.AppendFormat("{0};", timeGroup.Where(t => t.Tid == thread && t.Type == type).Select(e => e.Value).Sum());
                writer.AppendLine();
            }

            // Write to file.
            File.WriteAllText(path, writer.ToString());
        }
    }

    /// <summary>
    /// Represents a single entry in an output csv file.
    /// </summary>
    public class HarvestOutputEntry
    {
        public HarvestOutputEntry(
            string type,
            string program,
            string user,
            long time,
            double value,
            int tid,
            int pid,
            int cpu,
            int uid)
        {
            this.Type = type;
            this.Program = program;
            this.User = user;
            this.Time = time;
            this.Value = value;
            this.Tid = tid;
            this.Pid = pid;
            this.Cpu = cpu;
            this.Uid = uid;
        }

        public readonly string Type;
        public readonly string Program;
        public readonly string User;
        public readonly long Time;
        public readonly double Value;
        public readonly int Tid;
        public readonly int Pid;
        public readonly int Cpu;
        public readonly int Uid;

        public string ToCsvString()
        {
            return String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}", Time, User, Program, Tid, Pid, Cpu, Uid, Type, Value);
        }

    }

}
