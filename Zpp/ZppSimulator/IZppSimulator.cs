using Zpp.Simulation.Types;
using Zpp.ZppSimulator.impl;

namespace Zpp.ZppSimulator
{
    public interface IZppSimulator
    {
        void StartOneCycle(SimulationInterval simulationInterval);

        void StartPerformanceStudy();

        void StartTestCycle();
    }
}