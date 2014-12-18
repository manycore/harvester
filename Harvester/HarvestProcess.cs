using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Harvester
{
    /// <summary>
    /// Represents a program that performs data collection.
    /// </summary>
    public class HarvestProcess
    {
        // Process
        private Process Process;
        private Thread OutputThread;

        /// <summary>
        /// Constructs a new process.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="executable"></param>
        public HarvestProcess(string name, FileInfo executable)
        {
            this.Name = name;
            this.Executable = executable;
        }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the file path of the process.
        /// </summary>
        public FileInfo Executable
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the file path of the process.
        /// </summary>
        public FileInfo Stdout
        {
            get { return new FileInfo(Path.Combine(this.Executable.Directory.FullName, "stdout.txt")); }
        }

        /// <summary>
        /// Gets the file path of the process.
        /// </summary>
        public FileInfo Output
        {
            get { return new FileInfo(Path.Combine(this.Executable.Directory.FullName, "output.csv")); }
        }

        /// <summary>
        /// Gets whether the process is running or not.
        /// </summary>
        public bool IsRunning
        {
            get { return this.Process != null && !this.Process.HasExited; }
        }


        /// <summary>
        /// Starts the process.
        /// </summary>
        /// <param name="arguments">Arguments to the process</param>
        public void Run(string arguments, bool stdRedirect = true)
        {
            this.Process = new Process();
            this.Process.StartInfo = new ProcessStartInfo(this.Executable.FullName, arguments);
            this.Process.StartInfo.WorkingDirectory = this.Executable.Directory.FullName;
            this.Process.StartInfo.UseShellExecute = !stdRedirect;
            this.Process.StartInfo.RedirectStandardOutput = stdRedirect;
            this.Process.StartInfo.RedirectStandardInput = stdRedirect;
            this.Process.EnableRaisingEvents = true;
            this.Process.Start();

            // We need to set it to realtime
            this.Process.PriorityClass = ProcessPriorityClass.RealTime;

            if (stdRedirect)
            {
                this.OutputThread = StartThread(new ThreadStart(WriteStandardOutput), "StandardOutput");

            }
            Console.WriteLine("Running {0}...", this.Executable.Name);
        }


        /// <summary>
        /// Stops the process.
        /// </summary>
        public void Stop()
        {
            if (this.Process == null)
                return;


            if (!this.Process.HasExited)
            {
                //this.Process.Kill();
                this.Process.StandardInput.Close();

                //sned the ctrl-c to the process group (the parent will get it too!)
                /*SENDING_CTRL_C_TO_CHILD = true;
                GenerateConsoleCtrlEvent(ConsoleCtrlEvent.CTRL_C, this.Process.SessionId);
                this.Process.WaitForExit();
                SENDING_CTRL_C_TO_CHILD = false;*/


                //Console.WriteLine("Stopping {0}...", this.Executable.Name);
            }

            if (this.OutputThread != null && this.OutputThread.IsAlive)
                this.OutputThread.Abort();

            this.Process = null;
        }

        /// <summary>
        /// Waits for exit.
        /// </summary>
        public void WaitForExit()
        {
            if(this.Process != null && !this.Process.HasExited)
                this.Process.WaitForExit();
        }


        #region Private Members
        /// <summary>
        /// Thread which outputs standard output from the running executable to the appropriate file.
        /// </summary>
        private void WriteStandardOutput()
        {
            var filename = this.Stdout.FullName;

            if (File.Exists(filename))
                File.Delete(filename);

            using (StreamWriter writer = File.CreateText(filename))
            using (StreamReader reader = this.Process.StandardOutput)
            {
                writer.AutoFlush = true;

                for (; ; )
                {
                    string textLine = reader.ReadLine();

                    if (textLine == null)
                        break;

                    writer.WriteLine(textLine);
                }
            }

            if (File.Exists(filename))
            {
                FileInfo info = new FileInfo(filename);

                // if the error info is empty or just contains eof etc.

                if (info.Length < 4)
                    info.Delete();
            }
        }


        #endregion

        #region Static Members

        /// <summary>
        /// Constructs a harvest process from the binary files.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="exeName"></param>
        /// <param name="binary"></param>
        /// <returns></returns>
        public static HarvestProcess FromBinary(string name, string exeName, byte[] binary)
        {
            var directory = Decompress(name, binary);
            var filename  = new FileInfo(Path.Combine(directory, exeName));
            return new HarvestProcess(name, filename);
        }


        /// <summary>
        /// Decompresses a directory
        /// </summary>
        protected static string Decompress(string name, byte[] binary)
        {
            // Get the target directory, and make sure we haven't done it already.
            var libPath = "bin";
            var target = Path.Combine(libPath, name);
            var zipfile = Path.Combine(libPath, name + ".zip");
            if (Directory.Exists(target))
                return target;

            // Make sure we have a temporary directory
            if (!Directory.Exists(libPath))
                Directory.CreateDirectory(libPath);

            // Make sure we have a temporary for the target
            if (!Directory.Exists(target))
                Directory.CreateDirectory(target);

            // Write the zip file
            File.WriteAllBytes(zipfile, binary);

            // Extract to the temporary directory
            ZipFile.ExtractToDirectory(zipfile, target);

            // Delete the zip file
            File.Delete(zipfile);

            // Returns the directory it's in
            return target;
        }

        /// <summary>Start a thread.</summary>
        /// <param name="startInfo">start information for this thread</param>
        /// <param name="name">name of the thread</param>
        /// <returns>thread object</returns>
        private static Thread StartThread(ThreadStart startInfo, string name)
        {
            Thread t = new Thread(startInfo);
            t.IsBackground = true;
            t.Name = name;
            t.Start();
            return t;
        }

        #endregion
    }
}
