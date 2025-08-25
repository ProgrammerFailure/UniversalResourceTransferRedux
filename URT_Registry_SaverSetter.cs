using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using UnityEngine;
using static UniversalResourceTransferRedux.GenericUtils;
using static VehiclePhysics.EnergyProvider;

namespace UniversalResourceTransferRedux
{
    // Scenario modules are per game save.
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER)]
    internal class URT_Registry : ScenarioModule
    {
        [KSPField(isPersistant = true)]
        int nextTransmitterId = 1;

        [KSPField(isPersistant = true)]
        int nextReceiverId = 1;

        Dictionary<int, uint> transmitterFlightIds = new Dictionary<int, uint>();
        Dictionary<int, uint> receiverFlightIds = new Dictionary<int, uint>();
        public int registerNewReceiverId(uint partFlightId)
        {
            var receiverId = nextReceiverId;
            nextReceiverId += 1;
            receiverFlightIds.Add(receiverId, partFlightId);
            return receiverId;
        }

        public int registerNewTransmitterId(uint partFlightId)
        {
            var transmitterId = nextTransmitterId;
            nextTransmitterId += 1;
            transmitterFlightIds.Add(transmitterId, partFlightId);
            return transmitterId;
        }

        public uint getTransmitterPartFlightIdById(int transmitterId)
        {
            return transmitterFlightIds[transmitterId];
        }

        public uint getReceiverPartFlightIdById(int receiverId)
        {
            return receiverFlightIds[receiverId];
        }

        public void deregisterReceiver(int receiverId)
        {
            receiverFlightIds.Remove(receiverId);
        }

        public void deregisterTransmitter(int transmitterId)
        {
            transmitterFlightIds.Remove(transmitterId);
        }

        public Dictionary<int, uint> getReceiverIds()
        {
            return receiverFlightIds;
        }

        public List<ReceiverInfo> FetchAllReceivers(int transmitterID, string className)
        {
            List<ReceiverInfo> output = new List<ReceiverInfo>();
            foreach (int receiverId in receiverFlightIds.Keys)
            {
                var receiverInfo = GetReceiverInfo(receiverId, className);
                if (receiverInfo != null)
                {
                    output.Add(receiverInfo.Value);
                }
            }
            return output;
        }

        public ReceiverInfo? GetReceiverInfo(int receiverId, string className)
        {
            // --- Stage 0: Initial Validation & Retrieve Flight ID ---
            // Make sure receiverId exists in the mapping
            uint flightId;
            if (!receiverFlightIds.TryGetValue(receiverId, out flightId))
            {
                Debug.LogError($"{className}: Unable to fetch receiver with receiverId: {receiverId}. Reason: receiverId not found in receiverFlightIds dictionary.");
                return null;
            }

            // --- Stage 1: Try to get the ProtoPartSnapshot ---
            // A ProtoPartSnapshot is generally always available for persistent parts, even if not active in scene.
            var receiverProtoPart = FlightGlobals.FindProtoPartByID(flightId);
            if (receiverProtoPart == null)
            {
                Debug.LogError($"{className}: Unable to fetch receiver with receiverId: {receiverId}. Reason: ProtoPartSnapshot with flightID {flightId} not found.");
                return null; // Cannot proceed without a proto part
            }

            // --- Stage 2: Try to get the live Part and its URT_Receiver module ---
            // This is the ideal state, where the part is loaded and active in the current vessel.
            Part receiverActualPart = null;
            URT_Receiver receiverModule = null;

            // Check if the proto part's vessel reference is valid and if the part exists in the active vessel's parts list
            if (receiverProtoPart.pVesselRef?.vesselRef?.Parts != null)
            {
                // Find the actual part in the vessel's loaded parts
                receiverActualPart = receiverProtoPart.pVesselRef.vesselRef.Parts.FirstOrDefault(s => s.flightID == flightId);

                if (receiverActualPart != null)
                {
                    // Find the URT_Receiver module on the actual part
                    receiverModule = receiverActualPart.FindModulesImplementing<URT_Receiver>()
                                                       .FirstOrDefault(s => s.receiverID == receiverId);

                    if (receiverModule != null)
                    {
                        // Found the live module, return its info directly
                        return receiverModule.GetReceiverInfo();
                    }
                    else
                    {
                        // Live part found, but specific module with receiverId was not.
                        // This scenario might indicate a data mismatch or module not properly initialized on the live part.
                        Debug.LogWarning($"{className}: Live part {receiverActualPart.partInfo.title} with registered flightId {receiverFlightIds[receiverId]} for receiver (ID: {receiverId}) found, but URT_Receiver module with matching receiverId not found on it. Falling back to ProtoPart data.");
                    }
                }
                else
                {
                    // ProtoPartSnapshot pointed to a vessel, but the specific part wasn't found in its loaded parts.
                    // This can happen if the vessel is loaded but the part isn't (less common in KSP but possible in complex scenarios)
                    Debug.LogWarning($"{className}: ProtoPartSnapshot for ID {receiverId} referenced a vessel, but actual part (flightID: {flightId}) not found in vessel parts. Falling back to ProtoPart data.");
                }
            }
            else
            {
                // The vessel reference from the proto part was null, indicating the vessel might not be loaded,
                // or there's an issue with the proto part's vessel association.
                Debug.LogWarning($"{className}: ProtoPartSnapshot for ID {receiverId} has no valid vessel reference. Falling back to ProtoPart data only.");
            }


            // --- Stage 3: Fallback to ProtoPartSnapshot module values ---
            // If the live part/module wasn't found or wasn't ideal, try to extract info from the ProtoPartSnapshot's config.
            // This handles cases where the part is not currently loaded in memory as a live object.
            var receiverProtoModuleData = receiverProtoPart.modules
                                                .Where(s => s.moduleName == "URT_Receiver") // First, filter by module name
                                                .Where(s => s.moduleValues != null) // Then, ensure moduleValues exist (crucial null check)
                                                .Where(s => s.moduleValues.HasValue("receiverId")) // Check if the specific key exists
                                                .FirstOrDefault(s => s.moduleValues.GetInt("receiverId") == receiverId); // Finally, find the one with the matching receiverId

            if (receiverProtoModuleData != null && receiverProtoModuleData.moduleValues != null)
            {
                // Found the module's config data in the proto part
                ReceiverInfo receiver = new ReceiverInfo();

                // Use GetValue with default to prevent errors if a key is missing
                receiver.Area = receiverProtoModuleData.moduleValues.GetFloat("receiverArea", 0f);
                receiver.Efficiency = receiverProtoModuleData.moduleValues.GetFloat("receiverEfficiency", 0f);
                receiver.Wavelength = receiverProtoModuleData.moduleValues.GetFloat("receiverWavelength", 0f);
                receiver.parentProtoVessel = receiverProtoPart.pVesselRef; // This can be null if vessel not loaded, which is fine for ProtoPartInfo

                return receiver;
            }
            else
            {
                // This means we found the proto part, but either it didn't have a URT_Receiver module
                // or the module didn't have the expected receiverId or moduleValues.
                Debug.LogError($"{className}: Unable to fetch receiver with receiverId: {receiverId}. Reason: No valid URT_Receiver module data found in ProtoPartSnapshot {receiverProtoPart.partInfo.name} (Flight ID: {flightId}).");
                return null;
            }
        }
    }
}