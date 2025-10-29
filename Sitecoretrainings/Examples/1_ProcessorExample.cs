using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.SecurityModel;
using System;
using System.Web;

namespace Sitecoretrainings.Examples
{
    /// <summary>
    /// PROCESSOR EXAMPLE - Enhanced Version
    ///
    /// A Processor is a single step in an existing pipeline.
    /// This example demonstrates real-world security and access control logic.
    ///
    /// Use Case: Enforce security rules, validate access, handle redirects
    /// </summary>
    public class DemoProcessor : HttpRequestProcessor
    {
        // Configuration properties (can be set via config)
        public string ProtectedPath { get; set; } = "/sitecore/content/secure";
        public string LoginPagePath { get; set; } = "/login";
        public string UnauthorizedPagePath { get; set; } = "/unauthorized";

        public override void Process(HttpRequestArgs args)
        {
            Assert.ArgumentNotNull(args, nameof(args));

            try
            {
                // Skip processing for Sitecore backend, media requests, and API calls
                if (Context.Site == null || Context.Site.Name.Equals("shell", StringComparison.OrdinalIgnoreCase) || Context.Site.Name.Equals("admin", StringComparison.OrdinalIgnoreCase) || args.LocalPath.StartsWith("/sitecore/", StringComparison.OrdinalIgnoreCase) || args.LocalPath.StartsWith("/-/", StringComparison.OrdinalIgnoreCase) || args.LocalPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                Log.Info($"Hello World from Processor - Processing request: {args.Url.FilePath}", this);

                // Get the current item being requested
                Item currentItem = Context.Item;
                if (currentItem == null)
                {
                    Log.Warn($"Processor: No item resolved for path {args.LocalPath}", this);
                    return;
                }

                // Check if the item is under a protected path
                if (IsUnderProtectedPath(currentItem))
                {
                    Log.Info($"Processor: Item '{currentItem.Name}' is under protected path", this);

                    // Check if user is authenticated
                    if (!Context.User.IsAuthenticated)
                    {
                        Log.Info("Processor: User not authenticated, redirecting to login", this);
                        RedirectToLogin(args);
                        return;
                    }

                    // Check if user has read access to the item
                    if (!HasReadAccess(currentItem))
                    {
                        Log.Info($"Processor: User '{Context.User.Name}' lacks access to '{currentItem.Name}'", this);
                        RedirectToUnauthorized(args);
                        return;
                    }

                    Log.Info($"Processor: Access granted to '{currentItem.Name}' for user '{Context.User.Name}'", this);
                }

                // Example: Add custom HTTP headers
                AddCustomHeaders(args);

                // Example: Track page views (simplified)
                TrackPageView(currentItem);
            }
            catch (Exception ex)
            {
                Log.Error($"Processor error: {ex.Message}", ex, this);
                // Don't abort pipeline on error, just log it
            }
        }

        /// <summary>
        /// Check if item is under a protected content path
        /// </summary>
        private bool IsUnderProtectedPath(Item item)
        {
            if (item == null) return false;

            // Check if any ancestor path matches the protected path
            return item.Paths.FullPath.StartsWith(ProtectedPath, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if current user has read access to the item
        /// </summary>
        private bool HasReadAccess(Item item)
        {
            if (item == null) return false;

            // Use Sitecore security to check read access
            using (new SecurityDisabler()) // Temporarily disable to check without recursion
            {
                return item.Security.CanRead(Context.User);
            }
        }

        /// <summary>
        /// Redirect to login page and preserve return URL
        /// </summary>
        private void RedirectToLogin(HttpRequestArgs args)
        {
            var returnUrl = HttpUtility.UrlEncode(args.Url.FilePathWithQueryString);
            var loginUrl = $"{LoginPagePath}?returnUrl={returnUrl}";

            HttpContext.Current.Response.Redirect(loginUrl, true);
            args.AbortPipeline(); // Stop further pipeline processing
        }

        /// <summary>
        /// Redirect to unauthorized page
        /// </summary>
        private void RedirectToUnauthorized(HttpRequestArgs args)
        {
            HttpContext.Current.Response.Redirect(UnauthorizedPagePath, true);
            args.AbortPipeline(); // Stop further pipeline processing
        }

        /// <summary>
        /// Add custom HTTP headers to the response
        /// </summary>
        private void AddCustomHeaders(HttpRequestArgs args)
        {
            if (HttpContext.Current?.Response != null)
            {
                HttpContext.Current.Response.Headers.Add("X-Custom-Processor", "DemoProcessor");
                HttpContext.Current.Response.Headers.Add("X-Processed-By", "Sitecoretrainings");
            }
        }

        /// <summary>
        /// Track page view for analytics (simplified example)
        /// </summary>
        private void TrackPageView(Item item)
        {
            if (item == null) return;

            // In real implementation, this would integrate with analytics system
            Log.Info($"Processor: Page view tracked for '{item.Name}' (ID: {item.ID})", this);
        }
    }

    /// <summary>
    /// ITEM VALIDATION PROCESSOR EXAMPLE
    /// Demonstrates validation in the item:saving pipeline
    /// </summary>
    public class ItemValidationProcessor
    {
        public void Process(Sitecore.Events.SitecoreEventArgs args)
        {
            Assert.ArgumentNotNull(args, nameof(args));

            // Extract the item being saved
            var item = Sitecore.Events.Event.ExtractParameter<Item>(args, 0);
            if (item == null) return;

            Log.Info($"Hello World from Processor - Validating item: {item.Name}", this);

            // Skip if we're in a system context or shell site
            if (Context.Site?.Name == "shell" || Context.User?.Name == @"sitecore\admin")
            {
                return;
            }

            try
            {
                // Example validation: Check if required fields are filled
                if (item.TemplateID.ToString() == "{YOUR-TEMPLATE-ID}")
                {
                    var titleField = item.Fields["Title"];
                    var descriptionField = item.Fields["Description"];

                    if (titleField != null && string.IsNullOrWhiteSpace(titleField.Value))
                    {
                        Log.Warn($"Validation: Item '{item.Name}' is missing required 'Title' field", this);
                        args.Result.Cancel = true;
                        return;
                    }

                    if (descriptionField != null && descriptionField.Value.Length < 10)
                    {
                        Log.Warn($"Validation: Item '{item.Name}' has too short description", this);
                        args.Result.Cancel = true;
                        return;
                    }
                }

                // Example: Enforce naming conventions
                if (item.Name.Contains(" "))
                {
                    Log.Warn($"Validation: Item name '{item.Name}' contains spaces", this);
                    // Could auto-fix: item.Name = item.Name.Replace(" ", "-");
                }

                Log.Info($"Validation: Item '{item.Name}' passed all checks", this);
            }
            catch (Exception ex)
            {
                Log.Error($"Validation error for item '{item.Name}': {ex.Message}", ex, this);
            }
        }
    }
}

/*

CONFIG FILE: App_Config/Include/YourModule/ProcessorExample.config

<?xml version="1.0" encoding="utf-8" ?>
<configuration xmlns:patch="http://www.sitecore.net/xmlconfig/">
  <sitecore>
    <pipelines>
      <!-- Plug this processor into the httpRequestBegin pipeline -->
      <httpRequestBegin>
        <processor type="Sitecoretrainings.Examples.DemoProcessor, Sitecoretrainings" patch:after="processor[@type='Sitecore.Pipelines.HttpRequest.StartMeasurements, Sitecore.Kernel']"/>
      </httpRequestBegin>
    </pipelines>
  </sitecore>
</configuration>


KEY POINTS:
- A processor is ONE step in an EXISTING pipeline
- Processors run in a specific order (use patch:before/after to control position)
- Can abort the pipeline by calling args.AbortPipeline()

*/
