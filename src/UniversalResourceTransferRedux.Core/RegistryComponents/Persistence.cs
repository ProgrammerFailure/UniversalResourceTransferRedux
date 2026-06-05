using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalResourceTransferRedux.Core.RegistryComponents
{
    partial class URT_Registry
    {
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            var TransmitterFlightIdsNode = new ConfigNode();
            foreach (int transmitterId in transmitterFlightIds.Keys)
            {
                var flightIdNode = new ConfigNode();
                flightIdNode.AddValue("TransmitterID", transmitterId);
                flightIdNode.AddValue("FlightID", transmitterFlightIds[transmitterId]);
                TransmitterFlightIdsNode.AddNode("TRANSMITTER", flightIdNode);
            }
            node.AddNode("TransmitterFlightIds", TransmitterFlightIdsNode);

            var ReceiverFlightIdsNode = new ConfigNode();
            foreach (int receiverId in receiverFlightIds.Keys)
            {
                var flightIdNode = new ConfigNode();
                flightIdNode.AddValue("ReceiverID", receiverId);
                flightIdNode.AddValue("FlightID", receiverFlightIds[receiverId]);
                ReceiverFlightIdsNode.AddNode("RECEIVER", flightIdNode);
            }
            node.AddNode("ReceiverFlightIds", ReceiverFlightIdsNode);

            var LinksNode = new ConfigNode();
            foreach (URT_Link link in Links)
            {
                var LinkNode = new ConfigNode();
                LinkNode.AddValue("TransmitterID", link.TransmitterId);
                LinkNode.AddValue("ReceiverID", link.ReceiverId);
                LinkNode.AddValue("StaticEfficiencyFactor", link.ConstantLinkFactor);
                LinkNode.AddValue("MaxEfficiency", link.MaxEfficiencyLimit);
                LinkNode.AddValue("MaxDistanceSquared", link.MaxDistanceSquared);
                LinksNode.AddNode("LINK", LinkNode);
            }
            node.AddNode("Links", LinksNode);

            var TransmitterCurrentMaxAmountsNode = new ConfigNode();
            foreach (KeyValuePair<int, double> kvp in transmitterCurrentMaxAmounts)
            {
                var amountNode = new ConfigNode();
                amountNode.AddValue("TransmitterID", kvp.Key);
                amountNode.AddValue("MaxAmount", kvp.Value);
                TransmitterCurrentMaxAmountsNode.AddNode("TRANSMITTER_MAX", amountNode);
            }
            node.AddNode("TransmitterCurrentMaxAmounts", TransmitterCurrentMaxAmountsNode);

            var ReceiverRequestedAmountsNode = new ConfigNode();
            foreach (KeyValuePair<int, double> kvp in receiverRequestedAmounts)
            {
                var amountNode = new ConfigNode();
                amountNode.AddValue("ReceiverID", kvp.Key);
                amountNode.AddValue("RequestedAmount", kvp.Value);
                ReceiverRequestedAmountsNode.AddNode("RECEIVER_REQUEST", amountNode);
            }
            node.AddNode("ReceiverRequestedAmounts", ReceiverRequestedAmountsNode);

            var ManualTransmittersToTargetsNode = new ConfigNode();
            foreach (KeyValuePair<int, int> kvp in manualTransmittersToTargets)
            {
                var targetNode = new ConfigNode();
                targetNode.AddValue("TransmitterID", kvp.Key);
                targetNode.AddValue("ReceiverID", kvp.Value);
                ManualTransmittersToTargetsNode.AddNode("MANUAL_TARGET", targetNode);
            }
            node.AddNode("ManualTransmittersToTargets", ManualTransmittersToTargetsNode);

            var ReservedForActiveVesselTransmittersNode = new ConfigNode();
            foreach (int transmitterId in reservedForActiveVesselTransmitters)
            {
                var reservedNode = new ConfigNode();
                reservedNode.AddValue("TransmitterID", transmitterId);
                ReservedForActiveVesselTransmittersNode.AddNode("RESERVED_TRANSMITTER", reservedNode);
            }
            node.AddNode("ReservedForActiveVesselTransmitters", ReservedForActiveVesselTransmittersNode);

            var TransmitterModuleIdsNode = new ConfigNode();
            foreach (var transmitter in transmitterModuleIds)
            {
                var transmitterNode = new ConfigNode();
                transmitterNode.AddValue("TransmitterId", transmitter.Key);
                transmitterNode.AddValue("TransmitterModuleId", transmitter.Value);
                TransmitterModuleIdsNode.AddNode("TRANSMITTER", transmitterNode);
            }
            node.AddNode("TransmitterModuleIdsNode", TransmitterModuleIdsNode);

            var ReceiverModuleIdsNode = new ConfigNode();
            foreach (var receiver in receiverModuleIds)
            {
                var receiverNode = new ConfigNode();
                receiverNode.AddValue("ReceiverId", receiver.Key);
                receiverNode.AddValue("ReceiverModuleId", receiver.Value);
                ReceiverModuleIdsNode.AddNode("RECEIVER", receiverNode);
            }
            node.AddNode("ReceiverModuleIdsNode", ReceiverModuleIdsNode);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            transmitterFlightIds.Clear();
            receiverFlightIds.Clear();
            Links.Clear();
            transmitterCurrentMaxAmounts.Clear();
            receiverRequestedAmounts.Clear();
            manualTransmittersToTargets.Clear();
            reservedForActiveVesselTransmitters.Clear();

            if (node.HasNode("TransmitterFlightIds"))
            {
                ConfigNode parentNode = node.GetNode("TransmitterFlightIds");
                foreach (ConfigNode child in parentNode.GetNodes("TRANSMITTER"))
                {
                    if (int.TryParse(child.GetValue("TransmitterID"), out int tId) &&
                    uint.TryParse(child.GetValue("FlightID"), out uint fId))
                    {
                        transmitterFlightIds[tId] = fId;
                    }
                }
            }

            if (node.HasNode("ReceiverFlightIds"))
            {
                ConfigNode parentNode = node.GetNode("ReceiverFlightIds");
                foreach (ConfigNode child in parentNode.GetNodes("RECEIVER"))
                {
                    if (int.TryParse(child.GetValue("ReceiverID"), out int rId) &&
                    uint.TryParse(child.GetValue("FlightID"), out uint fId))
                    {
                        receiverFlightIds[rId] = fId;
                    }
                }
            }

            if (node.HasNode("Links"))
            {
                ConfigNode parentNode = node.GetNode("Links");
                foreach (ConfigNode child in parentNode.GetNodes("LINK"))
                {
                    if (int.TryParse(child.GetValue("TransmitterID"), out int tId) &&
                    int.TryParse(child.GetValue("ReceiverID"), out int rId) &&
                    double.TryParse(child.GetValue("StaticEfficiencyFactor"), out double constantEffFactor) &&
                    double.TryParse(child.GetValue("MaxDistanceSquared"), out double dist) &&
                    double.TryParse(child.GetValue("MaxEfficiency"), out double maxEff)
                    )
                    {
                        Links.Add(new URT_Link(tId, rId, constantEffFactor, dist, maxEff));
                    }
                }
            }

            if (node.HasNode("TransmitterCurrentMaxAmounts"))
            {
                ConfigNode parentNode = node.GetNode("TransmitterCurrentMaxAmounts");
                foreach (ConfigNode child in parentNode.GetNodes("TRANSMITTER_MAX"))
                {
                    if (int.TryParse(child.GetValue("TransmitterID"), out int tId) &&
                    double.TryParse(child.GetValue("MaxAmount"), out double amt))
                    {
                        transmitterCurrentMaxAmounts[tId] = amt;
                    }
                }
            }

            if (node.HasNode("ReceiverRequestedAmounts"))
            {
                ConfigNode parentNode = node.GetNode("ReceiverRequestedAmounts");
                foreach (ConfigNode child in parentNode.GetNodes("RECEIVER_REQUEST"))
                {
                    if (int.TryParse(child.GetValue("ReceiverID"), out int rId) &&
                    double.TryParse(child.GetValue("RequestedAmount"), out double amt))
                    {
                        receiverRequestedAmounts[rId] = amt;
                    }
                }
            }

            if (node.HasNode("ManualTransmittersToTargets"))
            {
                ConfigNode parentNode = node.GetNode("ManualTransmittersToTargets");
                foreach (ConfigNode child in parentNode.GetNodes("MANUAL_TARGET"))
                {
                    if (int.TryParse(child.GetValue("TransmitterID"), out int tId) &&
                    int.TryParse(child.GetValue("ReceiverID"), out int rId))
                    {
                        manualTransmittersToTargets[tId] = rId;
                    }
                }
            }

            if (node.HasNode("ReservedForActiveVesselTransmitters"))
            {
                ConfigNode parentNode = node.GetNode("ReservedForActiveVesselTransmitters");
                foreach (ConfigNode child in parentNode.GetNodes("RESERVED_TRANSMITTER"))
                {
                    if (int.TryParse(child.GetValue("TransmitterID"), out int tId))
                    {
                        reservedForActiveVesselTransmitters.Add(tId);
                    }
                }
            }

            if (node.HasNode("TransmitterModuleIdsNode"))
            {
                var parentNode = node.GetNode("TransmitterModuleIdsNode");
                foreach (var child in parentNode.GetNodes("TRANSMITTER"))
                {
                    transmitterModuleIds.Add(
                        child.GetInt("TransmitterId"),
                        child.GetInt("TransmitterModuleId")
                    );
                }
            }

            if (node.HasNode("ReceiverModuleIdsNode"))
            {
                var parentNode = node.GetNode("ReceiverModuleIdsNode");
                foreach (var child in parentNode.GetNodes("RECEIVER"))
                {
                    receiverModuleIds.Add(
                        child.GetInt("ReceiverId"),
                        child.GetInt("ReceiverModuleId")
                    );
                }
            }

            RebuildLinks();
        }

    }
}
