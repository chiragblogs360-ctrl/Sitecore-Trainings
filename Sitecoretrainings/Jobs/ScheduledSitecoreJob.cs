using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using System;
using System.Linq;

namespace Sitecoretrainings.Jobs
{
    /// <summary>
    /// Scheduled Sitecore Job for background processing tasks
    /// This job can be configured in a Sitecore config file to run on a schedule
    /// </summary>
    public class ScheduledSitecoreJob
    {
        /// <summary>
        /// Main execution method that will be called by Sitecore scheduler
        /// </summary>
        public void Execute()
        {
            try
            {
                Log.Info("========== Scheduled Sitecore Job Started ==========", this);

                // Get the master database
                var database = Database.GetDatabase("master");
                if (database == null)
                {
                    Log.Error("Master database not found", this);
                    return;
                }

                // Example: Process content items
                ProcessContentItems(database);

                Log.Info("========== Scheduled Sitecore Job Completed ==========", this);
            }
            catch (Exception ex)
            {
                Log.Error($"Scheduled Sitecore Job failed: {ex.Message}", ex, this);
            }
        }

        /// <summary>
        /// Process content items - example implementation
        /// </summary>
        private void ProcessContentItems(Database database)
        {
            try
            {
                Log.Info("Processing content items...", this);

                var contentRoot = database.GetItem("/sitecore/content");
                if (contentRoot == null)
                {
                    Log.Warn("Content root not found", this);
                    return;
                }

                using (new SecurityDisabler())
                {
                    // Get items updated in the last 1 hour
                    var cutoffTime = DateTime.UtcNow.AddHours(-1);
                    var recentItems = contentRoot.Axes.GetDescendants().Where(item => item.Statistics.Updated > cutoffTime).Take(20).ToList();

                    Log.Info($"Found {recentItems.Count} items updated in the last hour", this);

                    int processedCount = 0;
                    foreach (var item in recentItems)
                    {
                        if (ProcessItem(item))
                        {
                            processedCount++;
                        }
                    }

                    Log.Info($"Successfully processed {processedCount} items", this);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing content items: {ex.Message}", ex, this);
            }
        }

        /// <summary>
        /// Process a single item
        /// </summary>
        private bool ProcessItem(Item item)
        {
            try
            {
                Log.Info($"Processing item: {item.Name} (ID: {item.ID}, Path: {item.Paths.FullPath})", this);

                // Add your custom processing logic here
                // Examples:
                // - Update field values
                // - Sync to external system
                // - Generate reports
                // - Send notifications

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to process item '{item.Name}': {ex.Message}", ex, this);
                return false;
            }
        }
    }
}
