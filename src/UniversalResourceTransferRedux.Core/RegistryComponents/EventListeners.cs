using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalResourceTransferRedux.Core.RegistryComponents
{
    internal partial class URT_Registry
    {
        private void OnPartDie(Part p)
        {
            bool needRebuild = false;
            var tempManualTransmitters = manualTransmittersToTargets;
            var linksTemp = Links.ToArray();
            foreach (var receiverModule in p.FindModulesImplementing<IURT_Receiver>())
            {
                needRebuild = true;
                receiverFlightIds.Remove(receiverModule.ReceiverId);
                activeReceiverCache.Remove(receiverModule.ReceiverId);
                inactiveReceiverCache.Remove(receiverModule.ReceiverId);
                receiversOnActiveVessel.Remove(receiverModule.ReceiverId);
                receiverReceivedAmounts.Remove(receiverModule.ReceiverId);
                receiverRequestedAmounts.Remove(receiverModule.ReceiverId);
                foreach (var kvp in tempManualTransmitters)
                {
                    if (kvp.Value == receiverModule.ReceiverId) manualTransmittersToTargets.Remove(kvp.Key);
                }

                foreach (var link in linksTemp)
                {
                    if (link.ReceiverId == receiverModule.ReceiverId) Links.Remove(link);
                }
            }
            foreach (var transmitterModule in p.FindModulesImplementing<IURT_Transmitter>())
            {
                needRebuild = true;
                transmitterFlightIds.Remove(transmitterModule.TransmitterID);
                activeTransmitterCache.Remove(transmitterModule.TransmitterID);
                inactiveTransmitterCache.Remove(transmitterModule.TransmitterID);
                reservedForActiveVesselTransmitters.Remove(transmitterModule.TransmitterID);
                transmitterTransmittedAmounts.Remove(transmitterModule.TransmitterID);
                transmitterCurrentMaxAmounts.Remove(transmitterModule.TransmitterID);
                foreach (var kvp in tempManualTransmitters)
                {
                    if (kvp.Key == transmitterModule.TransmitterID) manualTransmittersToTargets.Remove(transmitterModule.TransmitterID);
                }
                foreach (var link in linksTemp)
                {
                    if (link.TransmitterId == transmitterModule.TransmitterID) Links.Remove(link);
                }
            }
            if (needRebuild) RebuildLinks();
        }
        private void OnActiveVesselChanged(Vessel newActiveVessel)
        {
            receiversOnActiveVessel.Clear();
            foreach (var receiverModuleList in newActiveVessel.parts.Select(s => s.FindModulesImplementing<IURT_Receiver>()))
            {
                foreach (var receiverModule in receiverModuleList)
                {
                    receiversOnActiveVessel.Add(receiverModule.ReceiverId);
                }
            }
            RebuildLinks();
        }

        private void OnVesselLoaded(Vessel loadedVessel)
        {
            foreach (var receiverModuleList in loadedVessel.parts.Select(s => s.FindModulesImplementing<IURT_Receiver>()))
            {
                foreach (var receiverModule in receiverModuleList)
                {
                    activeReceiverCache[receiverModule.ReceiverId] = receiverModule;
                    inactiveReceiverCache.Remove(receiverModule.ReceiverId);
                }
            }
            foreach (var transmitterModuleList in loadedVessel.parts.Select(s => s.FindModulesImplementing<IURT_Transmitter>()))
            {
                foreach (var transmitterModule in transmitterModuleList)
                {
                    activeTransmitterCache[transmitterModule.TransmitterID] = transmitterModule;
                    inactiveTransmitterCache.Remove(transmitterModule.TransmitterID);
                }
            }
        }
        private void OnVesselUnloaded(Vessel unloadedVessel)
        {
            foreach (var p in unloadedVessel.parts)
            {
                foreach (var rm in p.FindModulesImplementing<IURT_Receiver>())
                {
                    activeReceiverCache.Remove(rm.ReceiverId);
                }
                foreach (var tm in p.FindModulesImplementing<IURT_Transmitter>())
                {
                    activeTransmitterCache.Remove(tm.TransmitterID);
                }
            }
        }

        private void OnVesselDestroyedOrRecovered(Vessel v)
        {
            bool needRebuild = false;
            var tempManualTransmitters = manualTransmittersToTargets;
            var linksTemp = Links.ToArray();
            foreach (var p in v.parts)
            {
                foreach (var receiverModule in p.FindModulesImplementing<IURT_Receiver>())
                {
                    needRebuild = true;
                    receiverFlightIds.Remove(receiverModule.ReceiverId);
                    activeReceiverCache.Remove(receiverModule.ReceiverId);
                    inactiveReceiverCache.Remove(receiverModule.ReceiverId);
                    receiversOnActiveVessel.Remove(receiverModule.ReceiverId);
                    receiverReceivedAmounts.Remove(receiverModule.ReceiverId);
                    receiverRequestedAmounts.Remove(receiverModule.ReceiverId);
                    foreach (var kvp in tempManualTransmitters)
                    {
                        if (kvp.Value == receiverModule.ReceiverId) manualTransmittersToTargets.Remove(kvp.Key);
                    }

                    foreach (var link in linksTemp)
                    {
                        if (link.ReceiverId == receiverModule.ReceiverId) Links.Remove(link);
                    }
                }
                foreach (var transmitterModule in p.FindModulesImplementing<IURT_Transmitter>())
                {
                    needRebuild = true;
                    transmitterFlightIds.Remove(transmitterModule.TransmitterID);
                    activeTransmitterCache.Remove(transmitterModule.TransmitterID);
                    inactiveTransmitterCache.Remove(transmitterModule.TransmitterID);
                    reservedForActiveVesselTransmitters.Remove(transmitterModule.TransmitterID);
                    transmitterTransmittedAmounts.Remove(transmitterModule.TransmitterID);
                    transmitterCurrentMaxAmounts.Remove(transmitterModule.TransmitterID);
                    foreach (var kvp in tempManualTransmitters)
                    {
                        if (kvp.Key == transmitterModule.TransmitterID) manualTransmittersToTargets.Remove(transmitterModule.TransmitterID);
                    }
                    foreach (var link in linksTemp)
                    {
                        if (link.TransmitterId == transmitterModule.TransmitterID) Links.Remove(link);
                    }
                }
            }


            if (needRebuild) RebuildLinks();
        }

        private void OnVesselWasModified(Vessel v)
        {
            List<IURT_Transmitter> transmitters = new();
            List<IURT_Receiver> receivers = new();
            var linksToRemove = new List<URT_Link>();
            foreach (var link in Links)
            {
                if (transmitters.Any(s => s.TransmitterID == link.TransmitterId) &&
                    receivers.Any(s => s.ReceiverId == link.ReceiverId))
                {
                    linksToRemove.Add(link);
                }
            }
            foreach (var link in linksToRemove)
            {
                Links.Remove(link);
            }
        }
    }
}
