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

        private double time;
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
                    #if DEBUG
                    Debug.Log("[URT]: UniversalResourceTransferRedux.Registry.Instance has been set!");
                    #endif
                }
                else if (Instance != this)
                {
                    Destroy(this);
                }
            }
            GameEvents.onPartDie.Add(OnPartDie);
            GameEvents.onVesselChange.Add(OnActiveVesselChanged);
            GameEvents.onVesselUnloaded.Add(OnVesselUnloaded);
            GameEvents.onVesselLoaded.Add(OnVesselLoaded);
            GameEvents.OnVesselRecoveryRequested.Add(OnVesselDestroyedOrRecovered);
            GameEvents.onVesselWillDestroy.Add(OnVesselDestroyedOrRecovered);
            
            StartCoroutine(RunNetworkRebuild());
        }


        public void OnDisable()
        {
            GameEvents.onPartDie.Remove(OnPartDie);
            GameEvents.onVesselChange.Remove(OnActiveVesselChanged);
            GameEvents.onVesselUnloaded.Remove(OnVesselUnloaded);
            GameEvents.onVesselLoaded.Remove(OnVesselLoaded);
            GameEvents.OnVesselRecoveryRequested.Remove(OnVesselDestroyedOrRecovered);
            GameEvents.onVesselWillDestroy.Remove(OnVesselDestroyedOrRecovered);
        }
        public void FixedUpdate()
        {
            time = Planetarium.GetUniversalTime();
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
