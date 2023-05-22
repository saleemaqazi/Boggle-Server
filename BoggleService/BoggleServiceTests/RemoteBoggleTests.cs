using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Dynamic;
using static System.Net.HttpStatusCode;
using static System.Net.Http.HttpMethod;
using System.Diagnostics;

namespace BoggleTests
{
    /// <summary>
    /// Provides a way to start and stop the IIS web server from within the test
    /// cases.  If something prevents the test cases from stopping the web server,
    /// subsequent tests may not work properly until the stray process is killed
    /// manually.
    /// 
    /// Starting the service this way allows code coverage statistics for the service
    /// to be collected.
    /// </summary>
    public static class IISAgent
    {
        // Reference to the running process
        private static Process process = null;

        // Full path to IIS_EXPRESS executable
        private const string IIS_EXECUTABLE = @"C:\Program Files (x86)\IIS Express\iisexpress.exe";

        // Command line arguments to IIS_EXPRESS
        private const string ARGUMENTS = @"/site:""BoggleService"" /apppool:""Clr4IntegratedAppPool"" /config:""..\..\..\.vs\config\applicationhost.config""";
       
        /// <summary>
        /// Starts IIS
        /// </summary>
        public static void Start()
        {
            if (process == null)
            {               
                ProcessStartInfo info = new ProcessStartInfo(IIS_EXECUTABLE, ARGUMENTS);
                info.WindowStyle = ProcessWindowStyle.Minimized;
                info.UseShellExecute = false;
                process = Process.Start(info);
            }
        }

        /// <summary>
        ///  Stops IIS
        /// </summary>
        public static void Stop()
        {
            if (process != null)
            {
                process.Kill();
            }
        }
    }

    [TestClass]
    public class ToDoTests
    {
        /// <summary>
        /// This is automatically run prior to all the tests to start the server.
        /// </summary>
        // Remove the comment before the annotation if you want to make it work
        // [ClassInitialize]
        public static void StartIIS(TestContext testContext)
        {
            IISAgent.Start();
        }

        /// <summary>
        /// This is automatically run when all tests have completed to stop the server
        /// </summary>
        // Remove the comment before the annotation if you want to make it work
        // [ClassCleanup]
        public static void StopIIS()
        {
            IISAgent.Stop();
        }
    }
}

