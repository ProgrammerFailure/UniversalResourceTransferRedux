using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UniversalResourceTransferRedux.GenericUtils;

namespace UniversalResourceTransferRedux
{
    public class URT_Receiver : PartModule
    {
        //Part properties
        [KSPField(isPersistant = true, guiActive = false)]
        public int receiverID = -1;

        [KSPField(isPersistant = false, guiActive = false)]
        private float receiverArea;

        [KSPField(isPersistant = true, guiActive = false)]
        private float receiverWavelength;

        [KSPField(isPersistant = false, guiActive = false)]
        private float receiverEfficiency;

        [KSPField(isPersistant = false, guiActive = false)]
        private float receiverTuningFactor;
        //Dynamic properties

        [KSPField(isPersistant = true, guiActive = true, groupDisplayName = "Universal Resource Receiver", groupName = "URT_Receiver_gui", guiName = "Received Power")]
        public float receivedPower;

        [KSPField(isPersistant = true, guiActive = true, groupDisplayName = "Universal Resource Receiver", groupName = "URT_Receiver_gui", guiName = "Receiving Active"), UI_Toggle(enabledText = "Receiving Active", disabledText = "Receiving Disable")]
        //TODO: Add callback later for when receiving is turned on or off
        private bool isReceiving = false;

        private List<int> pairedTransmitters = new List<int>(); //TODO: implement serialization/deserialization of this list
        private Dictionary<int, TransmitterInfo?> pairedTransmitterInfos = new Dictionary<int, TransmitterInfo?>();

        private UniversalResourceTransferRedux.RegistryComponents.URT_Registry registry;

        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor) { return; }
            registry = ScenarioRunner.GetLoadedModules().Find(s => s.ClassName == "URT_Registry") as UniversalResourceTransferRedux.RegistryComponents.URT_Registry;

            if (receiverID == -1) //Not initted
            {
                receiverID = registry.registerNewReceiverId(this.part.flightID);
            }

            registry.registerActiveReceiver(receiverID, this);
        }

        public void OnDestroy()
        {
            if (registry != null)
            {
                registry.deregisterActiveReceiver(receiverID);
            }
        }

        #region utilities
        public ReceiverInfo GetReceiverInfo()
        {
            return ReceiverInfo.Create(
                receiverArea,
                receiverWavelength,
                receiverEfficiency,
                this.part.vessel.protoVessel,
                pairedTransmitters,
                isReceiving,
                receiverTuningFactor
            );
        }

        private IEnumerator RefreshTransmitterCache()
        {
            yield return new WaitForSeconds(0.5f);

            foreach (int transmitterId in pairedTransmitterInfos.Keys)
            {
                var pairedTransmitterInfo = registry.GetTransmitter(transmitterId, this.ClassName);
                if (pairedTransmitterInfo != null)
                {
                    pairedTransmitterInfos[transmitterId] = pairedTransmitterInfo;
                }
                //Todo: Error checking for if it IS null
                yield return null;
            }
        }
        #endregion
    }
}
