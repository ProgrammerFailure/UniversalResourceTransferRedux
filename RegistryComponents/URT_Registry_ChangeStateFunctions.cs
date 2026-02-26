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
        public void SetTransmitterState(int transmitterId, bool isEnabled)
        {
            if (activeTransmitterCache.TryGetValue(transmitterId, out URT_Transmitter transmitter)
    && transmitter != null)
            {
                transmitter.SetTransmitterState(isEnabled);
            }
            else if (!transmitterFlightIds.TryGetValue(transmitterId, out uint transmitterFlightId))
            {
                return;
            }
            else if (FlightGlobals.FindPartByID(transmitterFlightId) is Part part)
            {
                part.FindModulesImplementing<URT_Transmitter>().Find(s => s.transmitterID == transmitterId).SetTransmitterState(isEnabled);
            }
            else if (FlightGlobals.FindProtoPartByID(transmitterFlightId) is ProtoPartSnapshot protoPart)
            {
                protoPart.modules.
                    FindAll(s => s.moduleName == "URT_Transmitter").
                    Find(s => s.moduleValues.GetInt("transmitterID") == transmitterId).
                    moduleValues.SetValue("isTransmitting", false);
            }
            callListeners();
        }

        public void SetReceiverState(int receiverId, bool isEnabled)
        {
            if (activeReceiverCache.TryGetValue(receiverId, out URT_Receiver receiver))
            {
                receiver.SetReceiverState(isEnabled);
            }
            else if (!receiverFlightIds.TryGetValue(receiverId, out uint receiverFlightId))
            {
                return;
            }
            else if (FlightGlobals.FindPartByID(receiverFlightId) is Part receiverPart)
            {
                receiverPart.FindModulesImplementing<URT_Receiver>().
                    Find(s => s.receiverID == receiverId).
                    SetReceiverState(isEnabled);
            }
            else if (FlightGlobals.FindProtoPartByID(receiverFlightId) is ProtoPartSnapshot receiverProtoPart)
            {
                var moduleSnapshot = receiverProtoPart.modules
                    .Find(m => m.moduleName == "URT_Receiver"
                            && m.moduleValues.GetInt("receiverID") == receiverId);

                if (moduleSnapshot?.moduleValues == null)
                {
                    return;
                }
                else
                {
                    moduleSnapshot.moduleValues.SetValue("isReceiving", isEnabled);
                }

            }
            callListeners();
        }
        public void SetTransmitterTarget(int transmitterId, int receiverId)
        {
            int oldReceiverId = Instance.GetTransmitterTarget(transmitterId);
            //Try to set the receiver id in the transmitter
            if (activeTransmitterCache.TryGetValue(transmitterId, out URT_Transmitter transmitter)
                && transmitter != null)
            {
                transmitter.SetTarget(receiverId);
            }
            else if (!transmitterFlightIds.TryGetValue(transmitterId, out uint transmitterFlightId))
            {
                return;
            }
            else if (FlightGlobals.FindPartByID(transmitterFlightId) is Part part)
            {
                part.FindModulesImplementing<URT_Transmitter>().Find(s => s.transmitterID == transmitterId).SetTarget(receiverId);
            }
            else if (FlightGlobals.FindProtoPartByID(transmitterFlightId) is ProtoPartSnapshot protoPart)
            {
                protoPart.modules.
                    FindAll(s => s.moduleName == "URT_Transmitter").
                    Find(s => s.moduleValues.GetInt("transmitterID") == transmitterId).
                    moduleValues.SetValue("targetId", receiverId);
            }

            //Try to remove the transmitterId from the old receiver
            if (activeReceiverCache.TryGetValue(oldReceiverId, out URT_Receiver oldReceiver))
            {
                oldReceiver.RemoveTransmitter(transmitterId);
            }
            else if (!receiverFlightIds.TryGetValue(oldReceiverId, out uint oldReceiverFlightId))
            {
                return;
            }
            else if (FlightGlobals.FindPartByID(oldReceiverFlightId) is Part oldReceiverPart)
            {
                oldReceiverPart.FindModulesImplementing<URT_Receiver>().
                    Find(s => s.receiverID == oldReceiverId).
                    RemoveTransmitter(transmitterId);
            }
            else if (FlightGlobals.FindProtoPartByID(oldReceiverFlightId) is ProtoPartSnapshot oldReceiverProtoPart)
            {
                var moduleSnapshot = oldReceiverProtoPart.modules
                    .Find(m => m.moduleName == "URT_Receiver"
                            && m.moduleValues.GetInt("receiverID") == oldReceiverId);

                if (moduleSnapshot?.moduleValues == null)
                    return;

                var values = moduleSnapshot.moduleValues;

                var raw = values.GetValue("pairedTransmitters");
                if (string.IsNullOrEmpty(raw))
                    return;

                var transmitters = raw.Split(',').ToList();

                transmitters.Remove(transmitterId.ToString());

                values.SetValue("pairedTransmitters",
                    string.Join(",", transmitters));
            }

            // Try to add the transmitterId to the new receiver
            if (activeReceiverCache.TryGetValue(receiverId, out URT_Receiver receiver))
            {
                receiver.AddTransmitter(transmitterId);
            }
            else if (!receiverFlightIds.TryGetValue(receiverId, out uint receiverFlightId))
            {
                return;
            }
            else if (FlightGlobals.FindPartByID(receiverFlightId) is Part receiverPart)
            {
                receiverPart.FindModulesImplementing<URT_Receiver>().Find(s => s.receiverID == receiverId).AddTransmitter(transmitterId);
            }
            else if (FlightGlobals.FindProtoPartByID(receiverFlightId) is ProtoPartSnapshot receiverProtoPart)
            {
                var moduleSnapshot = receiverProtoPart.modules.
                    Find(s => s.moduleName == "URT_Receiver" &&
                s.moduleValues.GetInt("receiverID") == receiverId);
                if (moduleSnapshot?.moduleValues == null)
                {
                    return;
                }
                var values = moduleSnapshot.moduleValues;
                var raw = values.GetValue("pairedTransmitters");
                if (string.IsNullOrEmpty(raw))
                {
                    return;
                }
                var transmitters = raw.Split(',').ToList();
                transmitters.Add(transmitterId.ToString());
                values.SetValue("pairedTransmitters", string.Join(",", transmitters));
            }
            callListeners();
        }

    }
}
