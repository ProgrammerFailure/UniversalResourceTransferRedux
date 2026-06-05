using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UniversalResourceTransferRedux.Core.RegistryComponents
{
    internal partial class URT_Registry
    {
        internal void RebuildLinks()
        {
            var activeTransmitters = new List<int>();
            var usedTransmittersCount = 0;
            var linksToProcess = new List<URT_Link>();
            var manualLinks = new List<URT_Link>();
            var activeLinks = new List<(URT_Link, double)>();
            foreach (var link in Links)
            {
                if (receiverRequestedAmounts[link.ReceiverId] <= 0) continue;
                if (transmitterCurrentMaxAmounts[link.TransmitterId] <= 0) continue;
                activeTransmitters.Add(link.TransmitterId);
                var isManual = manualTransmittersToTargets.TryGetValue(link.TransmitterId, out var value);
                if (isManual && value == link.ReceiverId)
                {
                    manualLinks.Add(link);
                }
                else if (isManual && value != link.ReceiverId)
                {
                    continue;
                }

                linksToProcess.Add(link);
            }
            var activeTransmittersCount = activeTransmitters.Count();

            Dictionary<int, double> receiverRemainingDemands = new();
            foreach (var kvp in receiverRequestedAmounts)
            {
                receiverRemainingDemands.Add(kvp.Key, kvp.Value);
            }

            Dictionary<int, double> transmitterDemands = new();
            double receiverDemandsRunningSum = receiverRemainingDemands.Values.Sum();

            foreach (var manualLink in manualLinks)
            {
                if (receiverRemainingDemands[manualLink.ReceiverId] <= 0) continue;

                var rxPos = GetReceiverWorldPos(manualLink.ReceiverId);
                var txPos = GetTransmitterWorldPos(manualLink.TransmitterId);
                if (!rxPos.HasValue || !txPos.HasValue) continue;
                (var receiverPos, var transmitterPos) = (rxPos.Value, txPos.Value);
                var distanceSquared = (receiverPos - transmitterPos).sqrMagnitude;
                if (distanceSquared > manualLink.MaxDistanceSquared) continue;
                var theoreticalEfficiency = Math.Min(manualLink.ConstantLinkFactor / distanceSquared, 1);
                var maxPower = transmitterCurrentMaxAmounts[manualLink.TransmitterId] * theoreticalEfficiency;

                if (maxPower >= receiverRemainingDemands[manualLink.ReceiverId])
                {
                    var satisfiedReceiverDemand = receiverRemainingDemands[manualLink.ReceiverId];
                    activeLinks.Add((manualLink, satisfiedReceiverDemand));
                    transmitterDemands[manualLink.TransmitterId] = satisfiedReceiverDemand / theoreticalEfficiency;
                    receiverDemandsRunningSum -= satisfiedReceiverDemand;
                    receiverRemainingDemands[manualLink.ReceiverId] = 0;

                }
                else
                {
                    var satisfiedReceiverDemand = maxPower;
                    transmitterDemands[manualLink.TransmitterId] = transmitterCurrentMaxAmounts[manualLink.TransmitterId];
                    receiverDemandsRunningSum -= maxPower;
                    receiverRemainingDemands[manualLink.ReceiverId] -= maxPower;
                    activeLinks.Add((manualLink, maxPower));
                }
            }

            Dictionary<URT_Link, double> linksToEfficiencies = new();
            foreach (var link in linksToProcess)
            {
                var rxPos = GetReceiverWorldPos(link.ReceiverId);
                var txPos = GetTransmitterWorldPos(link.TransmitterId);
                if (!rxPos.HasValue || !txPos.HasValue) continue;
                (var receiverPos, var transmitterPos) = (rxPos.Value, txPos.Value);
                var distanceSquared = (receiverPos - transmitterPos).sqrMagnitude;
                if (distanceSquared > link.MaxDistanceSquared) continue;
                var theoreticalEfficiency = Math.Min(link.ConstantLinkFactor / distanceSquared, 1);

                linksToEfficiencies.Add(link, theoreticalEfficiency);
            }

            var linksSorted = linksToEfficiencies.OrderByDescending(s => s.Value);

            foreach (var linkToEfficiency in linksSorted)
            {
                if (receiverDemandsRunningSum == 0.0) break;
                if (usedTransmittersCount == activeTransmittersCount) break;
                if (receiverRemainingDemands[linkToEfficiency.Key.ReceiverId] <= 0) continue;
                if (transmitterDemands.ContainsKey(linkToEfficiency.Key.TransmitterId)) continue;
                if (reservedForActiveVesselTransmitters.Contains(linkToEfficiency.Key.TransmitterId) &&
                    !receiversOnActiveVessel.Contains(linkToEfficiency.Key.ReceiverId)) continue;

                var maxPower = transmitterCurrentMaxAmounts[linkToEfficiency.Key.TransmitterId] * linkToEfficiency.Value;

                if (maxPower >= receiverRemainingDemands[linkToEfficiency.Key.ReceiverId])
                {
                    var receiverSatisfaction = receiverRemainingDemands[linkToEfficiency.Key.ReceiverId];
                    receiverDemandsRunningSum -= receiverSatisfaction;
                    transmitterDemands.Add(
                        linkToEfficiency.Key.TransmitterId,
                        receiverSatisfaction / linkToEfficiency.Value
                    );
                    receiverRemainingDemands[linkToEfficiency.Key.ReceiverId] = 0;
                    usedTransmittersCount++;
                    activeLinks.Add((linkToEfficiency.Key, receiverSatisfaction));
                }
                else
                {
                    var receiverSatisfaction = maxPower;
                    transmitterDemands.Add(linkToEfficiency.Key.TransmitterId, transmitterCurrentMaxAmounts[linkToEfficiency.Key.TransmitterId]);
                    receiverRemainingDemands[linkToEfficiency.Key.ReceiverId] -= maxPower;
                    receiverDemandsRunningSum -= maxPower;
                    usedTransmittersCount++;
                    activeLinks.Add((linkToEfficiency.Key, maxPower));
                }
            }
            foreach (int transmitterId in transmitterTransmittedAmounts.Keys.ToList())
            {
                if (transmitterDemands.TryGetValue(transmitterId, out var value))
                {
                    transmitterTransmittedAmounts[transmitterId] = value;
                }
                else
                {
                    transmitterTransmittedAmounts[transmitterId] = 0.0;
                }
            }
            foreach (int receiverId in receiverReceivedAmounts.Keys.ToList())
            {
                if (receiverRemainingDemands.TryGetValue(receiverId, out var value))
                {

                    receiverReceivedAmounts[receiverId] = Math.Max(receiverRequestedAmounts[receiverId] - receiverRemainingDemands[receiverId], 0);

                }
                else
                {
                    //Should not happen
                    receiverReceivedAmounts[receiverId] = 0;
                }
            }
            ActiveLinks = activeLinks;
        }
        internal URT_Link? CreateLink(GenericUtils.TransmitterInfo? txInfo, GenericUtils.ReceiverInfo? rxInfo, int transmitterId, int receiverId)
        {
            if (!rxInfo.HasValue && !txInfo.HasValue)
            {
                throw new InvalidDataException("Both rxInfo and txInfo did not have a value!");
            }
            if (!rxInfo.HasValue)
            {
                throw new InvalidDataException("rxInfo did not have a value!");
            }
            if (!txInfo.HasValue)
            {
                throw new InvalidDataException("txInfo did not have a value!");
            }
            var transmitterInfo = txInfo.Value;
            var receiverInfo = rxInfo.Value;
            if (!transmitterInfo.ResourceTypeTags.Intersect(receiverInfo.ResourceTypeTags).Any())
            {
                Debug.Log("[URT] Invalid Link. Discarding.");
                Debug.Log("[URT]: Issue - transmitter and receiver shared no resource type tags");
                return null;
            }
            var efficiency = URT_PowerCalculator.CalculateConstantLinkFactor(transmitterInfo, receiverInfo);
            var maxDistanceSquared = efficiency.Item1 * transmitterInfo.MaxPower * efficiency.Item2;
            if (efficiency.Item1 > 0 && efficiency.Item2 > 0 && maxDistanceSquared > 0)
            {
                return new URT_Link(transmitterId, receiverId, efficiency.Item1, maxDistanceSquared, efficiency.Item2);
            }
            else
                Debug.Log("[URT] URT_Registry.CreateLink: Invalid link. Discarding");
            Debug.Log($"[URT] Link information: tID: {transmitterId}, rID: {receiverId}, efficiency: {efficiency}, maxDistanceSquared: {maxDistanceSquared}");
            Debug.Log($"[URT] Link constituents' information: {transmitterInfo.ToString()}, {receiverInfo.ToString()}");
                return null;
        }
    }
}
