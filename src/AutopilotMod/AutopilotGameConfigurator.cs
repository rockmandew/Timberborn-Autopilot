using Bindito.Core;
using Timberborn.HttpApiSystem;
using TimberbornAutopilot.Acting;
using TimberbornAutopilot.Http;
using TimberbornAutopilot.Planning;
using TimberbornAutopilot.Sensing;
using TimberbornAutopilot.Training;

namespace TimberbornAutopilot
{
    [Context("Game")]
    public class AutopilotGameConfigurator : IConfigurator
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
            containerDefinition.Bind<AutopilotParams>().AsSingleton();
            containerDefinition.Bind<EpisodeRecorder>().AsSingleton();
            containerDefinition.Bind<CampaignPlanner>().AsSingleton();
            containerDefinition.Bind<BrainLog>().AsSingleton();
            containerDefinition.Bind<WorldQuery>().AsSingleton();
            containerDefinition.Bind<PathRouter>().AsSingleton();
            containerDefinition.Bind<OpeningBook>().AsSingleton();
            containerDefinition.Bind<WorldModel>().AsSingleton();
            containerDefinition.Bind<MapSurveyor>().AsSingleton();
            containerDefinition.Bind<BuildPlacer>().AsSingleton();
            containerDefinition.Bind<ZonePlanner>().AsSingleton();
            containerDefinition.Bind<CrewManager>().AsSingleton();
            containerDefinition.Bind<SpeedController>().AsSingleton();
            containerDefinition.Bind<AutopilotCommandEndpoint>().AsSingleton();
            containerDefinition.Bind<AutopilotService>().AsSingleton();
            containerDefinition.MultiBind<IHttpApiEndpoint>().To<AutopilotStatusEndpoint>().AsSingleton();
            containerDefinition.MultiBind<IHttpApiEndpoint>().To<AutopilotDashboardEndpoint>().AsSingleton();
            containerDefinition.MultiBind<IHttpApiEndpoint>().To<AutopilotRemoteEndpoint>().AsSingleton();
            containerDefinition.MultiBind<IHttpApiEndpoint>().ToExisting<AutopilotCommandEndpoint>();
        }
    }
}
