using KSP.Localization;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UniversalResourceTransferRedux.Core.RegistryComponents;
using static UniversalResourceTransferRedux.Core.GenericUtils;

namespace UniversalResourceTransferRedux.Core
{
    public class URT_Transmitter : PartModule, IURT_Transmitter
    {
        

        //Interface members
        public int ModuleID => transmitterModuleId;
        public int TransmitterID => transmitterID;
        public Vessel Vessel => vessel;

        //Part properties
        [KSPField(isPersistant = true, guiActive = true)]
        protected int transmitterID = -1;

        [KSPField(isPersistant = true, guiActive = true)]
        protected int transmitterModuleId;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        protected float maxTransmittedPower = 10000;

        [KSPField(isPersistant = false, guiActive = false)]
        protected float transmitterDiameter;

        [KSPField(isPersistant = true, guiActive = false)]
        protected float transmitterWavelength;

        [KSPField(isPersistant = false, guiActive = false)]
        protected float transmitterEfficiency;

        [KSPField(isPersistant = false, guiActive = false)]
        protected string inputResource = "ElectricCharge";

        // This is defined as "how many EC is one unit of inputResource worth"
        [KSPField(isPersistant = false, guiActive = false)]
        protected float inputResourceEnergyFactor = 1.0f;

        [KSPField(isPersistant = false, guiActive = false)]
        protected string resourceTypeTags = "EMRadiation";

        [KSPField(isPersistant = false, guiActive = false)]
        protected float diffractionConstant = 1.22F;

        [KSPField(isPersistant = true, guiActive = false)]
        protected double lastUpdateTime;

        //Variables
        private URT_Registry registry;
        private int inputResourceHash;
        private double currentTransmittedAmount;

        // User set variables
        [KSPField(isPersistant = true, guiActive = true, guiName = "Transmitting")]
        [UI_Toggle(affectSymCounterparts = UI_Scene.Editor, enabledText = "Transmission Active", disabledText = "Transmission Disabled")]
        protected bool isTransmitting;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Max transmitted power")]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.Editor, minValue = 0, maxValue = 1000, stepIncrement = 1)]
        protected float maxTransmittedPowerGUI;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Reserve for active vessel")]
        [UI_Toggle(affectSymCounterparts = UI_Scene.Editor, enabledText = "Reserved for active vessel", disabledText = "Available for all receivers")]
        protected bool isReservedForActive;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Manual targeting?")]
        [UI_Toggle(affectSymCounterparts = UI_Scene.Editor, enabledText = "Manually targetting", disabledText = "Automatically targetting")]
        protected bool isManuallyTargeting;

        //
        //      [KSPField(isPersistant = true, guiActive = true, guiName = "Manual transmission target")]
        //    [UI_Cycle(affectSymCounterparts = UI_Scene.Editor, stateNames = new string[1] { "No valid receivers" })]
        //  private int manualtargetReceiverId;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight) return;
            StartCoroutine(WaitForRegistry());
        }

        //Core loop stuff
        private IEnumerator WaitForRegistry()
        {
            while (URT_Registry.Instance == null)
            {
#if DEBUG
                Debug.Log("[URT]: URT_Transmitter: Waiting for URT Registry to be up.");
#endif
                yield return null;
            }
            registry = URT_Registry.Instance;
            registry.RegisterListener(OnReceiverRegistered);
            OnReceiverRegistered();
            InitTransmitter();
        }

