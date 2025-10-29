using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sitecoretrainings.Examples
{
    /// <summary>
    /// SCHEDULED JOB/AGENT EXAMPLES - Sitecore 10.3
    ///
    /// In Sitecore 10.3, SCHEDULED AGENTS are the recommended approach for background tasks.
    /// These are configured in the <scheduling> section of config files and run on a timer.
    ///
    /// Use Case: Recurring maintenance tasks, data synchronization, cleanup operations
    /// </summary>

    #region Basic Scheduled Agent

    /// <summary>
    /// Simple scheduled agent that logs "Hello World"
    /// </summary>
    public class HelloWorldScheduledAgent
    {
        /// <summary>
        /// This method will be called by Sitecore on the configured schedule
        /// </summary>
        public void Run()
        {
            try
            {
                Log.Info("========================================", this);
                Log.Info("Hello World from Scheduled Agent!", this);
                Log.Info($"Current Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", this);
                Log.Info("========================================", this);
            }
            catch (Exception ex)
            {
                Log.Error($"HelloWorld Scheduled Agent failed: {ex.Message}", ex, this);
            }
        }
    }

    #endregion

    #region Batch Item Processor Agent

    /// <summary>
    /// Scheduled agent that processes items in batches
    /// </summary>
    public class BatchItemProcessorAgent
    {
        private int _processedCount = 0;
        private int _failedCount = 0;
        private List<string> _errors = new List<string>();

        /// <summary>
        /// Processes items from content tree
        /// </summary>
        public void Execute()
        {
            try
            {
                Log.Info("Hello World from Scheduled Agent - Starting batch processing", this);

                var database = Database.GetDatabase("master");
                var contentRoot = database.GetItem("/sitecore/content");

                if (contentRoot == null)
                {
                    Log.Error("Content root not found", this);
                    return;
                }

                // Get items to process (example: items updated in last hour)
                var itemsToProcess = GetRecentlyUpdatedItems(contentRoot, TimeSpan.FromHours(1));

                Log.Info($"Found {itemsToProcess.Count} items to process", this);

                // Process items
                foreach (var item in itemsToProcess)
                {
                    ProcessSingleItem(item);
                }

                // Report results
                ReportResults();
            }
            catch (Exception ex)
            {
                Log.Error($"Batch processor agent failed: {ex.Message}", ex, this);
            }
        }

        private List<Item> GetRecentlyUpdatedItems(Item rootItem, TimeSpan timespan)
        {
            var items = new List<Item>();

            try
            {
                using (new SecurityDisabler())
                {
                    var cutoffTime = DateTime.UtcNow.Subtract(timespan);

                    items.AddRange(rootItem.Axes.GetDescendants()
                        .Where(i => i.Statistics.Updated > cutoffTime)
                        .Take(50)); // Limit to 50 items
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting items: {ex.Message}", ex, this);
            }

            return items;
        }

        private void ProcessSingleItem(Item item)
        {
            try
            {
                // Example processing logic
                Log.Info($"Processing item: {item.Name} (ID: {item.ID})", this);

                _processedCount++;
            }
            catch (Exception ex)
            {
                _failedCount++;
                var errorMsg = $"Failed to process '{item.Name}': {ex.Message}";
                _errors.Add(errorMsg);
                Log.Error(errorMsg, ex, this);
            }
        }

        private void ReportResults()
        {
            Log.Info("========== Batch Processing Summary ==========", this);
            Log.Info($"Total Processed: {_processedCount}", this);
            Log.Info($"Total Failed: {_failedCount}", this);

            if (_errors.Any())
            {
                Log.Warn("Errors encountered:", this);
                foreach (var error in _errors)
                {
                    Log.Warn($"  - {error}", this);
                }
            }

            Log.Info("Hello World from Scheduled Agent - Processing completed!", this);
        }
    }

    #endregion

    #region Media Cleanup Agent

    /// <summary>
    /// Scheduled agent that cleans up unused media items
    /// </summary>
    public class MediaCleanupAgent
    {
        public void Execute()
        {
            try
            {
                Log.Info("Hello World from Scheduled Agent - Starting media library cleanup", this);

                var database = Database.GetDatabase("master");
                var mediaLibrary = database.GetItem("/sitecore/media library");

                if (mediaLibrary == null)
                {
                    Log.Warn("Media library not found", this);
                    return;
                }

                var unusedMedia = FindUnusedMediaItems(mediaLibrary);

                Log.Info($"Found {unusedMedia.Count} unused media items", this);

                int deletedCount = 0;
                foreach (var mediaItem in unusedMedia)
                {
                    if (DeleteMediaItem(mediaItem))
                    {
                        deletedCount++;
                    }
                }

                Log.Info($"Media cleanup completed. Deleted {deletedCount} items", this);
            }
            catch (Exception ex)
            {
                Log.Error($"Media cleanup agent failed: {ex.Message}", ex, this);
            }
        }

        private List<Item> FindUnusedMediaItems(Item mediaLibrary)
        {
            var unusedItems = new List<Item>();

            try
            {
                using (new SecurityDisabler())
                {
                    foreach (Item mediaItem in mediaLibrary.Axes.GetDescendants().Take(100)) // Limit for safety
                    {
                        // Check if media is referenced anywhere
                        var referrers = Globals.LinkDatabase.GetReferrers(mediaItem);

                        if (referrers.Length == 0)
                        {
                            // Check age - only consider if older than 30 days
                            var created = mediaItem.Statistics.Created;
                            if ((DateTime.Now - created).TotalDays > 30)
                            {
                                unusedItems.Add(mediaItem);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error finding unused media: {ex.Message}", ex, this);
            }

            return unusedItems;
        }

        private bool DeleteMediaItem(Item item)
        {
            try
            {
                using (new SecurityDisabler())
                {
                    item.Delete();
                    Log.Info($"Deleted media item: {item.Name}", this);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to delete media item '{item.Name}': {ex.Message}", ex, this);
                return false;
            }
        }
    }

    #endregion

    #region System Maintenance Agent

    /// <summary>
    /// Scheduled agent for system maintenance tasks
    /// </summary>
    public class SystemMaintenanceAgent
    {
        public void Run()
        {
            try
            {
                Log.Info("Hello World from Scheduled Agent - System maintenance running", this);

                // Example maintenance tasks
                CleanupOldLogs();
                CheckSystemHealth();
                OptimizeCaches();

                Log.Info("System maintenance completed successfully", this);
            }
            catch (Exception ex)
            {
                Log.Error($"System maintenance agent failed: {ex.Message}", ex, this);
            }
        }

        private void CleanupOldLogs()
        {
            try
            {
                Log.Info("Cleaning up old logs...", this);
                // Add cleanup logic here
            }
            catch (Exception ex)
            {
                Log.Error($"Log cleanup failed: {ex.Message}", ex, this);
            }
        }

        private void CheckSystemHealth()
        {
            try
            {
                Log.Info("Checking system health...", this);

                var database = Database.GetDatabase("master");
                if (database != null)
                {
                    Log.Info("Master database: OK", this);
                }

                var webDatabase = Database.GetDatabase("web");
                if (webDatabase != null)
                {
                    Log.Info("Web database: OK", this);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Health check failed: {ex.Message}", ex, this);
            }
        }

        private void OptimizeCaches()
        {
            try
            {
                Log.Info("Optimizing caches...", this);
                // Add cache optimization logic here
            }
            catch (Exception ex)
            {
                Log.Error($"Cache optimization failed: {ex.Message}", ex, this);
            }
        }
    }

    #endregion

    #region Content Sync Agent

    /// <summary>
    /// Scheduled agent for syncing content between environments
    /// </summary>
    public class ContentSyncAgent
    {
        public void Execute()
        {
            try
            {
                Log.Info("Hello World from Scheduled Agent - Starting content sync", this);

                var database = Database.GetDatabase("master");
                var syncRoot = database.GetItem("/sitecore/content");

                if (syncRoot == null)
                {
                    Log.Warn("Sync root not found", this);
                    return;
                }

                var itemsToSync = GetItemsToSync(syncRoot);

                Log.Info($"Syncing {itemsToSync.Count} items", this);

                int syncedCount = 0;
                foreach (var item in itemsToSync)
                {
                    if (SyncItem(item))
                    {
                        syncedCount++;
                    }
                }

                Log.Info($"Content sync completed. Synced {syncedCount} items", this);
            }
            catch (Exception ex)
            {
                Log.Error($"Content sync agent failed: {ex.Message}", ex, this);
            }
        }

        private List<Item> GetItemsToSync(Item rootItem)
        {
            var items = new List<Item>();

            try
            {
                using (new SecurityDisabler())
                {
                    // Get items updated in last 10 minutes
                    var cutoffTime = DateTime.UtcNow.AddMinutes(-10);

                    items.AddRange(rootItem.Axes.GetDescendants()
                        .Where(i => i.Statistics.Updated > cutoffTime)
                        .Take(20)); // Limit to 20 items per run
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting items to sync: {ex.Message}", ex, this);
            }

            return items;
        }

        private bool SyncItem(Item item)
        {
            try
            {
                Log.Info($"Syncing item: {item.Name} (ID: {item.ID})", this);

                // Add actual sync logic here (e.g., API call, file export, etc.)

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to sync item '{item.Name}': {ex.Message}", ex, this);
                return false;
            }
        }
    }

    #endregion
}

/*

HOW TO CONFIGURE SCHEDULED AGENTS (Sitecore 10.3):

CONFIG FILE: App_Config/Include/YourModule/ScheduledJobs.config

<?xml version="1.0" encoding="utf-8" ?>
<configuration xmlns:patch="http://www.sitecore.net/xmlconfig/">
  <sitecore>
    <scheduling>

      <!-- Hello World Agent - Runs every 5 minutes -->
      <agent type="Sitecoretrainings.Examples.HelloWorldScheduledAgent, Sitecoretrainings"
             method="Run"
             interval="00:05:00">
      </agent>

      <!-- Batch Processor - Runs every 1 hour -->
      <agent type="Sitecoretrainings.Examples.BatchItemProcessorAgent, Sitecoretrainings"
             method="Execute"
             interval="01:00:00">
      </agent>

      <!-- Media Cleanup - Runs daily at midnight (24 hours) -->
      <agent type="Sitecoretrainings.Examples.MediaCleanupAgent, Sitecoretrainings"
             method="Execute"
             interval="24:00:00">
      </agent>

      <!-- System Maintenance - Runs every 30 minutes -->
      <agent type="Sitecoretrainings.Examples.SystemMaintenanceAgent, Sitecoretrainings"
             method="Run"
             interval="00:30:00">
      </agent>

      <!-- Content Sync - Runs every 10 minutes -->
      <agent type="Sitecoretrainings.Examples.ContentSyncAgent, Sitecoretrainings"
             method="Execute"
             interval="00:10:00">
      </agent>

    </scheduling>
  </sitecore>
</configuration>

=====================================================================

INTERVAL FORMAT: HH:MM:SS
- 00:01:00 = Every 1 minute
- 00:05:00 = Every 5 minutes
- 00:30:00 = Every 30 minutes
- 01:00:00 = Every 1 hour
- 24:00:00 = Every 24 hours (daily)

=====================================================================

KEY POINTS FOR SITECORE 10.3:
- Use SCHEDULED AGENTS instead of programmatic Jobs
- Agents run automatically on configured intervals
- No need for JobManager or manual job starting
- Simple, reliable, and maintenance-free
- Perfect for recurring background tasks
- Logs appear in standard Sitecore logs

BEST PRACTICES:
 ✓ Keep agent execution time short (< 5 minutes)
 ✓ Use SecurityDisabler when accessing items programmatically
 ✓ Limit the number of items processed per run
 ✓ Include comprehensive error handling and logging
 ✓ Set appropriate intervals based on task requirements
 ✓ Test agents thoroughly before deployment
 ✓ Monitor log files for agent execution

COMMON USE CASES:
- Data synchronization with external systems
- Media library cleanup
- Content publishing automation
- Cache management
- Report generation
- System health checks
- Email notifications
- Database maintenance

*/
