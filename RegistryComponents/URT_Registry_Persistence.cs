using System.Collections.Generic;

namespace UniversalResourceTransferRedux.RegistryComponents
{
    internal partial class URT_Registry
    {
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            // --- Issue 2 Solution: Clear collections for robust reloading ---
            receiverFlightIds.Clear();
            transmitterFlightIds.Clear();

            // Load all saved receivers
            foreach (ConfigNode receiverNode in node.GetNodes("RECEIVER"))
            {
                var receiverId = receiverNode.GetInt("receiverId");
                var receiverFlightId = (uint)receiverNode.GetInt("receiverFlightId");
                receiverFlightIds.Add(receiverId, receiverFlightId);
            }

            // Load all saved transmitters
            foreach (ConfigNode transmitterNode in node.GetNodes("TRANSMITTER"))
            {
                var transmitterId = transmitterNode.GetInt("transmitterId");
                var transmitterFlightId = (uint)transmitterNode.GetInt("transmitterFlightId");

                // --- Issue 1 Solution: Use the correct dictionary ---
                transmitterFlightIds.Add(transmitterId, transmitterFlightId);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            // Save all registered receivers
            foreach (KeyValuePair<int, uint> entry in receiverFlightIds)
            {
                var receiverNode = new ConfigNode("RECEIVER");
                receiverNode.AddValue("receiverId", entry.Key);
                receiverNode.AddValue("receiverFlightId", entry.Value);
                node.AddNode(receiverNode);
            }

            // Save all registered transmitters
            foreach (KeyValuePair<int, uint> entry in transmitterFlightIds)
            {
                var transmitterNode = new ConfigNode("TRANSMITTER");
                transmitterNode.AddValue("transmitterId", entry.Key);
                transmitterNode.AddValue("transmitterFlightId", entry.Value);
                node.AddNode(transmitterNode);
            }
        }
    }
}