        private void InitTransmitter()
        {
            if (!HighLogic.LoadedSceneIsFlight) { return; }
            if (transmitterID == -1)
            {
                try
                {
                    transmitterID = registry.RegisterNewTransmitter(this.part.flightID, this, GetTransmitterInfo(), transmitterModuleId);
                }
                catch (InvalidOperationException ex)
                {
                    Debug.LogError($"[URT]: Could not register transmitter! Exception: {ex}");
                    isTransmitting = false;
                    this.moduleIsEnabled = false;
                    return;
                }
            }
            var resourceDef = PartResourceLibrary.Instance.GetDefinition(inputResource);
            if (resourceDef == null)
            {
                Debug.LogError($"[URT]: Could not resolve transmitter input resource! Resource name: {inputResource}, transmitterId: {transmitterID}");
                isTransmitting = false;
                this.moduleIsEnabled = false;
                return;
            }
            inputResourceHash = resourceDef.id;
            registry.RegisterActiveTransmitter(transmitterID, this);

            Fields["isTransmitting"].uiControlFlight.onFieldChanged = OnTransmissionStateChanged;
            Fields["maxTransmittedPowerGUI"].uiControlFlight.onFieldChanged = OnTransmissionStateChanged;
            Fields["isReservedForActive"].uiControlFlight.onFieldChanged = new Callback<BaseField, object>(
                (field, obj) =>
                {
                    if (isReservedForActive)
                    {
                        registry.DeregisterManualTransmitter(transmitterID);
                        registry.RegisterReservedForActiveVesselTransmitter(transmitterID);
                    }
                    else
                    {
                        registry.DeregisterReservedForActiveVesselTransmitter(transmitterID);
                    }
                });
            Fields["isManuallyTargeting"].uiControlFlight.onFieldChanged = OnManualTransmissionStateChanged;

            // Fields["manualtargetReceiverId"].uiControlFlight.onFieldChanged = Fields["isManuallyTargeting"].uiControlFlight.onFieldChanged;
            (Fields["maxTransmittedPowerGUI"].uiControlFlight as UI_FloatRange).maxValue = maxTransmittedPower * inputResourceEnergyFactor;
            (Fields["maxTransmittedPowerGUI"].uiControlFlight as UI_FloatRange).stepIncrement = maxTransmittedPower * inputResourceEnergyFactor / 100;
            Fields["maxTransmittedPowerGUI"].guiUnits = resourceDef.abbreviation + "/s";

            OnTransmissionStateChanged(null, null);
#if DEBUG
            Debug.Log($"[URT]: Transmitter with ID {transmitterID} fully initialized and ready!");
#endif
        }

        public void FixedUpdate()
        {
            if (registry == null || !isTransmitting || maxTransmittedPowerGUI == 0) return;
            vessel.GetConnectedResourceTotals(inputResourceHash,
                out double vesselCurrentResourceAmount,
                out double vesselCurrentResourceMaxAmount
            );
            if (vesselCurrentResourceMaxAmount <= 0) return; //Teardown frame protection
            var currentTime = Planetarium.GetUniversalTime();
            if (lastUpdateTime == 0)
            {
                lastUpdateTime = currentTime - (1.0 / 60.0); //Assume one frame, just for convenience
            }
            var deltaTime = currentTime - lastUpdateTime;
            lastUpdateTime = currentTime;

            currentTransmittedAmount = registry.transmitterTransmittedAmounts[transmitterID];
            var requiredAmount = (currentTransmittedAmount / inputResourceEnergyFactor) * deltaTime;

            
            if (vesselCurrentResourceAmount < requiredAmount)
            {
                isTransmitting = false;
#if DEBUG
                Debug.Log($"[URT] Transmitter with ID {transmitterID}: Not enough power!");
#endif
                OnTransmissionStateChanged(null, null);
                return;
            }
            part.RequestResource(inputResourceHash, requiredAmount);
        }
#if DEBUG
        //User functions
        [KSPEvent(active = true, guiActive = true, guiActiveEditor = false, guiName = "Debug dump")]
        private void DebugDump()
        {
            registry.DebugDumpRegistryState();
        }

        [KSPEvent(active = true, guiActive = true, guiActiveEditor = false, guiName = "Run network rebuild")]
        private void RunNetworkRebuild()
        {
            registry.RebuildLinks();
        }
#endif

        public TransmitterInfo GetTransmitterInfo()
        {
            return new TransmitterInfo(
                transmitterDiameter,
                transmitterWavelength,
                transmitterEfficiency,
                maxTransmittedPower,
                resourceTypeTags.Split(';'),
                diffractionConstant
            );
        }

        private void OnTransmissionStateChanged(BaseField field, object obj)
        {
            if (!isTransmitting)
            {
                registry.OnTransmitterMaxPowerChanged(transmitterID, 0);
                return;
            }
            registry.OnTransmitterMaxPowerChanged(transmitterID, maxTransmittedPowerGUI * inputResourceEnergyFactor);
        }

        private void OnReceiverRegistered()
        {
            // (Fields["manualtargetReceiverId"].uiControlFlight as UI_Cycle).stateNames = registry.GetReceiverIDs().Select(s => s.ToString()).ToArray();
        }

        private void OnManualTransmissionStateChanged(BaseField field, object obj)
        {
            if (isManuallyTargeting)
            {
                registry.DeregisterReservedForActiveVesselTransmitter(transmitterID);
                registry.RegisterManualTransmitter(transmitterID, 0 /* Replace with manual transmitter thing once that's figured out*/);
            }
            else
            {
                registry.DeregisterManualTransmitter(transmitterID);
            }
        }
    }
}
