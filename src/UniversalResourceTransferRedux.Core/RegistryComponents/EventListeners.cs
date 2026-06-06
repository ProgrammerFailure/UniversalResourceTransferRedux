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
            foreach (var receiverModule in p.FindModulesImplementing<URT_Receiver>())
            {
                needRebuild = true;
                receiverFlightIds.Remove(receiverModule.receiverId);
                activeReceiverCache.Remove(receiverModule.receiverId);
                inactiveReceiverCache.Remove(receiverModule.receiverId);
                receiversOnActiveVessel.Remove(receiverModule.receiverId);
                receiverReceivedAmounts.Remove(receiverModule.receiverId);
                receiverRequestedAmounts.Remove(receiverModule.receiverId);
                foreach (var kvp in tempManualTransmitters)
                {
                    if (kvp.Value == receiverModule.receiverId) manualTransmittersToTargets.Remove(kvp.Key);
                }

                foreach (var link in linksTemp)
                {
                    if (link.ReceiverId == receiverModule.receiverId) Links.Remove(link);
                }
            }
            foreach (var transmitterModule in p.FindModulesImplementing<URT_Transmitter>())
            {
                needRebuild = true;
                transmitterFlightIds.Remove(transmitterModule.transmitterID);
                activeTransmitterCache.Remove(transmitterModule.transmitterID);
                inactiveTransmitterCache.Remove(transmitterModule.transmitterID);
                reservedForActiveVesselTransmitters.Remove(transmitterModule.transmitterID);
                transmitterTransmittedAmounts.Remove(transmitterModule.transmitterID);
                transmitterCurrentMaxAmounts.Remove(transmitterModule.transmitterID);
                foreach (var kvp in tempManualTransmitters)
                {
                    if (kvp.Key == transmitterModule.transmitterID) manualTransmittersToTargets.Remove(transmitterModule.transmitterID);
                }
                foreach (var link in linksTemp)
                {
                    if (link.TransmitterId == transmitterModule.transmitterID) Links.Remove(link);
                }
            }
            if (needRebuild) RebuildLinks();
        }
        private void OnActiveVesselChanged(Vessel newActiveVessel)
        {
            receiversOnActiveVessel.Clear();
            foreach (var receiverModuleList in newActiveVessel.parts.Select(s => s.FindModulesImplementing<URT_Receiver>()))
            {
                foreach (var receiverModule in receiverModuleList)
                {
                    receiversOnActiveVessel.Add(receiverModule.receiverId);
                }
            }
            RebuildLinks();
        }

        private void OnVesselLoaded(Vessel loadedVessel)
        {
            foreach (var receiverModuleList in loadedVessel.parts.Select(s => s.FindModulesImplementing<URT_Receiver>()))
            {
                foreach (var receiverModule in receiverModuleList)
                {
                    activeReceiverCache[receiverModule.receiverId] = receiverModule;
                    inactiveReceiverCache.Remove(receiverModule.receiverId);
                }
            }
            foreach (var transmitterModuleList in loadedVessel.parts.Select(s => s.FindModulesImplementing<URT_Transmitter>()))
            {
                foreach (var transmitterModule in transmitterModuleList)
                {
                    activeTransmitterCache[transmitterModule.transmitterID] = transmitterModule;
                    inactiveTransmitterCache.Remove(transmitterModule.transmitterID);
                }
            }
        }
        private void OnVesselUnloaded(Vessel unloadedVessel)
        {
            foreach (var p in unloadedVessel.parts)
            {
                foreach (var rm in p.FindModulesImplementing<URT_Receiver>())
                {
                    activeReceiverCache.Remove(rm.receiverId);
                }
                foreach (var tm in p.FindModulesImplementing<URT_Transmitter>())
                {
                    activeTransmitterCache.Remove(tm.transmitterID);
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
                foreach (var receiverModule in p.FindModulesImplementing<URT_Receiver>())
                {
                    needRebuild = true;
                    receiverFlightIds.Remove(receiverModule.receiverId);
                    activeReceiverCache.Remove(receiverModule.receiverId);
                    inactiveReceiverCache.Remove(receiverModule.receiverId);
                    receiversOnActiveVessel.Remove(receiverModule.receiverId);
                    receiverReceivedAmounts.Remove(receiverModule.receiverId);
                    receiverRequestedAmounts.Remove(receiverModule.receiverId);
                    foreach (var kvp in tempManualTransmitters)
                    {
                        if (kvp.Value == receiverModule.receiverId) manualTransmittersToTargets.Remove(kvp.Key);
                    }

                    foreach (var link in linksTemp)
                    {
                        if (link.ReceiverId == receiverModule.receiverId) Links.Remove(link);
                    }
                }
                foreach (var transmitterModule in p.FindModulesImplementing<URT_Transmitter>())
                {
                    needRebuild = true;
                    transmitterFlightIds.Remove(transmitterModule.transmitterID);
                    activeTransmitterCache.Remove(transmitterModule.transmitterID);
                    inactiveTransmitterCache.Remove(transmitterModule.transmitterID);
                    reservedForActiveVesselTransmitters.Remove(transmitterModule.transmitterID);
                    transmitterTransmittedAmounts.Remove(transmitterModule.transmitterID);
                    transmitterCurrentMaxAmounts.Remove(transmitterModule.transmitterID);
                    foreach (var kvp in tempManualTransmitters)
                    {
                        if (kvp.Key == transmitterModule.transmitterID) manualTransmittersToTargets.Remove(transmitterModule.transmitterID);
                    }
                    foreach (var link in linksTemp)
                    {
                        if (link.TransmitterId == transmitterModule.transmitterID) Links.Remove(link);
                    }
                }
            }


            if (needRebuild) RebuildLinks();
        }

        private void OnVesselWasModified(Vessel v)
        {
            List<URT_Transmitter> transmitters = new();
            List<URT_Receiver> receivers = new();
            var linksToRemove = new List<URT_Link>();
            foreach (var link in Links)
            {
                if (transmitters.Any(s => s.transmitterID == link.TransmitterId) &&
                    receivers.Any(s => s.receiverId == link.ReceiverId))
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
