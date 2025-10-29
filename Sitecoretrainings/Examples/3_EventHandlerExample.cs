using Sitecore;
using Sitecore.Caching;
using Sitecore.ContentSearch;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Publishing;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sitecore.Configuration;
using Sitecore.Links;
using System.Net.Http;
using Sitecore.ContentSearch.Maintenance;

namespace Sitecoretrainings.Examples
{
    /// <summary>
    /// EVENT HANDLER EXAMPLE - Enhanced Version
    ///
    /// An Event Handler REACTS to something that has happened in Sitecore.
    /// This example demonstrates real-world event handling with cache management,
    /// search indexing, notifications, and external integrations.
    ///
    /// Use Case: React to Sitecore actions to trigger secondary workflows
    /// </summary>

    #region Item Saved Handler

    /// <summary>
    /// Handles item:saved event with comprehensive post-save operations
    /// </summary>
    public class ItemSavedEventHandler
    {
        public void OnItemSaved(object sender, EventArgs args)
        {
            var eventArgs = args as SitecoreEventArgs;
            if (eventArgs == null) return;

            var item = Event.ExtractParameter<Item>(eventArgs, 0);
            if (item == null) return;

            // Skip if this is a system item or in the shell site
            if (item.Paths.IsContentItem == false || Context.Site?.Name == "shell")
            {
                return;
            }

            Log.Info($"Hello World from Event Handler - Item Saved: {item.Name} (ID: {item.ID}, Path: {item.Paths.FullPath})", this);

            try
            {
                // 1. Clear relevant caches
                ClearItemCache(item);

                // 2. Update search index
                UpdateSearchIndex(item);

                // 3. Invalidate parent cache if needed
                InvalidateParentCache(item);

                // 4. Track changes for audit
                TrackItemChanges(item, eventArgs);

                // 5. Check if this should trigger related item updates
                UpdateRelatedItems(item);

                // 6. Send webhook notification (if configured)
                SendWebhookNotification("item.saved", item);

                Log.Info($"Post-save operations completed for '{item.Name}'", this);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in OnItemSaved for '{item.Name}': {ex.Message}", ex, this);
            }
        }

        private void ClearItemCache(Item item)
        {

            var siteContext = Sitecore.Context.Site;

            if (siteContext != null)
            {
                var htmlCache = CacheManager.GetHtmlCache(siteContext);
                htmlCache?.Clear();
                Log.Info($"HTML cache cleared for site '{siteContext.Name}' and item '{item.Name}'", this);
            }

            // Clear site-specific cache
            var siteCache = CacheManager.GetNamedInstance("websites", StringUtil.ParseSizeString("100MB"), true);
            siteCache?.Clear();
        }

