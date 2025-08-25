using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        //Dynamic properties

        [KSPField(isPersistant = true, guiActive = true, groupDisplayName = "Universal Resource Receiver", groupName = "URT_Receiver_gui", guiName = "Received Power")]
        public float receivedPower;

        [KSPField(isPersistant = true, guiActive = true, groupDisplayName = "Universal Resource Receiver", groupName = "URT_Receiver_gui", guiName = "Receiving Active"), UI_Toggle(enabledText = "Receiving Active", disabledText = "Receiving Disable")]
        //TODO: Add callback later for when receiving is turned on or off
        private bool isReceiving = false;

        private URT_Registry registry;

        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor) { return; }
            registry = ScenarioRunner.GetLoadedModules().Find(s => s.ClassName == "URT_Registry") as URT_Registry;

            if (receiverID == -1) //Not initted
            {
                receiverID = registry.registerNewReceiverId(this.part.flightID);
            }
        }

        public ReceiverInfo GetReceiverInfo()
        {
            ReceiverInfo receiverInfo = new ReceiverInfo();
            receiverInfo.Area = receiverArea;
            receiverInfo.Efficiency = receiverEfficiency;
            receiverInfo.parentProtoVessel = this.vessel.protoVessel;
            receiverInfo.Wavelength = receiverWavelength;
            return receiverInfo;
        }
    }
}
