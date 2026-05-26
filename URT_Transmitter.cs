using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Diagnostics;
using UniversalResourceTransferRedux.RegistryComponents;
using static UniversalResourceTransferRedux.GenericUtils;

namespace UniversalResourceTransferRedux
{
    public class URT_Transmitter : PartModule
    {
        //Part properties
        [KSPField(isPersistant = true, guiActive = true)]
        public int transmitterID = -1;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        public float maxTransmittedPower = 10000;

        [KSPField(isPersistant = false, guiActive = false)]
        public float transmitterArea;

        [KSPField(isPersistant = true, guiActive = false)]
        public float transmitterWavelength;

        [KSPField(isPersistant = false, guiActive = false)]
        public float transmitterEfficiency;

        [KSPField(isPersistant = false, guiActive = false)]
        public string inputResourceName = "ElectricCharge";

        [KSPField(isPersistant = false, guiActive = false)]
        public float inputResourceEnergyFactor = 1.0f;

        [KSPField(isPersistant = false, guiActive = false)]
        public string inputResourceGuiUnits = "EC/s";

        [KSPField(isPersistant = false, guiActive = false)]
        public float buildQuality = 1.0f; // Default to 100% quality

        //Dynamic properties

        [KSPField(isPersistant = true,
            guiActive = true,
            groupDisplayName = "Universal Resource Transmitter",
            groupName = "URT_transmitter_gui",
            guiName = "Transmitted Power",
            guiFormat = "F2",
            guiUnits = "EC/s"), UI_FloatRange(minValue = 0, maxValue = 1000, stepIncrement = 10)]
        public float transmittedPowerGui;

        [KSPField(isPersistant = true, guiActive = true, groupDisplayName = "Universal Resource Transmitter", groupName = "URT_transmitter_gui", guiName = "Transmission Active"), UI_Toggle(enabledText = "Transmitting", disabledText = "Transmission Disabled")]
        //TODO: Add callback later for when transmitting is turned on or off
        private bool isTransmitting = false;

        [KSPField(isPersistant = true, guiActive = true)]
        private float transmittedPower;

        [KSPField(isPersistant = true, guiActive = false)]
        private double lastUpdateTime;

        [KSPField(isPersistant = true, guiActive = false)]
        public int targetId = -1;

        private int inputResourceHash;

        private ReceiverInfo? targetReceiverInfo;
        private UniversalResourceTransferRedux.RegistryComponents.URT_Registry registry;



        public override void OnStart(StartState state)
        {
            Debug.Log($"[URT]: Transmitter module spawned; Scene: {HighLogic.LoadedScene.ToString()}");
            if (!HighLogic.LoadedSceneIsFlight)
            {
                Debug.Log("[URT]: Scene not flight! Quitting.");
                return;
            }
            StartCoroutine(WaitForRegistry());
        }
        private IEnumerator WaitForRegistry()
        {
            while (URT_Registry.Instance == null)
            {
                Debug.Log("[URT]: Waiting for URT Registry to be up.");
                yield return null;
            }
            InitTransmitter();
        }
        private void InitTransmitter()
        {
            if (!HighLogic.LoadedSceneIsFlight) { return; }
            registry = URT_Registry.Instance;
            if (registry == null)
            {
                Debug.LogError("[URT_Transmitter] CRITICAL. URT_Registry module not found.");
                isTransmitting = false;
                inputResourceHash = 0;
                return;
            }
            var resourceDef = PartResourceLibrary.Instance.GetDefinition(inputResourceName);
            if (resourceDef == null)
            {
                Debug.LogError($"{ClassName} with transmitterId ({transmitterID}) unable to initialize: invalid InputResource {inputResourceName}.");
                isTransmitting = false;
                inputResourceHash = 0;
                return;
            }
            inputResourceHash = resourceDef.id;
            if (transmitterID == -1) // If uninitiailized
            {
                transmitterID = registry.registerNewTransmitterId(this.part.flightID);
                Debug.Log("[URT]: Transmitter registered with registry!");
            }

            if (targetId != -1) //If target exists
            {
                StartCoroutine(RefreshTargetReceiverInfo());
            }
            else
            {
                isTransmitting = false;
            }
            registry.registerActiveTransmitter(transmitterID, this);
            Fields["transmittedPowerGui"].guiUnits = inputResourceGuiUnits;
            (Fields["transmittedPowerGui"].uiControlFlight as UI_FloatRange).minValue = 0;
            (Fields["transmittedPowerGui"]?.uiControlFlight as UI_FloatRange).maxValue = maxTransmittedPower;
            (Fields["transmittedPowerGui"]?.uiControlFlight as UI_FloatRange).stepIncrement = maxTransmittedPower / 100;
            Fields["transmittedPowerGui"].uiControlFlight.onFieldChanged = OnPowerChanged;
            Fields["isTransmitting"].uiControlFlight.onFieldChanged = OnTransmissionStateChanged;
            registry.TriggerAllListeners();
            Debug.Log("[URT]: Transmitter fully initialized and ready!");

        }
        public override void OnFixedUpdate()
        {
            transmittedPower = transmittedPowerGui * inputResourceEnergyFactor;
            if (!isTransmitting)
            {
                transmittedPower = 0f;
                return;
            }

            if (lastUpdateTime == 0)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
            }

