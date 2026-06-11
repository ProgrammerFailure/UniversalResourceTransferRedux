using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UniversalResourceTransferRedux.Core.RegistryComponents
{
    internal partial class URT_Registry
    {
        public void CallAllListeners()
        {
            foreach (var action in listeners)
            {
                action.Invoke();
            }
        }
        public int[] GetReceiverIDs()
        {
            return receiverFlightIds.Keys.ToArray();
        }
        public GenericUtils.TransmitterInfo? GetTransmitterInfo(int transmitterId)
        {
            if (activeTransmitterCache.TryGetValue(transmitterId, out var transmitterModule) && transmitterModule != null)
            {
#if DEBUG
                Debug.Log("[URT]: Active cache hit! Found transmitter module.");
#endif
                return transmitterModule.GetTransmitterInfo();
            }
            if (inactiveTransmitterCache.TryGetValue(transmitterId, out var transmitterProtoPart) &&
                transmitterProtoPart != null &&
                transmitterProtoPart.partPrefab
                .FindModulesImplementing<IURT_Transmitter>()
                .Find(s => s.ModuleID == transmitterModuleIds[transmitterId]) is IURT_Transmitter transmitterModule2)
            {
#if DEBUG
                Debug.Log("[URT]: Inactive cache hit! Found transmitter module on part prefab.");
#endif
                return transmitterModule2.GetTransmitterInfo();
            }
            RefreshCacheTransmitter(transmitterId);

            if (activeTransmitterCache.TryGetValue(transmitterId, out var transmitterModule3) && transmitterModule3 != null)
            {
#if DEBUG
                Debug.Log("[URT]: Active cache hit post refresh! Found transmitter module.");
#endif
                return transmitterModule3.GetTransmitterInfo();
            }
            if (inactiveTransmitterCache.TryGetValue(transmitterId, out var transmitterProtoPart2) &&
                transmitterProtoPart2 != null &&
                transmitterProtoPart2.partPrefab
                .FindModulesImplementing<IURT_Transmitter>()
                .Find(s => s.ModuleID == transmitterModuleIds[transmitterId]) is IURT_Transmitter transmitterModule4)
            {
#if DEBUG
                Debug.Log("[URT]: Inactive cache hit post refresh! Found transmitter module on part prefab.");
#endif
                return transmitterModule4.GetTransmitterInfo();
            }
#if DEBUG
            Debug.Log("[URT]: No cache hits for transmitter! Returning null.");
#endif
            return null;
        }
        public GenericUtils.ReceiverInfo? GetReceiverInfo(int receiverID)
        {
            if (activeReceiverCache.TryGetValue(receiverID, out var receiverModule) && receiverModule != null)
            {
#if DEBUG
                Debug.Log("[URT]: Active cache hit! Found receiver module.");
#endif
                return receiverModule.GetReceiverInfo();
            }
            if (inactiveReceiverCache.TryGetValue(receiverID, out var receiverProtoPart) &&
                receiverProtoPart != null &&
                receiverProtoPart.partPrefab
                .FindModulesImplementing<IURT_Receiver>()
                .Find(s => s.ModuleId == receiverModuleIds[receiverID]) is IURT_Receiver receiverModule2)
            {
#if DEBUG
                Debug.Log("[URT]: Inactive cache hit! Found receiver module on part prefab.");
#endif
                return receiverModule2.GetReceiverInfo();
            }
            RefreshCacheReceiver(receiverID);

            if (activeReceiverCache.TryGetValue(receiverID, out var receiverModule3) && receiverModule3 != null)
            {
#if DEBUG
                Debug.Log("[URT]: Active cache hit post refresh! Found receiver module.");
#endif
                return receiverModule3.GetReceiverInfo();
            }
            if (inactiveReceiverCache.TryGetValue(receiverID, out var receiverProtoPart2) &&
                receiverProtoPart2 != null &&
                receiverProtoPart2.partPrefab
                .FindModulesImplementing<IURT_Receiver>()
                .Find(s => s.ModuleId == receiverModuleIds[receiverID]) is IURT_Receiver receiverModule4)
                {
#if DEBUG
                Debug.Log("[URT]: Inactive cache hit post refresh! Found receiver module on part prefab.");
#endif
                return receiverModule4.GetReceiverInfo();
            }
#if DEBUG
            Debug.Log("[URT]: No cache hits. Returning null for receiver");
#endif
            return null;
        }

        public Vector3d? GetTransmitterWorldPos(int transmitterId)
        {
            if (activeTransmitterCache.TryGetValue(transmitterId, out var transmitterModule) && transmitterModule != null)
            {
                
                return transmitterModule.Vessel.GetWorldPos3D();
            }
            if (inactiveTransmitterCache.TryGetValue(transmitterId, out var transmitterProtoPart) &&
                transmitterProtoPart != null)
            {
                return GenericUtils.GetProtoVesselWorldPosAtTime(transmitterProtoPart.pVesselRef, time);
            }
            RefreshCacheTransmitter(transmitterId);
            
            if (activeTransmitterCache.TryGetValue(transmitterId, out var transmitterModule2) && transmitterModule2 != null)
            {
                return transmitterModule2.Vessel.GetWorldPos3D();
            }
            if (inactiveTransmitterCache.TryGetValue(transmitterId, out var transmitterProtoPart2) &&
                transmitterProtoPart2 != null)
            {
                return GenericUtils.GetProtoVesselWorldPosAtTime(transmitterProtoPart2.pVesselRef, time);
            }

            return null;
        }

        public Vector3d? GetReceiverWorldPos(int receiverID)
        {
            if (activeReceiverCache.TryGetValue(receiverID, out var receiverModule) && receiverModule != null)
            {
                return receiverModule.Vessel.GetWorldPos3D();
            }
            if (inactiveReceiverCache.TryGetValue(receiverID, out var receiverProtoPart) &&
                receiverProtoPart != null)
            {
                return GenericUtils.GetProtoVesselWorldPosAtTime(receiverProtoPart.pVesselRef, time);
            }
            RefreshCacheReceiver(receiverID);

            if (activeReceiverCache.TryGetValue(receiverID, out var receiverModule2) && receiverModule2 != null)
            {
                return receiverModule2.Vessel.GetWorldPos3D();
            }
            if (inactiveReceiverCache.TryGetValue(receiverID, out var receiverProtoPart2) &&
                receiverProtoPart2 != null)
            {
                return GenericUtils.GetProtoVesselWorldPosAtTime(receiverProtoPart2.pVesselRef, time);
            }

            return null;
        }

        public CelestialBody GetReceiverCelestialBody(int receiverID)
        {
            if (activeReceiverCache.TryGetValue(receiverID, out var receiverModule) && receiverModule != null)
            {
                return receiverModule.Vessel.mainBody;
            }
            if (inactiveReceiverCache.TryGetValue(receiverID, out var receiverProtoPart) &&
                receiverProtoPart != null)
            {
                if (receiverProtoPart.pVesselRef.vesselRef != null) return receiverProtoPart.pVesselRef.vesselRef.mainBody;
                else return FlightGlobals.Bodies[receiverProtoPart.pVesselRef.orbitSnapShot.ReferenceBodyIndex];
            }
            RefreshCacheReceiver(receiverID);

            if (activeReceiverCache.TryGetValue(receiverID, out var receiverModule2) && receiverModule2 != null)
            {
                return receiverModule2.Vessel.mainBody;
            }
            if (inactiveReceiverCache.TryGetValue(receiverID, out var receiverProtoPart2) &&
                receiverProtoPart2 != null)
            {
                if (receiverProtoPart2.pVesselRef.vesselRef != null) return receiverProtoPart2.pVesselRef.vesselRef.mainBody;
                else return FlightGlobals.Bodies[receiverProtoPart2.pVesselRef.orbitSnapShot.ReferenceBodyIndex];
            }

            return null;
        }

        public CelestialBody GetTransmitterCelestialBody(int transmitterId)
        {
            if (activeTransmitterCache.TryGetValue(transmitterId, out var transmitterModule) && transmitterModule != null)
            {
                return transmitterModule.Vessel.mainBody;
            }
            if (inactiveTransmitterCache.TryGetValue(transmitterId, out var transmitterProtoPart) &&
                transmitterProtoPart != null)
            {
                if (transmitterProtoPart.pVesselRef.vesselRef != null) return transmitterProtoPart.pVesselRef.vesselRef.mainBody;
                return FlightGlobals.Bodies[transmitterProtoPart.pVesselRef.orbitSnapShot.ReferenceBodyIndex];
            }
            RefreshCacheTransmitter(transmitterId);

            if (activeTransmitterCache.TryGetValue(transmitterId, out var transmitterModule2) && transmitterModule2 != null)
            {
                return transmitterModule2.Vessel.mainBody;
            }
            if (inactiveTransmitterCache.TryGetValue(transmitterId, out var transmitterProtoPart2) &&
                transmitterProtoPart2 != null)
            {
                if (transmitterProtoPart2.pVesselRef.vesselRef != null) return transmitterProtoPart2.pVesselRef.vesselRef.mainBody;
                return FlightGlobals.Bodies[transmitterProtoPart2.pVesselRef.orbitSnapShot.ReferenceBodyIndex];
            }

            return null;
        }

        private void RefreshCacheReceiver(int receiverId)
        {
            if (activeReceiverCache.TryGetValue(receiverId, out var receiverModule) &&
                receiverModule != null)
            {
                inactiveReceiverCache.Remove(receiverId);
                return;
            }
            if (FlightGlobals.FindPartByID(receiverFlightIds[receiverId]) is Part receiverPart &&
                receiverPart.FindModulesImplementing<IURT_Receiver>()
                .Find(s => s.ReceiverId == receiverId) is IURT_Receiver receiverFoundModule)
            {
                activeReceiverCache[receiverId] = receiverFoundModule;
                inactiveReceiverCache.Remove(receiverId);
                return;
            }
            activeReceiverCache.Remove(receiverId);

            if (inactiveReceiverCache.TryGetValue(receiverId, out var receiverProtoPart) &&
                receiverProtoPart != null &&
                receiverProtoPart.modules.Find(s => URT_AssemblyScanner.CompatibleReceivers.Contains(s.moduleName) &&
                s.moduleValues.GetInt("receiverId") == receiverId) != null)
            {
                return;
            }
            if (FlightGlobals.FindProtoPartByID(receiverFlightIds[receiverId]) is ProtoPartSnapshot receiverFoundProtoPart &&
                receiverFoundProtoPart.modules.Find(s => URT_AssemblyScanner.CompatibleReceivers.Contains(s.moduleName) &&
                s.moduleValues.GetInt("receiverId") == receiverId) != null)
            {
                inactiveReceiverCache[receiverId] = receiverFoundProtoPart;
                return;
            }
            inactiveReceiverCache.Remove(receiverId);
        }
        private void RefreshCacheTransmitter(int transmitterId)
        {
            if (activeTransmitterCache.TryGetValue(transmitterId, out var transmitterModule) && transmitterModule != null)
            {
                inactiveTransmitterCache.Remove(transmitterId);
                return;
            }

            if (FlightGlobals.FindPartByID(transmitterFlightIds[transmitterId]) is Part transmitterPart &&
                transmitterPart.FindModulesImplementing<IURT_Transmitter>()
                .Find(s => s.TransmitterID == transmitterId) is IURT_Transmitter transmitterFoundModule
                )
            {
                activeTransmitterCache[transmitterId] = transmitterFoundModule;
                inactiveTransmitterCache.Remove(transmitterId);
                return;
            }
            activeTransmitterCache.Remove(transmitterId);

            if (inactiveTransmitterCache.TryGetValue(transmitterId, out var transmitterProtoPart) &&
                transmitterProtoPart != null &&
                transmitterProtoPart.modules.Find(s =>
                URT_AssemblyScanner.CompatibleTransmitters.Contains(s.moduleName) &&
                s.moduleValues.GetInt("transmitterID") == transmitterId
                ) != null)
            {
                inactiveTransmitterCache[transmitterId] = transmitterProtoPart;
                return;
            }
            if (FlightGlobals.FindProtoPartByID(transmitterFlightIds[transmitterId]) is ProtoPartSnapshot transmitterFoundProtoPart &&
                transmitterFoundProtoPart.modules.Find(s =>
                URT_AssemblyScanner.CompatibleTransmitters.Contains(s.moduleName) &&
                s.moduleValues.GetInt("transmitterID") == transmitterId
                ) != null)
            {
                inactiveTransmitterCache[transmitterId] = transmitterFoundProtoPart;
                return;
            }
            inactiveTransmitterCache.Remove(transmitterId);
        }
        //Below written by AI
        #if DEBUG
        public void DebugDumpRegistryState()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[URT] ==================== REGISTRY DEBUG DUMP ====================");

            sb.AppendLine($"[URT] --- TRANSMITTERS ({transmitterFlightIds.Count} Registered) ---");
            foreach (KeyValuePair<int, uint> kvp in transmitterFlightIds)
            {
                int tId = kvp.Key;
                uint fId = kvp.Value;
                double maxPower = transmitterCurrentMaxAmounts.TryGetValue(tId, out double maxVal) ? maxVal : 0.0;
                double sentPower = transmitterTransmittedAmounts.TryGetValue(tId, out double sentVal) ? sentVal : 0.0;
                // Retrieve transmitterTransmittedPowers value
                double sentPowerWatts = transmitterTransmittedAmounts.TryGetValue(tId, out double sentPowVal) ? sentPowVal : 0.0;

                string cacheState = "Unloaded";
                if (activeTransmitterCache.TryGetValue(tId, out IURT_Transmitter module) && module != null)
                {
                    cacheState = "Active/Loaded";
                }
                else if (inactiveTransmitterCache.ContainsKey(tId))
                {
                    cacheState = "Cached/Unloaded";
                }

                sb.AppendLine($"  ID: {tId} | FlightID: {fId} | State: {cacheState} | MaxPower: {maxPower:F2} | Output: {sentPower:F2} | OutputPower: {sentPowerWatts:F2}");
            }

            sb.AppendLine($"[URT] --- RECEIVERS ({receiverFlightIds.Count} Registered) ---");
            foreach (KeyValuePair<int, uint> kvp in receiverFlightIds)
            {
                int rId = kvp.Key;
                uint fId = kvp.Value;
                double requested = receiverRequestedAmounts.TryGetValue(rId, out double reqVal) ? reqVal : 0.0;
                double received = receiverReceivedAmounts.TryGetValue(rId, out double recVal) ? recVal : 0.0;
                // Retrieve receiverReceivedPowers value
                double receivedPowerWatts = receiverReceivedAmounts.TryGetValue(rId, out double recPowVal) ? recPowVal : 0.0;

                string cacheState = "Unloaded";
                if (activeReceiverCache.TryGetValue(rId, out IURT_Receiver module) && module != null)
                {
                    cacheState = "Active/Loaded";
                }
                else if (inactiveReceiverCache.ContainsKey(rId))
                {
                    cacheState = "Cached/Unloaded";
                }

                sb.AppendLine($"  ID: {rId} | FlightID: {fId} | State: {cacheState} | Requested: {requested:F2} | Input: {received:F2} | InputPower: {receivedPowerWatts:F2}");
            }

            sb.AppendLine($"[URT] --- MANUAL TARGET PAIRINGS ({manualTransmittersToTargets.Count} Active) ---");
            foreach (KeyValuePair<int, int> kvp in manualTransmittersToTargets)
            {
                sb.AppendLine($"  Transmitter ID: {kvp.Key} ===> Targeted Receiver ID: {kvp.Value}");
            }

            sb.AppendLine($"[URT] --- ACTIVE VESSEL RESERVED POOL ({reservedForActiveVesselTransmitters.Count} Transmitters) ---");
            foreach (int tId in reservedForActiveVesselTransmitters)
            {
                sb.AppendLine($"  Transmitter ID: {tId} is reserved for Active Vessel");
            }

            sb.AppendLine($"[URT] --- GLOBAL LINKS DATABASE ({Links.Count} Known Paths) ---");
            foreach (URT_Link link in Links)
            {
                sb.AppendLine($"  Tx {link.TransmitterId} -> Rx {link.ReceiverId} | StaticEff: {link.ConstantLinkFactor:F4} | MaxSqrDist: {link.MaxDistanceSquared:F1}");
            }
            sb.AppendLine($"[URT] --- ACTIVE LINKS DATABASE ({ActiveLinks.Count} Active Links) ---");
            foreach (var link in ActiveLinks)
            {
                sb.AppendLine($"  Tx {link.Link.TransmitterId} -> Rx {link.Link.ReceiverId} | ReceivedPower: {link.ReceivedPower.ToString():F4}");
            }

            sb.AppendLine("[URT] =============================================================");
            Debug.Log(sb.ToString());
        }
        #endif
    }

}
