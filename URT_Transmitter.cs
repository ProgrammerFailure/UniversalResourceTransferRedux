using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        //Dynamic properties

        [KSPField(isPersistant = true, guiActive = true, groupDisplayName = "Universal Resource Transmitter", groupName = "URT_transmitter_gui", guiName = "Transmitted Power"), UI_FloatRange(minValue = 0, maxValue = 0)]
        //remember to set maxValue to the actual part's max value later on
        public float transmittedPower;

        [KSPField(isPersistant = true, guiActive = true, groupDisplayName = "Universal Resource Transmitter", groupName = "URT_transmitter_gui",guiName = "Transmission Active"), UI_Toggle(enabledText = "Transmitting", disabledText = "Transmission Disabled")]
        //TODO: Add callback later for when transmitting is turned on or off
        private bool isTransmitting = false;

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
            }

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
                isTransmitting
            );
        }
        private IEnumerator RefreshTargetReceiverInfo() // Changed return type to IEnumerator
        {
            // Wait for a short duration (e.g., 0.5 seconds) to allow other modules to initialize
            yield return new WaitForSeconds(0.5f);

            // Now, perform the potentially expensive data fetching
            var receiverInfo = registry.GetReceiverInfo(targetId, this.ClassName);
            if (receiverInfo != null)
            {
                targetReceiverInfo = receiverInfo;
            }
            else
            {
                isTransmitting = false;
                transmittedPower = 0;
            }

            // You could also add other initialization steps here that depend on targetReceiverInfo
            // For example, if you need to set up UI elements based on the fetched info.
        }


        #endregion
    }
}