            var currentTime = Planetarium.GetUniversalTime();
            var deltaTime = currentTime - lastUpdateTime;
            lastUpdateTime = currentTime;

            this.vessel.GetConnectedResourceTotals(inputResourceHash, out double vesselCurrentResourceAmount, out double vesselCurrentResourceMaxAmount);
            double requiredEC = transmittedPowerGui * deltaTime;

            if (vesselCurrentResourceAmount < requiredEC)
            {
                isTransmitting = false;
                transmittedPower = 0f;
                Debug.Log("[URT] Transmitter ran out of ElectricCharge and shut down.");
                return;
            }

            this.vessel.RequestResource(this.part, inputResourceHash, requiredEC, true);
        }

        public void OnDestroy()
        {
            if (registry != null)
            {
                registry.deregisterActiveTransmitter(transmitterID);
            }
        }


        public void OnPowerChanged(BaseField field, object obj)
        {
            transmittedPower = transmittedPowerGui * inputResourceEnergyFactor;
            registry.TriggerAllListeners();
        }

        public void OnTransmissionStateChanged(BaseField field, object obj)
        {
            registry.TriggerAllListeners();
        }
        public void SetTarget(int receiverId)
        {
            targetId = receiverId;
            targetReceiverInfo = registry.GetReceiverInfo(receiverId);
        }

        [KSPEvent(active = true, guiActive = true, guiName = "Check presence in cache", name = "transmitterCacheCheck")]
        public void CheckIfInCache()
        {
            if (registry.activeTransmitterCache.ContainsKey(transmitterID))
            {
                Debug.Log($"[URT]: Transmitter present in active cache. transmittedPower per the registry: {registry.GetTransmitter(transmitterID).Value.Power}");
            }
            else
            {
                Debug.LogError($"[URT]: Transmitter not present in active cache! transmittedPower per the registry: {registry.GetTransmitter(transmitterID).Value.Power}");
            }
        }

        [KSPEvent(active = true, guiActive = true, guiName = "Force recheck receiver cache", name = "resetReceiverCache")]
        private void RecheckReceiverCache()
        {
            var receiverInfo = registry.GetReceiverInfo(targetId);
            if (receiverInfo.HasValue)
            {
                targetReceiverInfo = receiverInfo.Value;
            }
            else
            {
                isTransmitting = false;
            }
        }

        public void ResetLastUpdateTime()
        {
            lastUpdateTime = Planetarium.GetUniversalTime();
            Debug.Log($"[URT]: lastUpdateTime = {lastUpdateTime}, UniversalTime = {Planetarium.GetUniversalTime()}");
        }

        #region Utilities
        public TransmitterInfo GetTransmitterInfo()
        {
            return TransmitterInfo.Create(
                transmitterArea,
                transmitterWavelength,
                transmitterEfficiency,
                this.part.vessel.protoVessel,
                transmittedPower,
                isTransmitting,
                buildQuality
            );
        }

        public void SetTransmitterState(bool isEnabled)
        {
            isTransmitting = isEnabled;
        }
        private IEnumerator RefreshTargetReceiverInfo()
        {
            yield return new WaitForSeconds(0.5f); // Initial wait to allow game world to initialize
            // This is an infinite loop that runs for the lifetime of the module.
            while (true)
            {

                var receiverInfo = registry.GetReceiverInfo(targetId);
                if (receiverInfo.HasValue)
                {
                    targetReceiverInfo = receiverInfo.Value;
                }
                else
                {
                    isTransmitting = false; 
                }

                // Wait for 30 seconds before the next refresh cycle.
                yield return new WaitForSeconds(30f);
            }
        }



        #endregion
    }
}
