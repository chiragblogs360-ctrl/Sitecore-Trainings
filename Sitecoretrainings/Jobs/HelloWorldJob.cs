using Sitecore.Diagnostics;
using System;

namespace Sitecoretrainings.Jobs
{
    /// <summary>
    /// Simple scheduled agent that logs "Hello World from job"
    /// This is a scheduled agent that runs on a configured interval
    /// </summary>
    public class HelloWorldJob
    {
        /// <summary>
        /// Main method that logs Hello World
        /// This method will be called by Sitecore on the configured schedule
        /// </summary>
        public void Run()
        {
            try
            {
                Log.Info("Hello World from job", this);
            }
            catch (Exception ex)
            {
                Log.Error($"HelloWorldJob failed: {ex.Message}", ex, this);
            }
        }
    }
}
