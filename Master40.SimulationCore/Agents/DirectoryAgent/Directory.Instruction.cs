﻿using Akka.Actor;
using AkkaSim.Definitions;
using Master40.DB.Models;
using Master40.SimulationImmutables;

namespace Master40.SimulationCore.Agents
{
    public partial class Directory
    {
        public class Instruction
        {
            public class CreateHubAgent : SimulationMessage
            {
                public static CreateHubAgent Create(FHubInformation message, IActorRef target)
                {
                    return new CreateHubAgent(message, target);
                }
                private CreateHubAgent(object message, IActorRef target) : base(message, target)
                {
                }
                public FHubInformation GetObjectFromMessage { get => Message as FHubInformation; }
            }

            public class RequestRessourceAgent : SimulationMessage
            {
                public static RequestRessourceAgent Create(string descriminator, IActorRef target)
                {
                    return new RequestRessourceAgent(descriminator, target);
                }
                private RequestRessourceAgent(object message, IActorRef target) : base(message, target)
                {
                }
                public string GetObjectFromMessage { get => Message as string; }
            }

            public class RegisterResources : SimulationMessage
            {
                public static RegisterResources Create(string descriminator, IActorRef target)
                {
                    return new RegisterResources(descriminator, target);
                }
                private RegisterResources(object message, IActorRef target) : base(message, target)
                {
                }
                public string GetObjectFromMessage { get => Message as string; }
            }

            public class CreateMachineAgents : SimulationMessage
            {
                public static CreateMachineAgents Create(FRessourceDefinition message, IActorRef target)
                {
                    return new CreateMachineAgents(message, target);
                }
                private CreateMachineAgents(object message, IActorRef target) : base(message, target)
                {
                }
                public FRessourceDefinition GetObjectFromMessage { get => Message as FRessourceDefinition; }
            }
            public class CreateStorageAgents : SimulationMessage
            {
                public static CreateStorageAgents Create(Stock message, IActorRef target)
                {
                    return new CreateStorageAgents(message, target);
                }
                private CreateStorageAgents(object message, IActorRef target) : base(message, target)
                {
                }
                public Stock GetObjectFromMessage { get => Message as Stock; }
            }
        }
    }
}