using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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

        private URT_Registry registry;
        private int targetId;
        private uint targetPartId;
        private string targetVesselName;


        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor) { return; }
            registry = ScenarioRunner.GetLoadedModules().Find(s => s.ClassName == "URT_Registry") as URT_Registry;
            if (registry == null)
            {
                Debug.Log("[URT_Transmitter] URT_Registry module not found.");
            }

            if (transmitterID == -1) // If uninitiailized
            { 
                transmitterID = registry.RegisterTransmitter(this.part.flightID);
            }
            (targetId, targetPartId) = registry.GetReceiverLinkedToTransmitter(transmitterID);
            if (targetId == -1) { isTransmitting = false; targetVesselName = "Not Transmitting"; targetPartId = 1; }
        }
    }
}
