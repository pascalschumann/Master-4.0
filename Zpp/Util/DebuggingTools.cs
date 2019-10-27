using System;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using Zpp.DataLayer;
using Zpp.Mrp2.impl.Scheduling.impl;
using Zpp.Util.Graph.impl;
using Zpp.ZppSimulator.impl;

namespace Zpp.Util
{
    public static class DebuggingTools
    {
        private static readonly string SimulationFolder = $"../../../Test/Ordergraphs/Simulation/";
        private static readonly string performanceLogFileName = "performance.log";

        /**
         * includes demandToProviderGraph, OrderOperationGraph and dbTransactionData
         */
        public static void PrintStateToFiles(SimulationInterval simulationInterval,
            IDbTransactionData dbTransactionData, int countOfPrintsInOneCycle)
        {
            if (Constants.IsWindows == false || ZppConfiguration.IsInPerformanceMode)
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
                ZppConfiguration.CacheManager.GetDbTransactionDataArchive().ToString(),
                Encoding.UTF8);
            DemandToProviderGraph demandToProviderGraph = new DemandToProviderGraph();
            File.WriteAllText(
                $"{SimulationFolder}demandToProviderGraph_interval_{simulationInterval.StartAt}_{countOfPrintsInOneCycle}.txt",
                demandToProviderGraph.ToString(), Encoding.UTF8);
            OrderOperationGraph orderOperationGraph = new OrderOperationGraph();

            WriteToFile(orderOperationGraph.ToString(),
                $"orderOperationGraph_interval_{simulationInterval.StartAt}_{countOfPrintsInOneCycle}.log");
        }

        public static void WriteToFile(string content, string fileName)
        {
            File.WriteAllText($"{SimulationFolder}{fileName}", content,
                Encoding.UTF8);
        }

        public static void WritePerformanceLog(string content)
        {
            WriteToFile(content, performanceLogFileName);
        }

        public static string Prettify(long value)
        {
            string valueAsString = value.ToString();
            string newValue = "";
            int length = valueAsString.Length;
            int count = 0;
            for (int i = length - 1; i >= 0; i--, count++)
            {
                if (count > 0 && count % 3 == 0)
                {
                    newValue = "." + newValue;
                }
                newValue = valueAsString[i] + newValue;
            }

            return newValue;
        }

        private static string ReadFile(string pathToFile)
        {
            return File.ReadAllText(pathToFile, Encoding.UTF8);
        }

        public static string ReadPerformanceLog()
        {
            return ReadFile($"{SimulationFolder}{performanceLogFileName}");
        }
    }
}