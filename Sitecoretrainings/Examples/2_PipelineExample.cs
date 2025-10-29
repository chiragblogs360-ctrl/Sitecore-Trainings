using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sitecoretrainings.Examples
{
    /// <summary>
    /// PIPELINE EXAMPLE - Enhanced Version
    ///
    /// A Pipeline is a complete workflow made up of multiple processors.
    /// This example demonstrates a real-world content publishing workflow.
    ///
    /// Use Case: Multi-step content approval and publishing process
    /// </summary>

    #region Pipeline Arguments

    /// <summary>
    /// Pipeline arguments that hold all data shared between processors
    /// </summary>
    public class ContentPublishingPipelineArgs : PipelineArgs
    {
        public Item SourceItem { get; set; }
        public Database TargetDatabase { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public List<Item> ProcessedItems { get; set; } = new List<Item>();
        public bool IsValid { get; set; }
        public bool RequiresApproval { get; set; }
        public string ApprovedBy { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        public int TotalItemsProcessed { get; set; }
    }

    #endregion

    #region Step 1: Validation Processor

    /// <summary>
    /// Validates content before publishing
    /// Checks required fields, template rules, and naming conventions
    /// </summary>
    public class ValidateContentProcessor
    {
        public void Process(ContentPublishingPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, nameof(args));

            Log.Info($"Hello World from Pipeline - Step 1: Validating item '{args.SourceItem?.Name}'", this);

            if (args.SourceItem == null)
            {
                args.ValidationErrors.Add("Source item is null");
                args.IsValid = false;
                args.AbortPipeline();
                return;
            }

            try
            {
                // Check if item has required fields
                ValidateRequiredFields(args);

                // Check if item name follows conventions
                ValidateItemName(args);

                // Check if item has valid workflow state
                ValidateWorkflowState(args);

                // Check if related items exist
                ValidateRelatedItems(args);

                // Set validation result
                args.IsValid = args.ValidationErrors.Count == 0;

                if (!args.IsValid)
                {
                    Log.Warn($"Validation failed for '{args.SourceItem.Name}': {string.Join(", ", args.ValidationErrors)}", this);
                    args.AbortPipeline();
                }
                else
                {
                    Log.Info($"Validation passed for '{args.SourceItem.Name}'", this);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Validation error: {ex.Message}", ex, this);
                args.ValidationErrors.Add($"Validation exception: {ex.Message}");
                args.AbortPipeline();
            }
        }

        private void ValidateRequiredFields(ContentPublishingPipelineArgs args)
        {
            // Example: Check for required fields based on template
            var titleField = args.SourceItem.Fields["Title"];
            var descriptionField = args.SourceItem.Fields["Description"];

            if (titleField != null && string.IsNullOrWhiteSpace(titleField.Value))
            {
                args.ValidationErrors.Add("Title field is required");
            }

            if (descriptionField != null && string.IsNullOrWhiteSpace(descriptionField.Value))
            {
                args.ValidationErrors.Add("Description field is required");
            }
        }

        private void ValidateItemName(ContentPublishingPipelineArgs args)
        {
            // Check if item name contains invalid characters
            if (Regex.IsMatch(args.SourceItem.Name, @"[^a-zA-Z0-9\-_]"))
            {
                args.ValidationErrors.Add($"Item name '{args.SourceItem.Name}' contains invalid characters");
            }

            // Check length
            if (args.SourceItem.Name.Length > 50)
            {
                args.ValidationErrors.Add($"Item name too long (max 50 characters)");
            }
        }

        private void ValidateWorkflowState(ContentPublishingPipelineArgs args)
        {
            var workflowField = args.SourceItem.Fields["__Workflow"];
            var workflowStateField = args.SourceItem.Fields["__Workflow state"];

            if (workflowField != null && !string.IsNullOrEmpty(workflowField.Value))
            {
                // Item is in workflow, check if approved
                args.RequiresApproval = true;

                if (workflowStateField == null || string.IsNullOrEmpty(workflowStateField.Value))
                {
                    args.ValidationErrors.Add("Item in workflow but has no workflow state");
                }
            }
        }

        private void ValidateRelatedItems(ContentPublishingPipelineArgs args)
        {
            // Example: Validate linked items exist
            var linksField = args.SourceItem.Fields["Related Items"];
            if (linksField != null && !string.IsNullOrEmpty(linksField.Value))
            {
                // Parse multilist or similar and validate
                Log.Info($"Validating related items for '{args.SourceItem.Name}'", this);
            }
        }
    }

    #endregion

    #region Step 2: Approval Processor

    /// <summary>
    /// Checks approval status and handles approval logic
    /// </summary>
    public class CheckApprovalProcessor
    {
        public void Process(ContentPublishingPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, nameof(args));

            Log.Info($"Hello World from Pipeline - Step 2: Checking approval for '{args.SourceItem.Name}'", this);

            if (!args.RequiresApproval)
            {
                Log.Info($"Item '{args.SourceItem.Name}' does not require approval, continuing...", this);
                return;
            }

            try
            {
                // Check if user has approval rights
                var currentUser = Context.User;
                if (currentUser != null && IsApprover(currentUser.Name))
                {
                    args.ApprovedBy = currentUser.Name;
                    Log.Info($"Item approved by '{args.ApprovedBy}'", this);

                    // Update approval metadata
                    args.Metadata["ApprovedBy"] = args.ApprovedBy;
                    args.Metadata["ApprovalDate"] = DateTime.UtcNow.ToString("o");
                }
                else
                {
                    Log.Warn($"User '{currentUser?.Name}' does not have approval rights", this);
                    args.ValidationErrors.Add("Content requires approval but user lacks approval rights");
                    args.AbortPipeline();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Approval check error: {ex.Message}", ex, this);
                args.AbortPipeline();
            }
        }

        private bool IsApprover(string username)
        {
            // In real implementation, check if user is in "Content Approvers" role
            return !string.IsNullOrEmpty(username);
        }
    }

    #endregion

    #region Step 3: Processing Processor

    /// <summary>
    /// Processes and transforms content before publishing
    /// </summary>
    public class ProcessContentProcessor
    {
        public void Process(ContentPublishingPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, nameof(args));

            Log.Info($"Hello World from Pipeline - Step 3: Processing content for '{args.SourceItem.Name}'", this);

            try
            {
                // Process the main item
                ProcessSingleItem(args, args.SourceItem);

                // Process child items if needed
                ProcessChildItems(args);

                Log.Info($"Content processing completed. {args.TotalItemsProcessed} items processed", this);
            }
            catch (Exception ex)
            {
                Log.Error($"Content processing error: {ex.Message}", ex, this);
                args.ValidationErrors.Add($"Processing failed: {ex.Message}");
                args.AbortPipeline();
            }
        }

        private void ProcessSingleItem(ContentPublishingPipelineArgs args, Item item)
        {
            using (new SecurityDisabler())
            {
                item.Editing.BeginEdit();
                try
                {
                    // Example: Update processing metadata
                    item.Fields["__Updated"]?.SetValue(DateTime.UtcNow.ToString("yyyyMMddTHHmmss"), true);

                    // Add to processed list
                    args.ProcessedItems.Add(item);
                    args.TotalItemsProcessed++;

                    item.Editing.EndEdit();

                    Log.Info($"Processed item: {item.Name} (ID: {item.ID})", this);
                }
                catch
                {
                    item.Editing.CancelEdit();
                    throw;
                }
            }
        }

        private void ProcessChildItems(ContentPublishingPipelineArgs args)
        {
            var children = args.SourceItem.Children;
            if (children != null && children.Count > 0)
            {
                Log.Info($"Processing {children.Count} child items", this);

                foreach (Item child in children)
                {
                    ProcessSingleItem(args, child);
                }
            }
        }
    }

    #endregion

    #region Step 4: Publishing Processor

    /// <summary>
    /// Publishes content to target database
    /// </summary>
    public class PublishContentProcessor
    {
        public void Process(ContentPublishingPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, nameof(args));

            Log.Info($"Hello World from Pipeline - Step 4: Publishing '{args.SourceItem.Name}'", this);

            if (args.TargetDatabase == null)
            {
                args.TargetDatabase = Database.GetDatabase("web");
            }

            try
            {
                using (new SecurityDisabler())
                {
                    // Publish each processed item
                    foreach (var item in args.ProcessedItems)
                    {
                        PublishSingleItem(item, args.TargetDatabase);
                    }

                    Log.Info($"Published {args.ProcessedItems.Count} items to '{args.TargetDatabase.Name}' database", this);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Publishing error: {ex.Message}", ex, this);
                args.ValidationErrors.Add($"Publishing failed: {ex.Message}");
                args.AbortPipeline();
            }
        }

        private void PublishSingleItem(Item item, Database targetDatabase)
        {
            // Simplified publish - in real implementation, use PublishManager
            Log.Info($"Publishing item: {item.Name} to {targetDatabase.Name}", this);

            // Example: Copy item to target database
            var targetItem = targetDatabase.GetItem(item.ID);
            if (targetItem != null)
            {
                Log.Info($"Item {item.Name} already exists in target, updating...", this);
            }
            else
            {
                Log.Info($"Creating new item {item.Name} in target database", this);
            }
        }
    }

    #endregion

    #region Step 5: Finalization Processor

    /// <summary>
    /// Finalizes the pipeline and logs results
    /// </summary>
    public class FinalizePublishingProcessor
    {
        public void Process(ContentPublishingPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, nameof(args));

            Log.Info($"Hello World from Pipeline - Step 5: Finalizing publishing for '{args.SourceItem.Name}'", this);

            try
            {
                // Log final statistics
                Log.Info($"Pipeline Summary:", this);
                Log.Info($"  - Source Item: {args.SourceItem.Name} ({args.SourceItem.ID})", this);
                Log.Info($"  - Total Items Processed: {args.TotalItemsProcessed}", this);
                Log.Info($"  - Validation Errors: {args.ValidationErrors.Count}", this);
                Log.Info($"  - Approved By: {args.ApprovedBy ?? "N/A"}", this);
                Log.Info($"  - Target Database: {args.TargetDatabase?.Name ?? "N/A"}", this);

                // Clear cache for published items
                ClearCacheForPublishedItems(args);

                // Send notifications (placeholder)
                SendNotifications(args);

                Log.Info($"Publishing pipeline completed successfully!", this);
            }
            catch (Exception ex)
            {
                Log.Error($"Finalization error: {ex.Message}", ex, this);
            }
        }

        private void ClearCacheForPublishedItems(ContentPublishingPipelineArgs args)
        {
            foreach (var item in args.ProcessedItems)
            {
                Sitecore.Caching.CacheManager.ClearAllCaches();
                Log.Info($"Cache cleared for: {item.Name}", this);
            }
        }

        private void SendNotifications(ContentPublishingPipelineArgs args)
        {
            // Placeholder for notification logic
            Log.Info($"Notifications sent for published content", this);
        }
    }

    #endregion

    #region Pipeline Runner

    /// <summary>
    /// Helper class to execute the content publishing pipeline
    /// </summary>
    public class ContentPublishingRunner
    {
        public static ContentPublishingPipelineArgs PublishContent(Item item, Database targetDb = null)
        {
            Assert.ArgumentNotNull(item, nameof(item));

            var args = new ContentPublishingPipelineArgs
            {
                SourceItem = item,
                TargetDatabase = targetDb ?? Database.GetDatabase("web")
            };

            Log.Info($"Starting content publishing pipeline for '{item.Name}'", typeof(ContentPublishingRunner));

            // Execute the pipeline
            CorePipeline.Run("contentPublishing", args);

            // Return results
            if (args.IsValid && args.ValidationErrors.Count == 0)
            {
                Log.Info($"Pipeline succeeded! {args.TotalItemsProcessed} items published", typeof(ContentPublishingRunner));
            }
            else
            {
                Log.Warn($"Pipeline failed with {args.ValidationErrors.Count} errors", typeof(ContentPublishingRunner));
            }

            return args;
        }
    }

    #endregion
}

/*

CONFIG FILE: App_Config/Include/YourModule/PipelineExample.config

<?xml version="1.0" encoding="utf-8" ?>
<configuration xmlns:patch="http://www.sitecore.net/xmlconfig/">
  <sitecore>
    <pipelines>
      <!-- Define a completely NEW custom pipeline -->
      <customDataProcessing>
        <!-- Processors run in the order they are defined -->
        <processor type="Sitecoretrainings.Examples.ValidateDataProcessor, Sitecoretrainings" method="Process"/>
        <processor type="Sitecoretrainings.Examples.ProcessDataProcessor, Sitecoretrainings" method="Process"/>
        <processor type="Sitecoretrainings.Examples.FinalizeDataProcessor, Sitecoretrainings" method="Process"/>
      </customDataProcessing>
    </pipelines>
  </sitecore>
</configuration>


HOW TO CALL FROM CODE:
 var args = new CustomPipelineArgs { Message = "test" };
 CorePipeline.Run("customDataProcessing", args);

KEY POINTS:
- A pipeline is a COLLECTION of processors working together
- You define the ENTIRE workflow (not just one step)
- Processors share data through the args object
- Use CorePipeline.Run() to execute your custom pipeline
- Each processor can abort the pipeline or modify shared data

 */
