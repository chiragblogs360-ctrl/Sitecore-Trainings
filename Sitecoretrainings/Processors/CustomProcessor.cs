using Sitecore.Diagnostics;
using Sitecore.Pipelines.HttpRequest;

namespace Sitecoretrainings.Processors
{
    public class CustomProcessor : HttpRequestProcessor
    {
        public override void Process(Sitecore.Pipelines.HttpRequest.HttpRequestArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Log.Info("Hello World from Processor (runs after CustomPipelineProcessor)!", this);
        }
    }
}
