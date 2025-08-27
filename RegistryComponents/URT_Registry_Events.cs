using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UniversalResourceTransferRedux.RegistryComponents
{
    internal partial class URT_Registry
    {
        /* Add this entire region to your URT_Registry class.
   It handles subscribing to, unsubscribing from, and reacting to
   global game events related to part/vessel destruction.
*/

        #region Event Handling for Permanent Deregistration

        // A private field to hold our event handler delegate for termination.
        // This is necessary to ensure we can properly unsubscribe later.
        private EventData<ProtoVessel>.OnEvent terminationEventHandler;

        // It's good practice to subscribe in OnAwake() or OnLoad() for ScenarioModules.
        public override void OnAwake()
        {
            base.OnAwake();
            // Initialize the handler delegate here.
            terminationEventHandler = (vessel) => HandleVesselRemoved(vessel, false);
            SubscribeToGameEvents();
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                // This prevents issues if KSP were to create a duplicate for some reason.
                Destroy(this);
            }
        }

        // And always, always unsubscribe in OnDestroy() to prevent issues.
        public void OnDestroy()
        {
            UnsubscribeFromGameEvents();
        }

        private void SubscribeToGameEvents()
        {
            Debug.Log("[URT_Registry] Subscribing to game destruction events.");
            GameEvents.onPartDie.Add(HandlePartDestroyed);
            GameEvents.onVesselRecovered.Add(HandleVesselRemoved);
            GameEvents.onVesselTerminated.Add(terminationEventHandler); // Use the stored delegate
        }

        private void UnsubscribeFromGameEvents()
        {
            Debug.Log("[URT_Registry] Unsubscribing from game destruction events.");
            GameEvents.onPartDie.Remove(HandlePartDestroyed);
            GameEvents.onVesselRecovered.Remove(HandleVesselRemoved);
            GameEvents.onVesselTerminated.Remove(terminationEventHandler); // Use the stored delegate
        }

        /// <summary>
        /// This handler is called when a single part is destroyed (e.g., explodes).
        /// </summary>
        private void HandlePartDestroyed(Part part)
        {
            if (part == null) return;
            uint flightId = part.flightID;

            // Using FirstOrDefault is a safe way to find the KeyValuePair.
            var transmitterEntry = transmitterFlightIds.FirstOrDefault(kvp => kvp.Value == flightId);
            if (transmitterEntry.Key != 0) // The default for an int Key in a KeyValuePair is 0 if not found
            {
                Debug.Log($"[URT_Registry] Part {part.partInfo.title} (flightID: {flightId}) was destroyed. Deregistering transmitter ID {transmitterEntry.Key}.");
                deregisterTransmitter(transmitterEntry.Key);
            }

            var receiverEntry = receiverFlightIds.FirstOrDefault(kvp => kvp.Value == flightId);
            if (receiverEntry.Key != 0)
            {
                Debug.Log($"[URT_Registry] Part {part.partInfo.title} (flightID: {flightId}) was destroyed. Deregistering receiver ID {receiverEntry.Key}.");
                deregisterReceiver(receiverEntry.Key);
            }
        }

        /// <summary>
        /// This handler is called when a whole vessel is recovered or terminated.
        /// It gets a ProtoVessel, so we must iterate through its proto parts.
        /// </summary>
        private void HandleVesselRemoved(ProtoVessel vessel, bool quick) // 'quick' is required by onVesselRecovered
        {
            if (vessel == null) return;

            Debug.Log($"[URT_Registry] Vessel {vessel.vesselName} was removed. Checking its parts for deregistration.");

            foreach (var protoPart in vessel.protoPartSnapshots)
            {
                uint flightId = protoPart.flightID;

                var transmitterEntry = transmitterFlightIds.FirstOrDefault(kvp => kvp.Value == flightId);
                if (transmitterEntry.Key != 0)
                {
                    Debug.Log($"[URT_Registry] Part from removed vessel (flightID: {flightId}) is being deregistered as transmitter ID {transmitterEntry.Key}.");
                    deregisterTransmitter(transmitterEntry.Key);
                }

                var receiverEntry = receiverFlightIds.FirstOrDefault(kvp => kvp.Value == flightId);
                if (receiverEntry.Key != 0)
                {
                    Debug.Log($"[URT_Registry] Part from removed vessel (flightID: {flightId}) is being deregistered as receiver ID {receiverEntry.Key}.");
                    deregisterReceiver(receiverEntry.Key);
                }
            }
        }

        #endregion

    }
}
