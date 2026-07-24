using Bindito.Core;
using Timberborn.HttpApiSystem;
using TimberbornAutopilot.Http;
using TimberbornAutopilot.Planning;
using TimberbornAutopilot.Sensing;

namespace TimberbornAutopilot
{
    [Context("Game")]
    public class AutopilotGameConfigurator : IConfigurator
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
            containerDefinition.Bind<CampaignPlanner>().AsSingleton();
            containerDefinition.Bind<WorldModel>().AsSingleton();
            containerDefinition.Bind<AutopilotService>().AsSingleton();
            containerDefinition.MultiBind<IHttpApiEndpoint>().To<AutopilotStatusEndpoint>().AsSingleton();
        }
    }
}
