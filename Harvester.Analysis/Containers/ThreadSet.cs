using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvester.Analysis
{
    public sealed class ThreadSet
    {
        private readonly Dictionary<int, double[]> Map 
            = new Dictionary<int, double[]>();

        /// <summary>
        /// Gets the grand total.
        /// </summary>
        public double Total
        {
            get { return Map.Sum(t => t.Value.Sum()); }
        }

        public void Add(int thread, ThreadState state, double elapsed)
        {
            if (!this.Map.ContainsKey(thread))
                this.Map.Add(thread, new double[8]);

            this.Map[thread][(int)state] += elapsed;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Thread  Init    Ready   Run     StandBy Term    Wait    Trans   Unkn");
            foreach(var thread in this.Map.Keys)
            {
                sb.Append(thread.ToString().PadRight(8, ' '));
                for (int i = 0; i < 8; ++i)
                {
                    sb.Append(this.Map[thread][i].ToString().PadRight(8, ' '));
                }
                sb.AppendLine();
            }

            sb.AppendLine("Grand Total: " + Total);

            return sb.ToString();
        }
    }
}
