using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniversalResourceTransferRedux
{
    // The idea of this class is to save and load the URT registry.
    // Scenario modules are per game save.
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER)]
    internal class URT_Registry : ScenarioModule
    {
        // Maps each receiver ID to a list of (transmitter ID, received power)
        private Dictionary<int, List<(int transmitterId, float recvPower)>> receiverTransmitterPairings
            = new Dictionary<int, List<(int, float)>>();

        // Transmitter ID -> partId
        private Dictionary<int , uint> transmitters = new Dictionary<int, uint>();

        // Receiver ID -> partId
        private Dictionary<int, uint> receivers = new Dictionary<int, uint>();

        //Transmitter ID -> ProtoPartModuleSnapshot
        private Dictionary<int, ProtoPartModuleSnapshot> transmittersProtoPartModules = new Dictionary<int, ProtoPartModuleSnapshot>();

        //Receiver ID -> ProtoPartModuleSnapshot
        private Dictionary<int, ProtoPartModuleSnapshot> receiverProtoPartModules = new Dictionary<int, ProtoPartModuleSnapshot>();

        //Private counters for next receiver and transmitter
        [KSPField(isPersistant = true)]
        private int nextTransmitterId = 1;

        [KSPField(isPersistant = true)]
        private int nextReceiverId = 1;

        #region Game hooks

        public override void OnSave(ConfigNode node)
        {
            /*Data structure
            Pairings:
                Dictionary of recvId mapped to a list of (transmitterId, recvPower)
            transmitters:
                Dictionary of transmitterId mapped to uint of flightId
            receivers:
                Dictionary of recvId mapped to unit of flightId

            Proposed confignode structure:

            NODE
            {
                RECEIVER-TRANSMITTER-PAIRINGS
                {
                    RECEIVER
                    {
                        receiverId = (id here)
                        TRANSMITTERS
                        {
                            TRANSMITTER
                            {
                                transmitterId = (id here)
                                recvPower = (recvPower here)
                            }
                        }
                    }
                }
                RECEIVERS
                {
                    RECEIVER
                    {
                        receiverId = (id here)
                        receiverPartFlightId = (flightId here)
                    }
                }
                TRANSMITTERS
                {
                    TRANSMITTER
                    {
                        transmitterId = (id here)
                        transmitterPartFlightId = (flightId here)
                    }
                }
            }
             */


            ConfigNode pairingsNode = new ConfigNode();
            ConfigNode transmittersNode = new ConfigNode();
            ConfigNode receiversNode = new ConfigNode();

            foreach (int receiverId in receiverTransmitterPairings.Keys)
            {
                ConfigNode currentPairings = new ConfigNode();
                ConfigNode currentReceiverTransmitters = new ConfigNode();
                currentPairings.AddValue("receiverId", receiverId);

                foreach((int transmitterId, float recvPower) in receiverTransmitterPairings[receiverId])
                {
                    ConfigNode currentTransmitter = new ConfigNode();
                    currentTransmitter.AddValue("transmitterId", transmitterId);
                    currentTransmitter.AddValue("recvPower", recvPower);
                    currentReceiverTransmitters.AddNode("TRANSMITTER", currentTransmitter);
                }
                currentPairings.AddNode("TRANSMITTERS", currentReceiverTransmitters);
                pairingsNode.AddNode("RECEIVER", currentPairings);
            }

            node.AddNode("RECEIVER-TRANSMITTER-PAIRINGS", pairingsNode);

            foreach (int transmitterId in transmitters.Keys)
            {
                ConfigNode currentTransmitter = new ConfigNode();
                currentTransmitter.AddValue("transmitterId", transmitterId);
                currentTransmitter.AddValue("transmitterPartFlightId", transmitters[transmitterId]);
                transmittersNode.AddNode("TRANSMITTER", currentTransmitter);
            }

            node.AddNode("TRANSMITTERS", transmittersNode);

            foreach (int receiverId in receivers.Keys)
            {
                ConfigNode currentReceiver = new ConfigNode();
                currentReceiver.AddValue("receiverId", receiverId);
                currentReceiver.AddValue("receiverPartFlightId", receivers[receiverId]);
                receiversNode.AddNode("RECEIVER", currentReceiver);
            }
            node.AddNode("RECEIVERS", receiversNode);
        }

        public override void OnLoad(ConfigNode node)
        {
            ConfigNode pairingsNode = node.GetNode("RECEIVER-TRANSMITTER-PAIRINGS");
            ConfigNode transmittersNode = node.GetNode("TRANSMITTERS");
            ConfigNode receiversNode = node.GetNode("RECEIVERS");

            foreach (ConfigNode currentReceiver in pairingsNode.GetNodes("RECEIVER"))
            {
                ConfigNode ChildTransmittersNodes = currentReceiver.GetNode("TRANSMITTERS");
                List<(int transmitterId, float recvPower)> transmitterConfigs = new List<(int transmitterId, float recvPower)>();
                var receiverId = int.Parse(currentReceiver.GetValue("receiverId"));

                foreach (ConfigNode currentTransmitter in ChildTransmittersNodes.GetNodes("TRANSMITTER"))
                {
                    var transmitterId = int.Parse(currentTransmitter.GetValue("transmitterId"));
                    var recvPower = float.Parse(currentTransmitter.GetValue("recvPower"));
                    transmitterConfigs.Add((transmitterId, recvPower));
                }

                receiverTransmitterPairings.Add(receiverId, transmitterConfigs);
            }

            foreach (ConfigNode currentTransmitter in transmittersNode.GetNodes("TRANSMITTER"))
            {
                var transmitterId = int.Parse(currentTransmitter.GetValue("transmitterId"));
                var transmitterFlightId = uint.Parse(currentTransmitter.GetValue("transmitterPartFlightId"));

                transmitters.Add(transmitterId, transmitterFlightId);
            }

            foreach (ConfigNode currentReceiver in receiversNode.GetNodes("RECEIVER"))
            {
                var receiverId = int.Parse(currentReceiver.GetValue("receiverId"));
                var receiverFlightId = uint.Parse(currentReceiver.GetValue("receiverPartFlightId"));

                receivers.Add(receiverId, receiverFlightId);
            }
            
            //TODO: Load data BEFORE protopartmodule caching
            (transmittersProtoPartModules, receiverProtoPartModules) = BuildTransmittersAndReceiversModulesLists();
        }

        #endregion


        #region Internal functions

        private (Dictionary<int, ProtoPartModuleSnapshot>, Dictionary<int, ProtoPartModuleSnapshot>) BuildTransmittersAndReceiversModulesLists()
        {
            Dictionary<int, ProtoPartModuleSnapshot> transmitterModulesList = new Dictionary<int, ProtoPartModuleSnapshot>();
            Dictionary<int, ProtoPartModuleSnapshot> receiversModulesList = new Dictionary<int, ProtoPartModuleSnapshot>();

            foreach (int transmitterId in transmitters.Keys)
            {
                var transmitterPart = FlightGlobals.FindProtoPartByID(transmitters[transmitterId]);
                var transmitterModule = transmitterPart.modules.Find(s => s.moduleName == "URT_Transmitter" && int.Parse(s.moduleValues.GetValue("transmitterID")) == transmitterId);
                transmitterModulesList.Add(transmitterId, transmitterModule);
            }
            foreach (int receiverId in receivers.Keys)
            {
                var receiverPart = FlightGlobals.FindProtoPartByID(receivers[receiverId]);
                var receiverModule = receiverPart.modules.Find(s => s.moduleName == "URT_Receiver" && int.Parse(s.moduleValues.GetValue("receiverID")) == receiverId);
                receiversModulesList.Add(receiverId, receiverModule);
            }

            return (transmitterModulesList, receiversModulesList);
        }

        #endregion

        #region Interface functions
        // Add a transmitter
        public int RegisterTransmitter(uint partFlightId)
        {
            var assignedId = nextTransmitterId;
            transmitters.Add(assignedId, partFlightId);
            nextTransmitterId += 1;
            return assignedId;
        }

        // Add a receiver
        public int RegisterReceiver(uint partFlightId)
        {
            var assignedId = nextReceiverId;
            receivers.Add(assignedId, partFlightId);
            nextReceiverId += 1;
            return assignedId;
        }

        // Link a transmitter to a receiver
        public void LinkTransmitterToReceiver(int receiverId, int transmitterId, float power)
        {
            if (!receiverTransmitterPairings.ContainsKey(receiverId))
            {
                receiverTransmitterPairings[receiverId] = new List<(int, float)>();
            }
            receiverTransmitterPairings[receiverId].Add((transmitterId, power));
        }

        // Get total received power for a receiver
        public float GetTotalReceivedPower(int receiverId)
        {
            if (receiverTransmitterPairings.TryGetValue(receiverId, out var list))
            {
                return list.Sum(entry => entry.recvPower);
            }
            return 0.0F;
        }
        public (int receiverId, uint receiverPartId) GetReceiverLinkedToTransmitter(int transmitterId)
        {
            foreach (var pair in receiverTransmitterPairings)
            {
                foreach ((int transmitterTestedId, double recvPower) in pair.Value)
                {
                    if (transmitterTestedId == transmitterId)
                    {
                        return (pair.Key, receivers[pair.Key]);
                    }
                }
            }
            return (-1, 1);
        }

        public List<(int transmitterId, float recvPower)> GetReceiverPairings(int receiverId)
        {
            return receiverTransmitterPairings[receiverId];
        }

        public ProtoPartModuleSnapshot GetTransmitterProtoPartModuleByTransmitterId(int transmitterId)
        {
            return transmittersProtoPartModules[transmitterId];
        }

        public ProtoPartModuleSnapshot GetReceiverProtoPartModuleByReceiverId(int receiverId)
        {
            return receiverProtoPartModules[receiverId];
            
        }
        #endregion
    }
}