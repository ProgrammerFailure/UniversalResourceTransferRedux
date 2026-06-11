using CommNet.Network;
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
            var activeTransmitters = new HashSet<int>();
            var usedTransmittersCount = 0;
            var linksToProcess = new List<URT_Link>();
            var manualLinks = new List<URT_Link>();
            var activeLinks = new List<URT_ActiveLink>();

            foreach (var link in Links)
            {
                if (receiverRequestedAmounts[link.ReceiverId] <= 0) continue;
                if (transmitterCurrentMaxAmounts[link.TransmitterId] <= 0) continue;
                activeTransmitters.Add(link.TransmitterId);
                var isManual = manualTransmittersToTargets.TryGetValue(link.TransmitterId, out var value);
                if (isManual && value == link.ReceiverId)
                {
                    manualLinks.Add(link);
                    continue;
                }
                else if (isManual && value != link.ReceiverId)
                {
                    continue;
                }

                linksToProcess.Add(link);
            }
            var activeTransmittersCount = activeTransmitters.Count;

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
                var lowestSharedParent = GenericUtils.FindLowestSharedParent(
                    GetTransmitterCelestialBody(manualLink.TransmitterId),
                    GetReceiverCelestialBody(manualLink.ReceiverId));
                var occlusionImpact = URT_PowerCalculator.OcclusionImpact(
                    transmitterPos,
                    receiverPos,
                    lowestSharedParent,
                    manualLink.AtmosphereAttenuationCoefficient
                    );
                if (occlusionImpact <= 1e-5) continue;
                var theoreticalEfficiency = Math.Min(manualLink.ConstantLinkFactor / distanceSquared, 1) * occlusionImpact * manualLink.MaxEfficiencyLimit;
                var maxPower = transmitterCurrentMaxAmounts[manualLink.TransmitterId] * theoreticalEfficiency;

                if (maxPower >= receiverRemainingDemands[manualLink.ReceiverId])
                {
                    var satisfiedReceiverDemand = receiverRemainingDemands[manualLink.ReceiverId];
                    var link = new URT_ActiveLink(manualLink, satisfiedReceiverDemand, lowestSharedParent, occlusionImpact);
                    activeLinks.Add(link);
                    transmitterDemands[manualLink.TransmitterId] = satisfiedReceiverDemand / theoreticalEfficiency;
                    receiverDemandsRunningSum -= satisfiedReceiverDemand;
                    receiverRemainingDemands[manualLink.ReceiverId] = 0;
                }
                else
                {
                    var satisfiedReceiverDemand = maxPower;
                    var link = new URT_ActiveLink(manualLink, satisfiedReceiverDemand, lowestSharedParent, occlusionImpact);
                    transmitterDemands[manualLink.TransmitterId] = transmitterCurrentMaxAmounts[manualLink.TransmitterId];
                    receiverDemandsRunningSum -= maxPower;
                    receiverRemainingDemands[manualLink.ReceiverId] -= maxPower;
                    activeLinks.Add(link);
                }
            }
            List<URT_LinkToProcess> linksToEfficiencies = new List<URT_LinkToProcess>();
            Dictionary<(int, int), (double, double)> linkTrueEfficiencies = new(); //key: (transmitterId, receiverId), value: (trueEfficiency, occlusionImpact)
            for (int i = 0; i < linksToProcess.Count; i++)
            {
                var link = linksToProcess[i];
                var rxPos = GetReceiverWorldPos(link.ReceiverId);
                var txPos = GetTransmitterWorldPos(link.TransmitterId);
                if (!rxPos.HasValue || !txPos.HasValue) continue;
                (var receiverPos, var transmitterPos) = (rxPos.Value, txPos.Value);
                var distanceSquared = (receiverPos - transmitterPos).sqrMagnitude;
                if (distanceSquared > link.MaxDistanceSquared) continue;
                var theoreticalEfficiency = Math.Min(link.ConstantLinkFactor / distanceSquared, 1) * link.MaxEfficiencyLimit;

                linksToEfficiencies.Add(new URT_LinkToProcess(link, theoreticalEfficiency, transmitterPos, receiverPos));
            }
            linksToEfficiencies.Sort((x, y) => y.TheoreticalEfficiency.CompareTo(x.TheoreticalEfficiency));

            for (int i = 0; i < linksToEfficiencies.Count; i++)
            {
                if (receiverDemandsRunningSum <= 0.0) break;
                if (usedTransmittersCount == activeTransmittersCount) break;
                var linkToProcess = linksToEfficiencies[i];
                if (receiverRemainingDemands[linkToProcess.Link.ReceiverId] <= 0) continue;
                if (transmitterDemands.ContainsKey(linkToProcess.Link.TransmitterId)) continue;
                if (reservedForActiveVesselTransmitters.Contains(linkToProcess.Link.TransmitterId) &&
                    !receiversOnActiveVessel.Contains(linkToProcess.Link.ReceiverId)) continue;

                double trueEfficiency;
                double occlusionImpact;
                CelestialBody body = GenericUtils.FindLowestSharedParent(
                        GetReceiverCelestialBody(linkToProcess.Link.ReceiverId),
                        GetTransmitterCelestialBody(linkToProcess.Link.TransmitterId));
                if (linkTrueEfficiencies.TryGetValue((linkToProcess.Link.TransmitterId, linkToProcess.Link.ReceiverId), out var trueEff))
                {
                    trueEfficiency = trueEff.Item1;
                    occlusionImpact = trueEff.Item2;
                }
                else
                {
                    occlusionImpact = URT_PowerCalculator.OcclusionImpact(
                    linkToProcess.TransmitterPosition,
                    linkToProcess.ReceiverPosition,
                    body,
                    linkToProcess.Link.AtmosphereAttenuationCoefficient
                    );
                    trueEfficiency = linkToProcess.TheoreticalEfficiency * occlusionImpact;
                }
                if (trueEfficiency <= 1e-9) continue;
                var skip = false;
                for (int j = i + 1; j < linksToEfficiencies.Count; j++)
                {

                    var tempLink = linksToEfficiencies[j];
                    if (tempLink.TheoreticalEfficiency <= trueEfficiency)
                    {
                        break;
                    }
                    if (tempLink.Link.TransmitterId != linkToProcess.Link.TransmitterId) continue;
                    double tempEfficiency;

                    if (linkTrueEfficiencies.TryGetValue((tempLink.Link.TransmitterId, tempLink.Link.ReceiverId), out var tempTrueEfficiency))
                    {
                        tempEfficiency = tempTrueEfficiency.Item1;
                    }
                    else
                    {
                        var tempOcclusionImpact = URT_PowerCalculator.OcclusionImpact(
                            tempLink.TransmitterPosition,
                            tempLink.ReceiverPosition,
                            GenericUtils.FindLowestSharedParent(
                               GetReceiverCelestialBody(tempLink.Link.ReceiverId),
                               GetTransmitterCelestialBody(tempLink.Link.TransmitterId)
                               ),
                            tempLink.Link.AtmosphereAttenuationCoefficient
                        );
                        tempEfficiency = tempLink.TheoreticalEfficiency * tempOcclusionImpact;
                        linkTrueEfficiencies.Add((tempLink.Link.TransmitterId, tempLink.Link.ReceiverId), (tempEfficiency, tempOcclusionImpact));
                    }
                    if (tempEfficiency > trueEfficiency)
                    {
                        skip = true;

                        break;
                    }
                }
                if (skip) continue;

                var maxPower = trueEfficiency * transmitterCurrentMaxAmounts[linkToProcess.Link.TransmitterId];
                if (maxPower < receiverRemainingDemands[linkToProcess.Link.ReceiverId])
                {
                    receiverRemainingDemands[linkToProcess.Link.ReceiverId] -= maxPower;
                    transmitterDemands[linkToProcess.Link.TransmitterId] = transmitterCurrentMaxAmounts[linkToProcess.Link.TransmitterId];
                    receiverDemandsRunningSum -= maxPower;
                    usedTransmittersCount++;
                    var activeLink = new URT_ActiveLink(linkToProcess.Link, maxPower, body, occlusionImpact);
                    activeLinks.Add(activeLink);
                }
                else
                {
                    var receiverSatisfaction = receiverRemainingDemands[linkToProcess.Link.ReceiverId];
                    receiverRemainingDemands[linkToProcess.Link.ReceiverId] = 0.0;
                    transmitterDemands[linkToProcess.Link.TransmitterId] = receiverSatisfaction / trueEfficiency;
                    receiverDemandsRunningSum -= receiverSatisfaction;
                    usedTransmittersCount++;
                    var activeLink = new URT_ActiveLink(linkToProcess.Link, receiverSatisfaction, body, occlusionImpact);
                    activeLinks.Add(activeLink);
                }
            }

            foreach (int transmitterId in transmitterTransmittedAmounts.Keys.ToList())
            {
                if (transmitterDemands.TryGetValue(transmitterId, out var demand))
                {
                    transmitterTransmittedAmounts[transmitterId] = transmitterDemands[transmitterId];
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
            ActiveLinks.Clear();
            foreach (var link in activeLinks)
            {
                ActiveLinks.Add(link);
            }
        }

        internal void ManageOcclusionCache()
        {
            int totalLinks = ActiveLinks.Count;
            if (totalLinks == 0) return;
            if (lastUpdatedIndex >= totalLinks)
            {
                lastUpdatedIndex = 0;
            }
            int updatesThisFrame = Math.Min(totalLinks, MaxUpdatesPerFrame);

            for (int i = 0; i < updatesThisFrame; i++)
            {
                var link = ActiveLinks[lastUpdatedIndex];

                var rxPos = GetReceiverWorldPos(link.Link.ReceiverId);
                var txPos = GetTransmitterWorldPos(link.Link.TransmitterId);

                if (rxPos.HasValue && txPos.HasValue)
                {
                    double distanceSquared = (rxPos.Value - txPos.Value).sqrMagnitude;

                    // Check planetary occlusion and atmospheric density
                    double occlusion = URT_PowerCalculator.OcclusionImpact(
                        txPos.Value,
                        rxPos.Value,
                        link.LowestSharedParent,
                        link.Link.AtmosphereAttenuationCoefficient
                    );

                    link.OcclusionImpact = occlusion;
                }
                else
                {
                    link.OcclusionImpact = 1.0;
                }
                ActiveLinks[lastUpdatedIndex] = link;

                // Increment and wrap our circular pointer
                lastUpdatedIndex = (lastUpdatedIndex + 1) % totalLinks;
            }
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
            if (GetTransmitterWorldPos(transmitterId) == GetReceiverWorldPos(receiverId))
            {
#if DEBUG
                Debug.Log("[URT] Transmitter and receiver returned the same position during link consideration!");
#endif
                return null; //Same vessel
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
                var rayleigh = (RayleighCoefficient / Math.Pow(transmitterInfo.Wavelength, 4));
                var mie = (MieCoefficient / Math.Pow(transmitterInfo.Wavelength, 1.3));
                return new URT_Link(transmitterId, receiverId, efficiency.Item1, maxDistanceSquared, efficiency.Item2, mie + rayleigh);
            }
            else
            Debug.Log("[URT] URT_Registry.CreateLink: Invalid link. Discarding");
            Debug.Log($"[URT] Link information: tID: {transmitterId}, rID: {receiverId}, efficiency: {efficiency}, maxDistanceSquared: {maxDistanceSquared}");
            Debug.Log($"[URT] Link constituents' information: {transmitterInfo.ToString()}, {receiverInfo.ToString()}");
            return null;
        }
    }
}
