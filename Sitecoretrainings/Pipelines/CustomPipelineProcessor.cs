using Sitecore.Diagnostics;
using Sitecore.Pipelines.HttpRequest;

namespace Sitecoretrainings.Pipelines
{
    public class CustomPipelineProcessor : HttpRequestProcessor
    {
        public override void Process(Sitecore.Pipelines.HttpRequest.HttpRequestArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Log.Info("Hello World from Pipeline Processor!", this);
        }
    }
}
