using Master40.SimulationCore.DistributionProvider;
using Zpp.Simulation.Types;

namespace Zpp.Simulation
{
    public interface ISimulator
    {
        bool ProcessCurrentInterval(SimulationInterval simulationInterval,
            OrderGenerator orderGenerator);
    }
}