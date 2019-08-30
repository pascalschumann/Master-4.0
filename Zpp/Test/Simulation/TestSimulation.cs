using System;
using Xunit;
using Zpp.Simulation;
using Zpp.Simulation.Types;

namespace Zpp.Test
{
    public class TestSimulation : AbstractTest
    {
        public TestSimulation() : base(initDefaultTestConfig: true, useLocalDb: true)
        {
            MrpRun.RunMrp(ProductionDomainContext);
        }

        [Fact]
        public void TestSimulationWithResults()
        {

            var Simulator = new Simulator();
            var simulationInval = new SimulationInterval(0, 1440);
            Simulator.ProcessCurrentInterval(simulationInval, ProductionDomainContext);
        }
    }
}