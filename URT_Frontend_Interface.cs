using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalResourceTransferRedux.RegistryComponents;

namespace UniversalResourceTransferRedux
{
    internal class URT_Frontend_Interface : URT_Frontend.IUrtManager
    {
        public void AssignNewTargetFor(int transmitterId, int receiverId)
        {
            URT_Registry.Instance.SetTransmitterTarget(transmitterId, receiverId);
        }
        public List<URT_Frontend.ReceiverUIData> GetReceiverDisplayData()
        {
            var receivers = new List<URT_Frontend.ReceiverUIData>();
            foreach (int receiverId in URT_Registry.Instance.GetAllReceiverIds())
            {
                var tempReceiverInfo = URT_Registry.Instance.GetReceiverInfo(receiverId);
                var receiverInfo = new GenericUtils.ReceiverInfo();
                if (!tempReceiverInfo.HasValue)
                {
                    continue;
                }
                else
                {
                    receiverInfo = tempReceiverInfo.Value;
                }
                var receiverData = new URT_Frontend.ReceiverUIData();

                receiverData.Id = receiverId;
                receiverData.IsEnabled = receiverInfo.isReceiving;
                if (URT_Registry.Instance.GetReceiverPartAndVesselName(receiverId) is GenericUtils.PartAndVesselName names)
                {
                    (receiverData.PartName, receiverData.VesselName) = (names.partName, names.vesselName);
                }
                else
                {
                    (receiverData.PartName, receiverData.VesselName) = ("Name Unknown", "Name Unknown");
                }

                var transmitterInfos = new List<(int, GenericUtils.TransmitterInfo?)>();
                foreach (int transmitterId in receiverInfo.pairedTransmitters)
                {
                    transmitterInfos.Add((transmitterId, URT_Registry.Instance.GetTransmitter(transmitterId)));
                }
                var transmitterPowers = URT_PowerCalculator.CalculateRecvPower(receiverInfo, transmitterInfos);
                receiverData.Power = transmitterPowers.Values.Sum();
                receiverData.TransmitterIdsToPower = transmitterPowers;
                receivers.Add(receiverData);
            }
            return receivers;
        }

        public List<URT_Frontend.TransmitterUIData> GetTransmitterDisplayData()
        {
            var transmitters = new List<URT_Frontend.TransmitterUIData>();
            foreach (int transmitterId in URT_Registry.Instance.GetAllTransmitterIds())
            {
                var transmitterData = new URT_Frontend.TransmitterUIData();
                var tempTransmitterInfo = URT_Registry.Instance.GetTransmitter(transmitterId);
                if (!tempTransmitterInfo.HasValue) continue;
                var transmitterInfo = tempTransmitterInfo.Value;
                transmitterData.Id = transmitterId;
                transmitterData.IsEnabled = transmitterInfo.isTransmitting;
                transmitterData.Power = transmitterInfo.Power;
                if (URT_Registry.Instance.GetTransmitterTarget(transmitterId) is int targetId && targetId != -1)
                {
                    if (URT_Registry.Instance.GetReceiverPartAndVesselName(targetId) is GenericUtils.PartAndVesselName transmitterNames)
                    {
                        transmitterData.TargetName = transmitterNames.vesselName + ", " + transmitterNames.partName;
                    }
                }
                else
                {
                    transmitterData.TargetName = "Target Unknown";
                }

                if (URT_Registry.Instance.GetTransmitterPartAndVesselName(transmitterId) is GenericUtils.PartAndVesselName names)
                {
                    transmitterData.PartName = names.partName;
                    transmitterData.VesselName = names.vesselName;
                }
                else
                {
                    (transmitterData.PartName, transmitterData.VesselName) = ("Target Unknown", "Target Unknown");
                }
                transmitters.Add(transmitterData);
            }
            return transmitters;
        }

        public void SetReceiverEnabled(int receiverId, bool isEnabled)
        {
            URT_Registry.Instance.SetReceiverState(receiverId, isEnabled);
        }

        public void SetTransmitterEnabled(int transmitterId, bool isEnabled)
        {
            URT_Registry.Instance.SetTransmitterState(transmitterId, isEnabled);
        }
    }
}
