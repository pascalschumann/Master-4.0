using System;
using System.Collections.Generic;
using System.Diagnostics;
using Akka.Actor;
using AkkaSim;
using Zpp.Simulation.Agents;
using Zpp.Simulation.Agents.Resource;
using Zpp.Simulation.Messages;

namespace Zpp.Simulation.Monitors
{
    public class WorkTimeMonitor : SimulationMonitor
    {
        public WorkTimeMonitor(long time) 
            : base(time, new List<Type> { typeof(Resource.FinishWork) })
        {
        }

        protected override void EventHandle(object o)
        {
            // base.EventHandle(o);
            var m = o as Resource.FinishWork;
            var material = m.Message as MaterialRequest;
            Debug.WriteLine("Finished: " + material.Material.Name + " on Time: " + _Time);
        }

        public static Props Props(long time)
        {
            return Akka.Actor.Props.Create(() => new WorkTimeMonitor(time));
        }
    }
}
