using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalResourceTransferRedux.Core.RegistryComponents
{
    internal partial class URT_Registry
    {
        public int RegisterNewTransmitter(uint partFlightId, URT_Transmitter transmitterModule, GenericUtils.TransmitterInfo txInfo, int transmitterModuleId)
        {
            if (transmitterFlightIds.ContainsValue(partFlightId) && transmitterModuleIds.ContainsValue(transmitterModuleId))
            {
                throw new InvalidOperationException("Duplicate transmitter added!");
            }
            foreach (var receiverId in receiverFlightIds.Keys)
            {
                RefreshCacheReceiver(receiverId);
                var link = CreateLink(txInfo, GetReceiverInfo(receiverId), nextTransmitterInt, receiverId);
                if (link != null && !Links.Contains(link))
                {
                    Links.Add(link);
                }
            }
            transmitterFlightIds.Add(nextTransmitterInt, partFlightId);
            transmitterModuleIds.Add(nextTransmitterInt, transmitterModuleId);
            var assignedId = nextTransmitterInt;
            activeTransmitterCache[assignedId] = transmitterModule;
            inactiveTransmitterCache.Remove(assignedId);
            nextTransmitterInt++;
            return assignedId;
        }

        public int RegisterNewReceiver(uint partFlightId, URT_Receiver receiverModule, GenericUtils.ReceiverInfo rxInfo, int receiverModuleId)
        {
            if (receiverFlightIds.ContainsValue(partFlightId) && receiverModuleIds.ContainsValue(receiverModuleId))
            {
                throw new InvalidOperationException("Duplicate receiver added!");
            }
            foreach (var transmitterId in transmitterFlightIds.Keys)
            {
                RefreshCacheTransmitter(transmitterId);
                var link = CreateLink(GetTransmitterInfo(transmitterId), rxInfo, transmitterId, nextReceiverId);
                if (link != null && !Links.Contains(link))
                {
                    Links.Add(link);
                }
            }
            receiverFlightIds.Add(nextReceiverId, partFlightId);
            receiverModuleIds.Add(nextReceiverId, receiverModuleId);
            var assignedId = nextReceiverId;
            activeReceiverCache[assignedId] = receiverModule;
            inactiveReceiverCache.Remove(assignedId);
            nextReceiverId++;
            CallAllListeners();
            return assignedId;
        }

        public void RegisterActiveTransmitter(int transmitterId, URT_Transmitter transmitterModule)
        {
            if (!transmitterFlightIds.ContainsKey(transmitterId)) return;
            activeTransmitterCache[transmitterId] = transmitterModule;
        }
        public void RegisterActiveReceiver(int receiverId, URT_Receiver receiverModule)
        {
            if (!receiverFlightIds.ContainsKey(receiverId)) return;
            activeReceiverCache[receiverId] = receiverModule;
        }
        public void DeregisterActiveTransmitter(int transmitterId)
        {
            activeTransmitterCache.Remove(transmitterId);
        }
        public void DeregisterActiveReceiver(int receiverId)
        {
            activeReceiverCache.Remove(receiverId);
        }

        public void RegisterListener(Action listener)
        {
            listeners.Add(listener);
        }
    }
}
