using Master40.DB.Data.WrappersForPrimitives;
using Zpp.ZppSimulator.impl;

namespace Zpp.ZppSimulator
{
    public interface IZppSimulator
    {
        void StartOneCycle(SimulationInterval simulationInterval, Quantity customerOrderQuantity);

        void StartOneCycle(SimulationInterval simulationInterval);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shouldPersist">Should dbTransactionData and dbTransactionDataArchive
        /// be persisted at the end</param>
        void StartPerformanceStudy(bool shouldPersist);

        void StartTestCycle();

        void StartMultipleTestCycles();
    }
}