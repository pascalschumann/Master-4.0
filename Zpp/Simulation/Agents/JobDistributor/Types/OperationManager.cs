using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Akka.Actor;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Microsoft.EntityFrameworkCore.Internal;

namespace Zpp.Simulation.Agents.JobDistributor.Types
{
    public class OperationManager
    {
        public M_Machine Machine  { get; set; }
        public bool IsWorking { get; set; }
        public IActorRef ResourceRef { get; set; }
        public Queue<T_ProductionOrderOperation> JobQueue { get; set; }

        public OperationManager()
        {
            JobQueue = new Queue<T_ProductionOrderOperation>();
        }

        public bool SetStatusForFirstItemInQueue(ProducingState state)
        {
            if (JobQueue.Count == 0) return false;

            JobQueue.Peek().ProducingState = ProducingState.Waiting;
            return true;
        }

        public bool HasJobs()
        {
            return JobQueue.Any(x => x.ProducingState == ProducingState.Waiting);
        }
    }
}
