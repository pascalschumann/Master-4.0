using System.IO;
using System.Text;
using Zpp.DataLayer;
using Zpp.Mrp2.impl.Scheduling.impl;
using Zpp.Util.Graph.impl;
using Zpp.ZppSimulator.impl;

namespace Zpp.Util
{
    public class DebuggingTools
    {
        private static readonly string SimulationFolder = $"../../../Test/Ordergraphs/Simulation/";
        
        /**
         * includes demandToProviderGraph, OrderOperationGraph and dbTransactionData
         */
        public static void PrintStateToFiles(SimulationInterval simulationInterval,
            IDbTransactionData dbTransactionData, int countOfPrintsInOneCycle)
        {
            if (Constants.IsWindows == false)
            {
                // skip this in the cloud, results there in DirectoryNotFoundException 
                return;
            }
            if (simulationInterval.StartAt.Equals(0))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(SimulationFolder);
                if (directoryInfo.Exists)
                {
                    foreach (FileInfo file in directoryInfo.GetFiles())
                    {
                        file.Delete();
                    }
                }
            }
            
            File.WriteAllText(
                $"{SimulationFolder}dbTransactionData_interval_{simulationInterval.StartAt}_{countOfPrintsInOneCycle}.txt",
                dbTransactionData.ToString(), Encoding.UTF8);
            File.WriteAllText(
                $"{SimulationFolder}dbTransactionDataArchive_interval_{simulationInterval.StartAt}_{countOfPrintsInOneCycle}.txt",
                ZppConfiguration.CacheManager.GetDbTransactionDataArchive().ToString(), Encoding.UTF8);
            DemandToProviderGraph demandToProviderGraph = new DemandToProviderGraph();
            File.WriteAllText(
                $"{SimulationFolder}demandToProviderGraph_interval_{simulationInterval.StartAt}_{countOfPrintsInOneCycle}.txt",
                demandToProviderGraph.ToString(), Encoding.UTF8);
            OrderOperationGraph orderOperationGraph = new OrderOperationGraph();
            File.WriteAllText(
                $"{SimulationFolder}orderOperationGraph_interval_{simulationInterval.StartAt}_{countOfPrintsInOneCycle}.txt",
                orderOperationGraph.ToString(), Encoding.UTF8);
            
        }
    }
}