        private void UpdateSearchIndex(Item item)
        {
            try
            {
                // Get all configured search indexes
                var indexes = ContentSearchManager.Indexes;

                foreach (var index in indexes)
                {
                    // Check if this item should be in this index
                    if (index.Crawlers.Any(c => c.IsExcludedFromIndex((IIndexable)item) == false))
                    {
                        // Refresh the item in the index
                        index.Refresh((IIndexable)item);
                        Log.Info($"Search index '{index.Name}' updated for item '{item.Name}'", this);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to update search index: {ex.Message}", this);
            }
        }

        private void InvalidateParentCache(Item item)
        {
            if (item?.Parent == null)
                return;

            var parent = item.Parent;

            // Clear parent item cache properly
            parent.Database.Caches.ItemCache.RemoveItem(parent.ID);

            Log.Info($"Parent cache cleared for '{parent.Name}'", this);
        }


        private void TrackItemChanges(Item item, SitecoreEventArgs eventArgs)
        {
            var changes = Event.ExtractParameter<ItemChanges>(eventArgs, 1);
            if (changes != null)
            {
                Log.Info($"Item changed fields:", this);
                foreach (FieldChange fieldChange in changes.FieldChanges)
                {
                    Log.Info($"  - Field: {fieldChange.FieldID}, " + $"Original: '{fieldChange.OriginalValue}', " + $"New: '{fieldChange.Value}'", this);
                }
            }
        }

        private void UpdateRelatedItems(Item item)
        {
            // Example: Update "last modified" on parent or related items
            var relatedItemsField = item.Fields["Related Items"];
            if (relatedItemsField != null && !string.IsNullOrEmpty(relatedItemsField.Value))
            {
                using (new SecurityDisabler())
                {
                    var relatedIds = relatedItemsField.Value.Split('|');
                    foreach (var id in relatedIds)
                    {
                        var relatedItem = item.Database.GetItem(id);
                        if (relatedItem != null)
                        {
                            Log.Info($"Related item found: {relatedItem.Name}", this);
                            // Could update related item here if needed
                        }
                    }
                }
            }
        }

        private void SendWebhookNotification(string eventType, Item item)
        {
            // Async fire-and-forget webhook
            Task.Run(async () =>
            {
                try
                {
                    var webhookUrl = Sitecore.Configuration.Settings.GetSetting("Webhook.Url");
                    if (string.IsNullOrEmpty(webhookUrl)) return;

                    using (var client = new HttpClient())
                    {
                        var payload = new
                        {
                            eventType = eventType,
                            itemId = item.ID.ToString(),
                            itemName = item.Name,
                            itemPath = item.Paths.FullPath,
                            timestamp = DateTime.UtcNow
                        };

                        // In real implementation, serialize payload and send
                        Log.Info($"Webhook notification would be sent for '{item.Name}'", this);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Webhook error: {ex.Message}", ex, this);
                }
            });
        }
    }

    #endregion

    #region Publish End Handler

    /// <summary>
    /// Handles publish:end event with post-publish operations
    /// </summary>
    public class PublishEndEventHandler
    {
        public void OnPublishEnd(object sender, EventArgs args)
        {
            Log.Info("Hello World from Event Handler - Publish Operation Completed", this);

            var eventArgs = args as SitecoreEventArgs;
            if (eventArgs == null) return;

            try
            {
                // Extract publish context
                var publisher = Event.ExtractParameter<Publisher>(eventArgs, 0);

                if (publisher != null)
                {
                    Log.Info($"Publish Summary:", this);
                    Log.Info($"  - Source DB: {publisher.Options.SourceDatabase.Name}", this);
                    Log.Info($"  - Target DB: {publisher.Options.TargetDatabase.Name}", this);
                    Log.Info($"  - Publish Mode: {publisher.Options.Mode}", this);
                    Log.Info($"  - Language: {publisher.Options.Language.Name}", this);
                }

                // 1. Clear all relevant caches on publish
                ClearPublishCaches();

                // 2. Warm up critical pages
                WarmUpCache();

                // 3. Rebuild search indexes
                RebuildSearchIndexes();

                // 4. Send publish notification
                SendPublishNotification(publisher);

                // 5. Log metrics
                LogPublishMetrics(publisher);

                Log.Info("Post-publish operations completed successfully", this);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in OnPublishEnd: {ex.Message}", ex, this);
            }
        }

        private void ClearPublishCaches()
        {
            // Clear all HTML caches
            foreach (var site in Sitecore.Configuration.Factory.GetSiteInfoList())
            {
                var siteContext = Sitecore.Configuration.Factory.GetSite(site.Name);
                if (siteContext != null)
                {
                    var htmlCache = CacheManager.GetHtmlCache(siteContext);
                    htmlCache?.Clear();
                    Log.Info($"HTML cache cleared for site: {site.Name}", this);
                }
            }

            // Clear data cache
            CacheManager.ClearAllCaches();
            Log.Info("All caches cleared after publish", this);
        }

        private void WarmUpCache()
        {
            // In real implementation, make HTTP requests to key pages
            Log.Info("Cache warm-up initiated for critical pages", this);

            var criticalPaths = new[] { "/", "/home", "/products", "/about" };
            foreach (var path in criticalPaths)
            {
                Log.Info($"  - Warming up: {path}", this);
                // Make HTTP request to warm cache
            }
        }

        private void RebuildSearchIndexes()
        {
            try
            {
                var indexes = ContentSearchManager.Indexes;
                foreach (var index in indexes)
                {
                    if (index.Name.Contains("web"))
                    {
                        // Rebuild the index asynchronously
                        IndexCustodian.RefreshTree((IIndexable)index);
                        Log.Info($"Search index '{index.Name}' refresh initiated", this);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to rebuild indexes: {ex.Message}", this);
            }
        }

        private void SendPublishNotification(Publisher publisher)
        {
            // Send email or notification to content authors
            Log.Info("Publish notification sent to content authors", this);

            // In real implementation, use Sitecore.MainUtil.SendMail() or external service
        }

        private void LogPublishMetrics(Publisher publisher)
        {
            // Log to analytics or monitoring system
            Log.Info($"Publish metrics logged for reporting", this);
        }
    }

    #endregion

    #region Item Deleted Handler

    /// <summary>
    /// Handles item:deleted event with cleanup operations
    /// </summary>
    public class ItemDeletedEventHandler
    {
        public void OnItemDeleted(object sender, EventArgs args)
        {
            var eventArgs = args as SitecoreEventArgs;
            if (eventArgs == null) return;

            var item = Event.ExtractParameter<Item>(eventArgs, 0);
            if (item == null) return;

            Log.Info($"Hello World from Event Handler - Item Deleted: {item.Name} (ID: {item.ID})", this);

            try
            {
                // 1. Log deletion for audit trail
                LogDeletion(item);

                // 2. Remove from search indexes
                RemoveFromSearchIndexes(item);

                // 3. Clean up related references
                CleanupReferences(item);

                // 4. Clear cache entries
                ClearRelatedCaches(item);

                // 5. Notify external systems
                NotifyExternalSystems("item.deleted", item);

                Log.Info($"Cleanup operations completed for deleted item '{item.Name}'", this);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in OnItemDeleted: {ex.Message}", ex, this);
            }
        }

        private void LogDeletion(Item item)
        {
            // Create audit log entry
            Log.Audit($"Item deleted: {item.Name} (ID: {item.ID}, Path: {item.Paths.FullPath}) by user: {Context.User.Name}", this);
        }

        private void RemoveFromSearchIndexes(Item item)
        {
            try
            {
                var indexes = ContentSearchManager.Indexes;
                foreach (var index in indexes)
                {
                    index.Refresh((IIndexable)item);
                    Log.Info($"Removed item from index '{index.Name}'", this);
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to remove from indexes: {ex.Message}", this);
            }
        }

        private void CleanupReferences(Item item)
        {
            // Find and update items that reference this deleted item
            Log.Info($"Checking for references to deleted item '{item.Name}'", this);

            // In real implementation, use LinkDatabase to find references
            var links = Globals.LinkDatabase.GetReferrers(item);
            if (links.Any())
            {
                Log.Warn($"Found {links.Length} references to deleted item '{item.Name}'", this);
                // Could update or notify about broken links
            }
        }



        private void ClearRelatedCaches(Item item)
        {
            if (item == null)
            {
                Log.Warn("ClearRelatedCaches called with null item", this);
                return;
            }

            // 1️⃣ Clear the item cache
            item.Database.Caches.ItemCache.RemoveItem(item.ID);

            // 2️⃣ Optionally clear the parent item’s cache
            if (item.Parent != null)
            {
                item.Parent.Database.Caches.ItemCache.RemoveItem(item.Parent.ID);
            }

            // 3️⃣ Optionally clear HTML cache (for presentation)
            var htmlCache = CacheManager.GetHtmlCache(Sitecore.Context.Site);
            if (htmlCache != null)
            {
                htmlCache.RemoveKeysContaining(item.ID.ToString());
                if (item.Parent != null)
                    htmlCache.RemoveKeysContaining(item.Parent.ID.ToString());
            }

            Log.Info($"Cleared cache for '{item.Paths.FullPath}' and its parent (if any)", this);
        }


        private void NotifyExternalSystems(string eventType, Item item)
        {
            Log.Info($"External systems notified of item deletion: {item.Name}", this);
            // Call external APIs or webhooks
        }
    }

    #endregion

    #region Custom Event Example

    /// <summary>
    /// Example of creating and raising custom events
    /// </summary>
    public class CustomEventArgs : EventArgs
    {
        public string Action { get; set; }
        public Item Item { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    public class CustomEventRaiser
    {
        public static void RaiseCustomEvent(string action, Item item)
        {
            var eventArgs = new CustomEventArgs
            {
                Action = action,
                Item = item
            };

            eventArgs.Metadata["Timestamp"] = DateTime.UtcNow.ToString("o");
            eventArgs.Metadata["User"] = Context.User?.Name ?? "Unknown";

            // Raise custom event
            Event.RaiseEvent("mymodule:customaction", new object[] { eventArgs });

            Log.Info($"Custom event raised: {action} for item {item?.Name}", typeof(CustomEventRaiser));
        }
    }

    public class CustomEventSubscriber
    {
        public void OnCustomAction(object sender, EventArgs args)
        {
            Log.Info("Hello World from Event Handler - Custom Event Triggered", this);

            var eventArgs = Event.ExtractParameter<CustomEventArgs>(args as SitecoreEventArgs, 0);
            if (eventArgs != null)
            {
                Log.Info($"Custom action received: {eventArgs.Action}", this);
                Log.Info($"Item: {eventArgs.Item?.Name}", this);
                Log.Info($"Metadata: {string.Join(", ", eventArgs.Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"))}", this);
            }
        }
    }

    #endregion

    #region User Login Handler

    /// <summary>
    /// Handles user:loggedin event
    /// </summary>
    public class UserLoginEventHandler
    {
        public void OnUserLoggedIn(object sender, EventArgs args)
        {
            var username = Event.ExtractParameter<string>(args as SitecoreEventArgs, 0);

            Log.Info($"Hello World from Event Handler - User Logged In: {username}", this);

            try
            {
                // Track login for security audit
                Log.Audit($"User login: {username} from IP: {Sitecore.Web.WebUtil.GetHostIPAddress()}", this);

                // Update last login time
                UpdateLastLoginTime(username);

                // Load user preferences or personalization data
                LoadUserPreferences(username);

                // Send analytics event
                TrackLoginEvent(username);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in OnUserLoggedIn: {ex.Message}", ex, this);
            }
        }

        private void UpdateLastLoginTime(string username)
        {
            Log.Info($"Last login time updated for user: {username}", this);
            // Update user profile in database
        }

        private void LoadUserPreferences(string username)
        {
            Log.Info($"Loading preferences for user: {username}", this);
            // Load from profile or database
        }

        private void TrackLoginEvent(string username)
        {
            Log.Info($"Login event tracked for analytics: {username}", this);
            // Send to analytics platform
        }
    }

    #endregion
}

/*
 * CONFIG FILE: App_Config/Include/YourModule/EventHandlerExample.config
 *
 * <?xml version="1.0" encoding="utf-8" ?>
 * <configuration xmlns:patch="http://www.sitecore.net/xmlconfig/">
 *   <sitecore>
 *     <events>
 *       <!-- Item saved event -->
 *       <event name="item:saved">
 *         <handler type="Sitecoretrainings.Examples.ItemSavedEventHandler, Sitecoretrainings" method="OnItemSaved"/>
 *       </event>
 *
 *       <!-- Publish end event -->
 *       <event name="publish:end">
 *         <handler type="Sitecoretrainings.Examples.PublishEndEventHandler, Sitecoretrainings" method="OnPublishEnd"/>
 *       </event>
 *
 *       <!-- Item deleted event -->
 *       <event name="item:deleted">
 *         <handler type="Sitecoretrainings.Examples.ItemDeletedEventHandler, Sitecoretrainings" method="OnItemDeleted"/>
 *       </event>
 *
 *       <!-- User login event -->
 *       <event name="user:loggedin">
 *         <handler type="Sitecoretrainings.Examples.UserLoginEventHandler, Sitecoretrainings" method="OnUserLoggedIn"/>
 *       </event>
 *
 *       <!-- Custom event -->
 *       <event name="mymodule:customaction">
 *         <handler type="Sitecoretrainings.Examples.CustomEventSubscriber, Sitecoretrainings" method="OnCustomAction"/>
 *       </event>
 *     </events>
 *   </sitecore>
 * </configuration>
 *
 * KEY POINTS:
 * - Event handlers are REACTIVE (respond to things that happen)
 * - Multiple handlers can subscribe to the same event
 * - Events fire AFTER the action (item:saved) or BEFORE (item:saving)
 * - Use Event.RaiseEvent() to create custom events
 * - Great for loose coupling and separation of concerns
 * - Keep handlers fast - use jobs for time-consuming work
 * - Always include error handling
 */
