using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UniversalResourceTransferRedux.GenericUtils;

namespace UniversalResourceTransferRedux.RegistryComponents
{
    internal partial class URT_Registry
    {
        public int[] GetAllReceiverIds()
        {
            return receiverFlightIds.Keys.ToArray();
        }

        public int[] GetAllTransmitterIds()
        {
            return transmitterFlightIds.Keys.ToArray();
        }
        public int GetTransmitterTarget(int transmitterId)
        {
            if (activeTransmitterCache.TryGetValue(transmitterId, out URT_Transmitter transmitter)
                && transmitter != null)
            {
                return transmitter.targetId;
            }
            else if (!transmitterFlightIds.ContainsKey(transmitterId))
            {
                return -1;
            }
            else if (FlightGlobals.FindPartByID(transmitterFlightIds[transmitterId]) is Part part)
            {
                return part.FindModulesImplementing<URT_Transmitter>().Find(s => s.transmitterID == transmitterId).targetId;
            }
            else if (FlightGlobals.FindProtoPartByID(transmitterFlightIds[transmitterId]) is ProtoPartSnapshot protoPart)
            {
                return protoPart.modules.FindAll(s => s.moduleName == "URT_Transmitter").
                    Find(s => s.moduleValues.GetInt("transmitterID") == transmitterId).
                    moduleValues.GetInt("targetId");
            }
            else
            {
                return -1;
            }
        }
        public PartAndVesselName? GetReceiverPartAndVesselName(int receiverId)
        {
            if (activeReceiverCache.TryGetValue(receiverId, out URT_Receiver receiver)
                && receiver != null)
            {
                return PartAndVesselName.Create(receiver.part.partName,
                        receiver.vessel.vesselName);
            }
            else if (!receiverFlightIds.ContainsKey(receiverId))
            {
                return null;
            }
            else if (FlightGlobals.FindPartByID(receiverFlightIds[receiverId]) is Part part)
            {

                return PartAndVesselName.Create(part.partName, part.vessel.vesselName);
            }
            else if (FlightGlobals.FindProtoPartByID(receiverFlightIds[receiverId]) is ProtoPartSnapshot protoPart)
            {
                return PartAndVesselName.Create(protoPart.partName, protoPart.pVesselRef.vesselName);
            }
            else
            {
                return null;
            }
        }

        public PartAndVesselName? GetTransmitterPartAndVesselName(int transmitterId)
        {
            if (activeTransmitterCache.TryGetValue(transmitterId, out URT_Transmitter transmitter) && transmitter != null)
            {
                return PartAndVesselName.Create(transmitter.part.partName, transmitter.vessel.vesselName);
            }
            else if (!transmitterFlightIds.ContainsKey(transmitterId))
            {
                return null;
            }
            else if (FlightGlobals.FindPartByID(transmitterFlightIds[transmitterId]) is Part part)
            {
                return PartAndVesselName.Create(part.partName, part.vessel.vesselName);
            }
            else if (FlightGlobals.FindProtoPartByID(transmitterFlightIds[transmitterId]) is ProtoPartSnapshot protoPart)
            {
                return PartAndVesselName.Create(protoPart.partName, protoPart.pVesselRef.vesselName);
            }
            else
            {
                return null;
            }
        }
        public ReceiverInfo? GetReceiverInfo(int receiverId)
        {
            if (activeReceiverCache.TryGetValue(receiverId, out URT_Receiver activeReceiver))
            {
                return activeReceiver.GetReceiverInfo();
            }
            else if (!receiverFlightIds.TryGetValue(receiverId, out uint receiverFlightId))
            {
                return null;
            }
            else if (FlightGlobals.FindPartByID(receiverFlightId) is Part receiverPart)
            {
                return receiverPart.FindModulesImplementing<URT_Receiver>()?.Find(s => s.receiverID == receiverId)?.GetReceiverInfo();
            }
            else if (FlightGlobals.FindProtoPartByID(receiverFlightId) is ProtoPartSnapshot receiverProtoPart)
            {
                var protoModule = receiverProtoPart.modules.Find(s => s.moduleName == "URT_Receiver" && s.moduleValues.GetInt("receiverID") == receiverId);
                if (protoModule == null) return null;


                var prefabModule = receiverProtoPart.partInfo.partPrefab.FindModuleImplementing<URT_Receiver>();
                if (prefabModule == null) return null;


                var moduleArea = prefabModule.receiverArea; 
                var moduleEfficiency = prefabModule.receiverEfficiency;
                var receiverTuningFactor = prefabModule.receiverTuningFactor;


                var moduleWavelength = protoModule.moduleValues.GetFloat("receiverWavelength", prefabModule.receiverWavelength);
                var moduleIsReceiving = protoModule.moduleValues.GetBool("isReceiving");

                List<int> pairedTransmitters = new List<int>();
                var serializedList = protoModule.moduleValues.GetString("pairedTransmitters", "");
                if (!string.IsNullOrEmpty(serializedList))
                {
                    pairedTransmitters = serializedList.Split(',').Select(int.Parse).ToList();
                }

                return ReceiverInfo.Create(
                    moduleArea,
                    moduleWavelength,
                    moduleEfficiency,
                    receiverProtoPart.pVesselRef,
                    pairedTransmitters,
                    moduleIsReceiving,
                    receiverTuningFactor
                );
            }
            return null;
        }

        public TransmitterInfo? GetTransmitter(int transmitterId)
        {
            if (activeTransmitterCache.TryGetValue(transmitterId, out var activeTransmitter))
            {
                return activeTransmitter.GetTransmitterInfo();
            }
            else if (!transmitterFlightIds.TryGetValue(transmitterId, out var transmitterFlightId))
            {
                return null;
            }
            else if (FlightGlobals.FindPartByID(transmitterFlightId) is Part transmitterPart)
            {
                return transmitterPart.FindModulesImplementing<URT_Transmitter>()?.Find(s => s.transmitterID == transmitterId)?.GetTransmitterInfo();
            }
            else if (FlightGlobals.FindProtoPartByID(transmitterFlightId) is ProtoPartSnapshot transmitterProtoPart)
            {
                var protoModule = transmitterProtoPart.modules?.Find(s => s.moduleName == "URT_Transmitter" && s.moduleValues.GetInt("transmitterID") == transmitterId);
                if (protoModule == null) return null;

                var prefabModule = transmitterProtoPart.partInfo.partPrefab.FindModuleImplementing<URT_Transmitter>();
                if (prefabModule == null) return null;

                return TransmitterInfo.Create(
                    prefabModule.transmitterArea,                                                           
                    protoModule.moduleValues.GetFloat("transmitterWavelength", prefabModule.transmitterWavelength), 
                    prefabModule.transmitterEfficiency,                                                     
                    transmitterProtoPart.pVesselRef,                                                        
                    protoModule.moduleValues.GetFloat("transmittedPower", 0f),                              
                    protoModule.moduleValues.GetBool("isTransmitting", false),                              
                    prefabModule.buildQuality                                                               
                );
            }
            return null;
        }

    }
}
