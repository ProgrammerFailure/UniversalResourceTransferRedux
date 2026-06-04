using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniversalResourceTransferRedux.Core.RegistryComponents
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.SPACECENTER)]
    internal partial class URT_Registry : ScenarioModule
    {
        //Permanent dictionaries
        private Dictionary<int, uint> transmitterFlightIds = new();
        private Dictionary<int, uint> receiverFlightIds = new();
        private Dictionary<int, int> transmitterModuleIds = new();
        private Dictionary<int, int> receiverModuleIds = new();
        //Dynamic dictionaries
        public Dictionary<int, double> transmitterTransmittedAmounts = new();
        public Dictionary<int, double> receiverReceivedAmounts = new();

        private Dictionary<int, double> transmitterCurrentMaxAmounts = new();
        private Dictionary<int, double> receiverRequestedAmounts = new();
        private Dictionary<int, int> manualTransmittersToTargets = new();
        private List<int> reservedForActiveVesselTransmitters = new();
        private List<int> receiversOnActiveVessel = new();

        //Active PartModule and ProtoPartSnapshot caches
        private Dictionary<int, URT_Transmitter> activeTransmitterCache = new();
        private Dictionary<int, URT_Receiver> activeReceiverCache = new();
        private Dictionary<int, ProtoPartSnapshot> inactiveTransmitterCache = new();
        private Dictionary<int, ProtoPartSnapshot> inactiveReceiverCache = new();

        private List<URT_Link> Links = new();
        private List<(URT_Link, double)> ActiveLinks = new(); //Link, receivedPower
        private List<Action> listeners = new();
        private Dictionary<int, double> receiversWorkingDict = new();

        //Instance
        public static RegistryComponents.URT_Registry Instance = null;

        [KSPField(isPersistant = true)]
        private int nextTransmitterInt;

        [KSPField(isPersistant = true)]
        private int nextReceiverId;

        public void Start()
        {
            if (Instance == null)
            {
                if (Instance == null)
                {
                    Instance = this;
                    Debug.Log("[URT]: UniversalResourceTransferRedux.Registry.Instance has been set!");
                }
                else if (Instance != this)
                {
                    Destroy(this);
                }
            }
            GameEvents.onPartDestroyed.Add(OnPartDestroyed);
            GameEvents.onVesselChange.Add(OnActiveVesselChanged);
            GameEvents.onVesselUnloaded.Add(OnVesselLoadedOrUnloaded);
            GameEvents.onVesselLoaded.Add(OnVesselLoadedOrUnloaded);
            GameEvents.OnVesselRecoveryRequested.Add(OnVesselDestroyedOrRecovered);
            GameEvents.onVesselDestroy.Add(OnVesselDestroyedOrRecovered);
            
            StartCoroutine(RunNetworkRebuild());
        }

        public void FixedUpdate()
        {
            URT_PowerCalculator.ProcessLinks(ActiveLinks,
                transmitterCurrentMaxAmounts,
                receiverReceivedAmounts,
                transmitterTransmittedAmounts,
                receiversWorkingDict,
                receiverFlightIds.Keys,
                transmitterFlightIds.Keys);
        }
        private System.Collections.IEnumerator RunNetworkRebuild()
        {
            RebuildLinks();
            yield return new WaitForSeconds(60);
        }
        public void OnDisable()
        {
            GameEvents.onPartDestroyed.Remove(OnPartDestroyed);
            GameEvents.onVesselChange.Remove(OnActiveVesselChanged);
            GameEvents.onVesselUnloaded.Remove(OnVesselLoadedOrUnloaded);
            GameEvents.onVesselLoaded.Remove(OnVesselLoadedOrUnloaded);
            GameEvents.OnVesselRecoveryRequested.Remove(OnVesselDestroyedOrRecovered);
            GameEvents.onVesselDestroy.Remove(OnVesselDestroyedOrRecovered);
        }
        private void OnPartDestroyed(Part p)
        {
            bool needRebuild = false;
            var tempManualTransmitters = manualTransmittersToTargets;
            var linksTemp = Links.ToArray();
            foreach (var receiverModule in p.FindModulesImplementing<URT_Receiver>())
            {
                needRebuild = true;
                receiverFlightIds.Remove(receiverModule.receiverId);
                activeReceiverCache.Remove(receiverModule.receiverId);
                inactiveReceiverCache.Remove(receiverModule.receiverId);
                receiversOnActiveVessel.Remove(receiverModule.receiverId);
                receiverReceivedAmounts.Remove(receiverModule.receiverId);
                receiverRequestedAmounts.Remove(receiverModule.receiverId);
                foreach (var kvp in tempManualTransmitters)
                {
                    if (kvp.Value == receiverModule.receiverId) manualTransmittersToTargets.Remove(kvp.Key);
                }

                foreach (var link in linksTemp)
                {
                    if (link.ReceiverId == receiverModule.receiverId) Links.Remove(link);
                }
            }
            foreach (var transmitterModule in p.FindModulesImplementing<URT_Transmitter>())
            {
                needRebuild = true;
                transmitterFlightIds.Remove(transmitterModule.transmitterID);
                activeTransmitterCache.Remove(transmitterModule.transmitterID);
                inactiveTransmitterCache.Remove(transmitterModule.transmitterID);
                reservedForActiveVesselTransmitters.Remove(transmitterModule.transmitterID);
                transmitterTransmittedAmounts.Remove(transmitterModule.transmitterID);
                transmitterCurrentMaxAmounts.Remove(transmitterModule.transmitterID);
                foreach (var kvp in tempManualTransmitters)
                {
                    if (kvp.Key == transmitterModule.transmitterID) manualTransmittersToTargets.Remove(transmitterModule.transmitterID);
                }
                foreach (var link in linksTemp)
                {
                    if (link.TransmitterId == transmitterModule.transmitterID) Links.Remove(link);
                }
            }
            if (needRebuild) RebuildLinks();
        }
        private void OnActiveVesselChanged(Vessel newActiveVessel)
        {
            receiversOnActiveVessel.Clear();
            foreach (var receiverModuleList in newActiveVessel.parts.Select(s => s.FindModulesImplementing<URT_Receiver>()))
            {
                foreach (var receiverModule in receiverModuleList)
                {
                    receiversOnActiveVessel.Add(receiverModule.receiverId);
                }
            }
            RebuildLinks();
        }

        private void OnVesselLoadedOrUnloaded(Vessel loadedOrUnloadedVessel)
        {
            foreach (var receiverModuleList in loadedOrUnloadedVessel.parts.Select(s => s.FindModulesImplementing<URT_Receiver>()))
            {
                foreach (var receiverModule in receiverModuleList)
                {
                    activeReceiverCache[receiverModule.receiverId] = receiverModule;
                    inactiveReceiverCache.Remove(receiverModule.receiverId);
                }
            }
            foreach (var transmitterModuleList in loadedOrUnloadedVessel.parts.Select(s => s.FindModulesImplementing<URT_Transmitter>()))
            {
                foreach (var transmitterModule in transmitterModuleList)
                {
                    activeTransmitterCache[transmitterModule.transmitterID] = transmitterModule;
                    inactiveTransmitterCache.Remove(transmitterModule.transmitterID);
                }
            }
        }

        private void OnVesselDestroyedOrRecovered(Vessel v)
        {
            bool needRebuild = false;
            var tempManualTransmitters = manualTransmittersToTargets;
            var linksTemp = Links.ToArray();
            foreach (var p in v.parts)
            {
                foreach (var receiverModule in p.FindModulesImplementing<URT_Receiver>())
                {
                    needRebuild = true;
                    receiverFlightIds.Remove(receiverModule.receiverId);
                    activeReceiverCache.Remove(receiverModule.receiverId);
                    inactiveReceiverCache.Remove(receiverModule.receiverId);
                    receiversOnActiveVessel.Remove(receiverModule.receiverId);
                    receiverReceivedAmounts.Remove(receiverModule.receiverId);
                    receiverRequestedAmounts.Remove(receiverModule.receiverId);
                    foreach (var kvp in tempManualTransmitters)
                    {
                        if (kvp.Value == receiverModule.receiverId) manualTransmittersToTargets.Remove(kvp.Key);
                    }

                    foreach (var link in linksTemp)
                    {
                        if (link.ReceiverId == receiverModule.receiverId) Links.Remove(link);
                    }
                }
                foreach (var transmitterModule in p.FindModulesImplementing<URT_Transmitter>())
                {
                    needRebuild = true;
                    transmitterFlightIds.Remove(transmitterModule.transmitterID);
                    activeTransmitterCache.Remove(transmitterModule.transmitterID);
                    inactiveTransmitterCache.Remove(transmitterModule.transmitterID);
                    reservedForActiveVesselTransmitters.Remove(transmitterModule.transmitterID);
                    transmitterTransmittedAmounts.Remove(transmitterModule.transmitterID);
                    transmitterCurrentMaxAmounts.Remove(transmitterModule.transmitterID);
                    foreach (var kvp in tempManualTransmitters)
                    {
                        if (kvp.Key == transmitterModule.transmitterID) manualTransmittersToTargets.Remove(transmitterModule.transmitterID);
                    }
                    foreach (var link in linksTemp)
                    {
                        if (link.TransmitterId == transmitterModule.transmitterID) Links.Remove(link);
                    }
                }
            }

            
            if (needRebuild) RebuildLinks();
        }
    }
    internal class URT_Link
    {
        public int TransmitterId { get; }
        public int ReceiverId { get; }

        public double ConstantLinkFactor;

        public double MaxDistanceSquared;

        public double MaxEfficiencyLimit;



        public URT_Link(int transmitterId, int receiverId, double constantLinkFactor, double maxDistanceSquared, double maxEfficiencyLimit)
        {
            TransmitterId = transmitterId;
            ReceiverId = receiverId;
            ConstantLinkFactor = constantLinkFactor;
            MaxDistanceSquared = maxDistanceSquared;
            MaxEfficiencyLimit = maxEfficiencyLimit;
        }
    }
}
