using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harvester.Properties;

namespace Harvester
{
    class Program
    {
        static void Main(string[] args)
        {
            // Start the performance collection
            var type = "analyze";
            if(args.Length > 0)
                type = args[0];

            if (type != "analyze")
            {
                Collect(type);
            }
            else
            {
                Analyze("MatmulIJK", "data/MatmulIJK");
                Analyze("MatmulKJI", "data/MatmulKJI");
                Analyze("MatmulKIJ", "data/MatmulKIJ");

                Analyze("ComputePi", "data/ComputePi");
                Analyze("Mandelbrot", "data/Mandelbrot");
                Analyze("NQueens", "data/NQueens");
                Analyze("RayTracer", "data/RayTracer");
            }

            Console.WriteLine("Analysis: Completed");
            if (type != "analyze")
                Environment.Exit(0);
            
        }

        /// <summary>
        /// Performs the data collection and the subsequent analysis.
        /// </summary>
        static void Collect(string processName)
        {
            var pcm = HarvestProcess.FromBinary("pcm-win", "pcm.exe", Resources.pcm_win);
            var os = HarvestProcess.FromBinary("os-win", "PerfMonitor.exe", Resources.os_win);

            pcm.Run("5");
            os.Run(" -KernelEvents:ContextSwitch,MemoryPageFaults  start");

            Console.WriteLine("Press any key to stop data collection...");
            Console.ReadKey();

            pcm.Stop();
            os.Run("stop");

            Console.WriteLine("Waiting for data collection to stop...");
            os.WaitForExit();
            pcm.WaitForExit();


            // Analyze
            var experiment = new HarvestExperimentWin();
            Console.WriteLine("Copying and merging to the experiment folder...");
            experiment.Merge(processName, pcm, os);

        }

        /// <summary>
        /// Analyzes the data.
        /// </summary>
        /// <param name="processName">The process to analyze.</param>
        /// <param name="folder">The folder containing the data</param>
        static void Analyze(string processName, string folder)
        {
            var experiment = new HarvestExperimentWin(folder);
            experiment.Merge(processName, null, null);
        }


    }
}
