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
                Parallel.Invoke(
                    () => Analyze("Matmul", "accounta", "data/AccountA"),
                    () => Analyze("Matmul", "accountb", "data/AccountB"),
                    () => Analyze("Matmul", "badcachea", "data/BadCacheA"),
                    () => Analyze("ComputePi", "computepi", "data/ComputePi"),
                    () => Analyze("EXCEL", "excel", "data/Excel"),
                    () => Analyze("Mandelbrot", "mandelbrot", "data/Mandelbrot"),
                    () => Analyze("MatmulIJK", "matmulijk", "data/MatmulIJK"),
                    () => Analyze("MatmulKIJ", "matmulkij", "data/MatmulKIJ"),
                    () => Analyze("MatmulKJI", "matmulkji", "data/MatmulKJI"),
                    () => Analyze("Matmul", "mergesortp", "data/MergeSortP"),
                    () => Analyze("Matmul", "mergesorts", "data/MergeSortS"),
                    () => Analyze("node", "nodejs", "data/NodeJS"),
                    () => Analyze("NQueens", "nqueens", "data/NQueens"),
                    () => Analyze("Matmul", "particlep", "data/ParticleP"),
                    () => Analyze("Matmul", "particles", "data/ParticleS"),
                    () => Analyze("Matmul", "pc1x1", "data/PC1x1"),
                    () => Analyze("Matmul", "pc1x10", "data/PC1x10"),
                    () => Analyze("Matmul", "pc1x100", "data/PC1x100"),
                    () => Analyze("Matmul", "pc10x1", "data/PC10x1"),
                    () => Analyze("Matmul", "pc10x10", "data/PC10x10"),
                    () => Analyze("Matmul", "pc10x100", "data/PC10x100"),
                    () => Analyze("Matmul", "pc100x1", "data/PC100x1"),
                    () => Analyze("Matmul", "pc100x10", "data/PC100x10"),
                    () => Analyze("Matmul", "pc100x100", "data/PC100x100"),
                    () => Analyze("Matmul", "phasea", "data/PhaseA"),
                    () => Analyze("Matmul", "phaseb", "data/PhaseB"),
                    () => Analyze("Matmul", "philosophers45", "data/Philosophers45"),
                    () => Analyze("RayTracer", "raytracer", "data/RayTracer"),
                    () => Analyze("Server", "spike", "data/Spike"),
                    () => Analyze("WINWORD", "word", "data/Word")
                );
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
            os.Run(" -KernelEvents:ContextSwitch,MemoryPageFaults /providers:FE2625C1-C10D-452C-B813-A8703EA9D2BA start");

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
            experiment.Merge(processName, processName, pcm, os);

        }

        /// <summary>
        /// Analyzes the data.
        /// </summary>
        /// <param name="processName">The process to analyze.</param>
        /// <param name="folder">The folder containing the data</param>
        static void Analyze(string processName, string name, string folder)
        {
            var experiment = new HarvestExperimentWin(folder);
            experiment.Merge(processName, name, null, null);
        }


    }
}
