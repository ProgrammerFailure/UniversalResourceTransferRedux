using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UniversalResourceTransferRedux.RegistryComponents;
using static UniversalResourceTransferRedux.GenericUtils;

namespace UniversalResourceTransferRedux
{

    public class URT_Transmitter : PartModule
    {
        //Part properties
        [KSPField(isPersistant = true, guiActive = false)]
        public int transmitterID = -1;

        [KSPField(isPersistant = false, guiActive = false)]
        private float maxTransmittedPower;

        [KSPField(isPersistant = false, guiActive = false)]
        private float transmitterArea;

        [KSPField(isPersistant = true, guiActive = false)]
        private float transmitterWavelength;

        [KSPField(isPersistant = false, guiActive = false)]
        private float transmitterEfficiency;

        [KSPField(isPersistant = false, guiActive = false)]
        private string inputResourceName = "ElectricCharge";

        [KSPField(isPersistant = false, guiActive = false)]
        private float inputResourceEnergyFactor = 1.0f;

        [KSPField(isPersistant = false, guiActive = false)]
        private string inputResourceGuiUnits = "EC/s";

        [KSPField(isPersistant = false, guiActive = false)]
        private float buildQuality = 1.0f; // Default to 100% quality

        //Dynamic properties

        [KSPField(isPersistant = true,
            guiActive = true,
            groupDisplayName = "Universal Resource Transmitter",
            groupName = "URT_transmitter_gui",
            guiName = "Transmitted Power",
            guiFormat ="F2",
            guiUnits = "EC/s")]
        public float transmittedPowerGui;

        [KSPField(isPersistant = true, guiActive = true, groupDisplayName = "Universal Resource Transmitter", groupName = "URT_transmitter_gui",guiName = "Transmission Active"), UI_Toggle(enabledText = "Transmitting", disabledText = "Transmission Disabled")]
        //TODO: Add callback later for when transmitting is turned on or off
        private bool isTransmitting = false;

        [KSPField(isPersistant = false, guiActive = false)]
        private float transmittedPower;

        private int inputResourceHash;

        private ReceiverInfo? targetReceiverInfo;
        private UniversalResourceTransferRedux.RegistryComponents.URT_Registry registry;
        private int targetId = -1;


        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor) { return; }
            registry = URT_Registry.Instance;
            if (registry == null)
            {
                Debug.Log("[URT_Transmitter] URT_Registry module not found.");
                isEnabled = false;
                moduleIsEnabled = false;
                isTransmitting = false;
                inputResourceHash = 0;
                return;
            }
            var resourceDef = PartResourceLibrary.Instance.GetDefinition(inputResourceName);
            if (resourceDef == null)
            {
                Debug.LogError($"{ClassName} with transmitterId ({transmitterID}) unable to initialize: invalid InputResource {inputResourceName}.");
                isEnabled = false;
                moduleIsEnabled = false;
                isTransmitting = false;
                inputResourceHash = 0;
                return;
            }
            inputResourceHash = resourceDef.id;
            if (transmitterID == -1) // If uninitiailized
            {
                transmitterID = registry.registerNewTransmitterId(this.part.flightID);
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
            
        }

        public override void OnFixedUpdate()
        {
            if (!isTransmitting)
            {
                return;
            }
            double vesselCurrentResourceAmount;
            this.vessel.GetConnectedResourceTotals(inputResourceHash, out vesselCurrentResourceAmount, out double vesselCurrentResourceMaxAmount);
            if (vesselCurrentResourceAmount < transmittedPowerGui * Time.fixedDeltaTime)
            {
                isTransmitting = false;
                return;
            }
            this.vessel.RequestResource(this.part, inputResourceHash, transmittedPowerGui, true);
            transmittedPower = transmittedPowerGui * inputResourceEnergyFactor;
        }

        public void OnDestroy()
        {
            if (registry != null)
            {
                registry.deregisterActiveTransmitter(transmitterID);
            }
        }

        
        //To be a KSPEvent
        public void SetTarget(int receiverId)
        {
            
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
        private IEnumerator RefreshTargetReceiverInfo()
        {
            yield return new WaitForSeconds(0.5f);
            // This is an infinite loop that runs for the lifetime of the module.
            while (true)
            {
                // Initial wait to allow game world to initialize

                var receiverInfo = registry.GetReceiverInfo(targetId, this.ClassName);
                if (receiverInfo.HasValue)
                {
                    targetReceiverInfo = receiverInfo.Value;
                    isTransmitting = true;
                }
                else
                {
                    isTransmitting = false; // Could also set isTransmitting = false;
                    transmittedPower = 0;
                }

                // Wait for 30 seconds before the next refresh cycle.
                yield return new WaitForSeconds(30f);
            }
        }



        #endregion
    }
}
