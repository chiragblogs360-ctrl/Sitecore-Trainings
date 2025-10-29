using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sitecoretrainings.Jobs
{
    /// <summary>
    /// Scheduled agent that logs recently published items
    /// Runs on a schedule to check for and log published items
    /// </summary>
    public class PublishLoggerJob
    {
        private int _loggedCount = 0;
        private List<string> _publishedItems = new List<string>();

        /// <summary>
        /// Logs recently published items (items recently updated in master database)
        /// This method will be called by Sitecore on the configured schedule
        /// </summary>
        public void Run()
        {
            try
            {
                Log.Info("========== Publish Logger Started ==========", this);

                LogRecentlyUpdatedItems();

                ReportResults();
            }
            catch (Exception ex)
            {
                Log.Error($"Publish Logger Job failed: {ex.Message}", ex, this);
            }
        }

        /// <summary>
        /// Logs items that were recently updated (likely published)
        /// </summary>
        private void LogRecentlyUpdatedItems()
        {
            try
            {
                var masterDb = Database.GetDatabase("master");
                var webDb = Database.GetDatabase("web");

                if (masterDb == null || webDb == null)
                {
                    Log.Warn("Cannot access master or web database", this);
                    return;
                }

                Log.Info("Checking for recently published items", this);

                // Get items updated in the last 10 minutes
                var recentlyUpdatedItems = GetRecentlyUpdatedItems(masterDb, TimeSpan.FromMinutes(10));

                Log.Info($"Found {recentlyUpdatedItems.Count} recently updated items", this);

                foreach (var item in recentlyUpdatedItems)
                {
                    LogPublishedItem(item, webDb.Name);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error logging published items: {ex.Message}", ex, this);
            }
        }

        /// <summary>
        /// Gets items that were recently updated
        /// </summary>
        private List<Item> GetRecentlyUpdatedItems(Database database, TimeSpan timespan)
        {
            var items = new List<Item>();

            try
            {
                var contentRoot = database.GetItem("/sitecore/content");
                if (contentRoot != null)
                {
                    using (new SecurityDisabler())
                    {
                        var cutoffTime = DateTime.UtcNow.Subtract(timespan);

                        items.AddRange(contentRoot.Axes.GetDescendants()
                            .Where(i => i.Statistics.Updated > cutoffTime)
                            .Take(50)); // Limit to 50 items for safety
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting recently updated items: {ex.Message}", ex, this);
            }

            return items;
        }

        /// <summary>
        /// Logs information about a single published item
        /// </summary>
        private void LogPublishedItem(Item item, string targetDatabase)
        {
            try
            {
                var logMessage = new StringBuilder();
                logMessage.AppendLine("----------------------------------------");
                logMessage.AppendLine($"Published Item #{_loggedCount + 1}:");
                logMessage.AppendLine($"  Name: {item.Name}");
                logMessage.AppendLine($"  ID: {item.ID}");
                logMessage.AppendLine($"  Path: {item.Paths.FullPath}");
                logMessage.AppendLine($"  Template: {item.TemplateName}");
                logMessage.AppendLine($"  Target Database: {targetDatabase}");
                logMessage.AppendLine($"  Last Updated: {item.Statistics.Updated:yyyy-MM-dd HH:mm:ss}");
                logMessage.AppendLine("----------------------------------------");

                Log.Info(logMessage.ToString(), this);

                _publishedItems.Add($"{item.Name} (ID: {item.ID}, Path: {item.Paths.FullPath})");
                _loggedCount++;
            }
            catch (Exception ex)
            {
                Log.Error($"Error logging item '{item?.Name}': {ex.Message}", ex, this);
            }
        }

        /// <summary>
        /// Reports final summary of logged items
        /// </summary>
        private void ReportResults()
        {
            Log.Info("========== Publish Logger Summary ==========", this);
            Log.Info($"Total Items Logged: {_loggedCount}", this);

            if (_publishedItems.Any())
            {
                Log.Info("Published Items List:", this);
                foreach (var item in _publishedItems)
                {
                    Log.Info($"  - {item}", this);
                }
            }
            else
            {
                Log.Info("No recently published items found", this);
            }

            Log.Info("========== Publish Logger Completed ==========", this);
        }
    }
}
