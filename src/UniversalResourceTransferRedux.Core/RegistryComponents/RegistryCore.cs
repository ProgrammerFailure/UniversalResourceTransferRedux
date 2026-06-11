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
        private Dictionary<int, double> transmitterCurrentMaxAmounts = new();
        private Dictionary<int, double> receiverRequestedAmounts = new();
        private Dictionary<int, int> manualTransmittersToTargets = new();
        private List<int> reservedForActiveVesselTransmitters = new();
        private List<URT_Link> Links = new();
        //Dynamic dictionaries
        public Dictionary<int, double> transmitterTransmittedAmounts = new();
        public Dictionary<int, double> receiverReceivedAmounts = new();        
        private List<int> receiversOnActiveVessel = new();

        //Active PartModule and ProtoPartSnapshot caches
        private Dictionary<int, IURT_Transmitter> activeTransmitterCache = new();
        private Dictionary<int, IURT_Receiver> activeReceiverCache = new();
        private Dictionary<int, ProtoPartSnapshot> inactiveTransmitterCache = new();
        private Dictionary<int, ProtoPartSnapshot> inactiveReceiverCache = new();

        //Working collections
        private List<URT_ActiveLink> ActiveLinks = new(); //Link, receivedPower
        private List<Action> listeners = new();
        private Dictionary<int, double> receiversWorkingDict = new();
        public static Dictionary<int, URT_BodyValues> BodySquaredRadiiAndAtmoRadii = new(); 

        //Instance
        public static RegistryComponents.URT_Registry Instance = null;

        //Physics stuff
        public static readonly double RayleighCoefficient = 13.9E-31;
        public static readonly double MieCoefficient = 1.1E-13;

        private double time;
        [KSPField(isPersistant = true)]
        protected int nextTransmitterInt;

        [KSPField(isPersistant = true)]
        protected int nextReceiverId;

        [KSPField(isPersistant = true)]
        protected int lastUpdatedIndex = 0;

        protected int MaxUpdatesPerFrame = 5;

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
            GameEvents.onVesselWasModified.Add(OnVesselWasModified);
            BodySquaredRadiiAndAtmoRadii.Clear();
            foreach (var body in FlightGlobals.Bodies)
            {
                BodySquaredRadiiAndAtmoRadii.Add(body.flightGlobalsIndex, new URT_BodyValues(
                    body.Radius * body.Radius,
                    (body.Radius + body.atmosphereDepth) * (body.Radius + body.atmosphereDepth),
                    (body.sphereOfInfluence * body.sphereOfInfluence),
                    GenericUtils.CalculateBaseScaleHeight(body),
                    body.atmDensityASL
                ));
            }
            StartCoroutine(RunNetworkRebuild());
            StartCoroutine(RunOcclusionCacheManagement());
        }
        public void OnDisable()
        {
            GameEvents.onPartDie.Remove(OnPartDie);
            GameEvents.onVesselChange.Remove(OnActiveVesselChanged);
            GameEvents.onVesselUnloaded.Remove(OnVesselUnloaded);
            GameEvents.onVesselLoaded.Remove(OnVesselLoaded);
            GameEvents.OnVesselRecoveryRequested.Remove(OnVesselDestroyedOrRecovered);
            GameEvents.onVesselWillDestroy.Remove(OnVesselDestroyedOrRecovered);
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
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
            while (true)
            {
                RebuildLinks();
                yield return new WaitForSecondsRealtime(20);
            }
        }
        private System.Collections.IEnumerator RunOcclusionCacheManagement()
        {
            while (true)
            {
                ManageOcclusionCache();
                yield return new WaitForSecondsRealtime(1);
            }
        }


    }
    internal readonly struct URT_Link
    {
        public readonly int TransmitterId;
        public readonly int ReceiverId;

        public readonly double ConstantLinkFactor;

        public readonly double MaxDistanceSquared;

        public readonly double MaxEfficiencyLimit;

        public readonly double AtmosphereAttenuationCoefficient;


        public URT_Link(int transmitterId, int receiverId, double constantLinkFactor, double maxDistanceSquared, double maxEfficiencyLimit, double atmoAttenuationCoeff)
        {
            TransmitterId = transmitterId;
            ReceiverId = receiverId;
            ConstantLinkFactor = constantLinkFactor;
            MaxDistanceSquared = maxDistanceSquared;
            MaxEfficiencyLimit = maxEfficiencyLimit;
            AtmosphereAttenuationCoefficient = atmoAttenuationCoeff;
        }
    }
    internal  struct URT_ActiveLink
    {
        public readonly URT_Link Link;
        public readonly double ReceivedPower;
        public readonly CelestialBody LowestSharedParent;
        public double OcclusionImpact;

        public URT_ActiveLink(URT_Link link, double receivedPower, CelestialBody lowestSharedParent, double occlusionImpact)
        {
            Link = link;
            ReceivedPower = receivedPower;
            LowestSharedParent = lowestSharedParent;
            OcclusionImpact = occlusionImpact;
        }
    }
    internal readonly struct URT_BodyValues
    {
        public readonly double SquaredBodyRadius;
        public readonly double SquaredBodyAtmoTotalRadius;
        public readonly double SquaredSOIRadius;
        public readonly double ScaleHeight;
        public readonly double ASLDensity;
        public URT_BodyValues(double sqrRadius, double sqrAtmoRadius, double sqrSoiRadius, double scaleHeight, double aslDensity)
        {
            SquaredBodyRadius = sqrRadius;
            SquaredBodyAtmoTotalRadius = sqrAtmoRadius;
            SquaredSOIRadius = sqrSoiRadius;
            ScaleHeight = scaleHeight;
            ASLDensity = aslDensity;
        }
    }

    internal readonly struct URT_LinkToProcess
    {
        public readonly URT_Link Link;
        public readonly double TheoreticalEfficiency;
        public readonly Vector3d TransmitterPosition;
        public readonly Vector3d ReceiverPosition;
        public URT_LinkToProcess(URT_Link link, double theoreticalEff, Vector3d transmitterPos, Vector3d receiverPos)
        {
            Link = link;
            TheoreticalEfficiency = theoreticalEff;
            TransmitterPosition = transmitterPos;
            ReceiverPosition = receiverPos;
        }
    }
}
