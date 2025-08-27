using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniversalResourceTransferRedux.RegistryComponents; // Issue 4: Added using directive
using static UniversalResourceTransferRedux.GenericUtils;

namespace UniversalResourceTransferRedux
{
    public class URT_Receiver : PartModule
    {
        // Part properties
        [KSPField(isPersistant = false, guiActive = false)]
        private float receiverArea;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Wavelength")]
        private float receiverWavelength;
        [KSPField(isPersistant = false, guiActive = false)]
        private float receiverEfficiency;
        [KSPField(isPersistant = false, guiActive = false)]
        public double receiverTuningFactor;
        [KSPField(isPersistant = true, guiActive = false)]
        public string outputResourceName = "ElectricCharge";
        [KSPField(isPersistant = true, guiActive = false)]
        public float outputResourceEnergyFactor = 1.0f;

        //Dynamic properties
        [KSPField(isPersistant = true, guiActive = false)]
        public float receivedPower;
        [KSPField(isPersistant = true, guiActive = false)]
        public int receiverID = -1;

        [KSPField(isPersistant = true, guiActive = true, groupDisplayName = "Universal Resource Receiver", groupName = "URT_Receiver_gui", guiName = "Receiving Active"),
            UI_Toggle(enabledText = "Receiving Active", disabledText = "Receiving Disabled", affectSymCounterparts = UI_Scene.All)]
        private bool isReceiving = false;
        [KSPField(isPersistant = false, guiActive = true, groupDisplayName = "Universal Resource Receiver", groupName = "URT_Receiver_gui", guiName = "Received Power", guiUnits = "EC/s", guiFormat = "F2")]
        private float receivedPowerGui;


        private List<int> pairedTransmitters = new List<int>();
        private Dictionary<int, TransmitterInfo?> pairedTransmitterInfos = new Dictionary<int, TransmitterInfo?>();
        private URT_Registry registry;
        private int outputResourceHash;


        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasValue("pairedTransmitters"))
            {
                // Parse the saved string into our list
                pairedTransmitters = node.GetValue("pairedTransmitters").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => int.Parse(s)).ToList();

                // Issue 2 Solution: Initialize the cache dictionary with keys from the loaded list.
                pairedTransmitterInfos.Clear();
                foreach (var id in pairedTransmitters)
                {
                    if (!pairedTransmitterInfos.ContainsKey(id))
                    {
                        pairedTransmitterInfos.Add(id, null);
                    }
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            // Join the list into a comma-separated string and add it to the node.
            // Using .Count check makes save file cleaner if list is empty.
            if (pairedTransmitters.Count > 0)
            {
                node.AddValue("pairedTransmitters", string.Join(",", pairedTransmitters));
            }
            base.OnSave(node);
        }

        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor) { return; }
            registry = URT_Registry.Instance;

            if (registry == null)
            {
                Debug.LogError("[URT_Receiver] Cannot find URT_Registry instance!");
                return;
            }

            if (receiverID == -1) //Not initted
            {
                receiverID = registry.registerNewReceiverId(this.part.flightID);
            }

            registry.registerActiveReceiver(receiverID, this);

            // Issue 3 Solution: Start the coroutine here, in the safe OnStart lifecycle method.
            StartCoroutine(ManageTransmitterCache());
            outputResourceHash = PartResourceLibrary.Instance.GetDefinition(outputResourceName).id;
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            if (!isReceiving)
            {
                return;
            }
            this.vessel.GetConnectedResourceTotals(outputResourceHash, out double vesselOutputResourceAmount, out double vesselOutputResourceCapacity);
            double vesselOutputResourceSpareCapacity = vesselOutputResourceCapacity - vesselOutputResourceAmount;
            List<(int, TransmitterInfo?)> transmittersTuple = new List<(int, TransmitterInfo?)>();
            foreach (int transmitterId in pairedTransmitterInfos.Keys)
            {
                transmittersTuple.Add((transmitterId, pairedTransmitterInfos[transmitterId]));
            }
            receivedPower = URT_PowerCalculator.CalculateRecvPower(GetReceiverInfo(), transmittersTuple).Values.Sum();
            receivedPowerGui = receivedPower * outputResourceEnergyFactor;
            if (vesselOutputResourceSpareCapacity < receivedPowerGui)
            {
                return;

            }
            vessel.RequestResource(part, outputResourceHash, -1 * receivedPowerGui, true);

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
            // Note: Your GetReceiverInfo was missing parameters from the new generic version. I've updated it.
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

        private IEnumerator ManageTransmitterCache()
        {
            while (true)
            {
                while (!isReceiving)
                {
                    yield return new WaitForSeconds(1.0f);
                }

                Debug.Log($"[URT_Receiver] Refreshing cache for {pairedTransmitterInfos.Count} transmitters.");
                pairedTransmitterInfos.Clear();
                foreach (int transmitterId in pairedTransmitters.ToList())
                {
                    pairedTransmitterInfos.Add(transmitterId, registry.GetTransmitter(transmitterId, ClassName));
                }
                yield return new WaitForSeconds(30f);
            }
        }
        #endregion
    }
}
