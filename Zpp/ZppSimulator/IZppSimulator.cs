using Master40.DB.Data.WrappersForPrimitives;
using Zpp.ZppSimulator.impl;

namespace Zpp.ZppSimulator
{
    public interface IZppSimulator
    {
        void StartOneCycle(SimulationInterval simulationInterval, Quantity customerOrderQuantity);

        void StartOneCycle(SimulationInterval simulationInterval);

        void StartPerformanceStudy();

        void StartTestCycle();

        void StartMultipleTestCycles();
    }
}