using Bindito.Core;

namespace TimberbornAutopilot
{
    [Context("Game")]
    public class AutopilotGameConfigurator : IConfigurator
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
            containerDefinition.Bind<AutopilotService>().AsSingleton();
        }
    }
}
