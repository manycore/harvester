using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester.Analysis
{
    /// <summary>
    /// Represents an output of the analysis. This is something we can use as a source for
    /// further data processing.
    /// </summary>
    public class EventOutput : List<EventEntry>
    {
        #region Public Members - Add()
        /// <summary>
        /// Adds a single event entry to the output.
        /// </summary>
        /// <param name="type">The type of the event.</param>
        /// <param name="program">The program name.</param>
        /// <param name="user">The user name.</param>
        /// <param name="time">The time of the event.</param>
        /// <param name="value">The measured value of the event.</param>
        /// <param name="tid">The thread number of the event.</param>
        /// <param name="pid">The process number of the event.</param>
        /// <param name="cpu">The processor number of the event.</param>
        /// <param name="uid">The user number of the event.</param>
        public void Add(string type, string program, string user, long time,double value, int tid, int pid, int cpu, int uid)
        {
            this.Add(new EventEntry()
            {
                Type = type,
                Program = program,
                User = user,
                Time = time,
                Value = value,
                Tid = tid,
                Pid = pid,
                Cpu = cpu,
                Uid = uid
            });
        }

        /// <summary>
        /// Adds a single event entry to the output.
        /// </summary>
        /// <param name="type">The type of the event.</param>
        /// <param name="time">The time of the event.</param>
        /// <param name="thread">The specific thread.</param>
        /// <param name="core">The processor number of the event.</param>
        /// <param name="value">The measured value of the event.</param>
        public void Add(string type, DateTime time, EventThread thread, int core, double value)
        {
            this.Add(type, thread.Process, thread.User, time, value, thread.Tid, thread.Pid, core, thread.Uid);
        }

        /// <summary>
        /// Adds a single event entry to the output.
        /// </summary>
        /// <param name="type">The type of the event.</param>
        /// <param name="frame">The frame of the event.</param>
        /// <param name="thread">The specific thread.</param>
        /// <param name="core">The processor number of the event.</param>
        /// <param name="value">The measured value of the event.</param>
        public void Add(string type, EventFrame frame, EventThread thread,  double value)
        {
            this.Add(type, thread.Process, thread.User, frame.Time, value, thread.Tid, thread.Pid, frame.Core, thread.Uid);
        }

        /// <summary>
        /// Adds a single event entry to the output.
        /// </summary>
        /// <param name="type">The type of the event.</param>
        /// <param name="program">The program name.</param>
        /// <param name="user">The user name.</param>
        /// <param name="time">The time of the event.</param>
        /// <param name="value">The measured value of the event.</param>
        /// <param name="tid">The thread number of the event.</param>
        /// <param name="pid">The process number of the event.</param>
        /// <param name="cpu">The processor number of the event.</param>
        /// <param name="uid">The user number of the event.</param>
        public void Add(string type, string program, string user, DateTime time, double value, int tid, int pid, int cpu, int uid)
        {
            this.Add(type, program, user, time.Ticks, value, tid, pid, cpu, uid);
        }

        /// <summary>
        /// Adds a single event entry to the output. This represents a 'system' user.
        /// </summary>
        /// <param name="type">The type of the event.</param>
        /// <param name="time">The time of the event.</param>
        /// <param name="value">The measured value of the event.</param>
        /// <param name="cpu">The processor number of the event.</param>
        public void AddSystem(string type, long time, double value, int cpu)
        {
            this.Add(type, "system", "root", time, value, 0, 0, cpu, 0);
        }
        #endregion

        #region Public Members - Save() & Export()
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
        #endregion
    }


}