using Sitecore.Diagnostics;
using Sitecore.Events;
using System;

namespace Sitecoretrainings.Events
{
    public class PublishEventHandler
    {
        public void OnItemPublished(object sender, EventArgs args)
        {
            Log.Info("Hello World from Item Published Event!", this);
        }

        public void OnItemPublishing(object sender, EventArgs args)
        {
            Log.Info("Hello World from Item Publishing Event!", this);
        }

        public void OnPublishEnd(object sender, EventArgs args)
        {
            Log.Info("Hello World from Publish End Event!", this);
        }
    }
}
