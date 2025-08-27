using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalResourceTransferRedux
{
    internal static class URT_PowerCalculator
    {
        // Issue 1: Removed the unsafe registry field. This class should be stateless.

        public static Dictionary<int, float> CalculateRecvPower(GenericUtils.ReceiverInfo receiverInfo, List<(int transmitterId, GenericUtils.TransmitterInfo? transmitterInfo)> transmitters)
        {
            Dictionary<int, float> recvPowers = new Dictionary<int, float>();

            foreach ((int transmitterId, GenericUtils.TransmitterInfo? transmitterInfo) in transmitters)
            {
                // Issue 4: Simplified continue logic into a single guard clause.
                if (!transmitterInfo.HasValue || !transmitterInfo.Value.isTransmitting)
                {
                    recvPowers.Add(transmitterId, 0.0f);
                    continue;
                }

                // The .Value is now safe to access for the rest of the loop.
                var txInfo = transmitterInfo.Value;

                double wavelengthMismatchEffect = CalculateWavelengthMismatch(txInfo, receiverInfo);
                double beamDispersionEffect = CalculateBeamDispersion(txInfo, receiverInfo);

                double recvPower = txInfo.Power;
                recvPower *= txInfo.Efficiency;
                recvPower *= receiverInfo.Efficiency;
                recvPower *= wavelengthMismatchEffect;
                recvPower *= beamDispersionEffect;

                recvPowers.Add(transmitterId, (float)recvPower);
            }
            return recvPowers;
        }

        private static double CalculateWavelengthMismatch(GenericUtils.TransmitterInfo transmitterInfo, GenericUtils.ReceiverInfo receiverInfo)
        {
            // Issue 3: Handle division-by-zero for TuningFactor.
            if (receiverInfo.TuningFactor == 0.0)
            {
                // A tuning factor of 0 means perfect precision is required.
                // Return 1.0 if they match exactly, 0.0 otherwise.
                return transmitterInfo.Wavelength == receiverInfo.Wavelength ? 1.0 : 0.0;
            }
            if (transmitterInfo.Wavelength <= 0 || receiverInfo.Wavelength <= 0)
            {
                return 0.0; // Log of zero or negative is undefined.
            }

            double mismatch = Math.Log(transmitterInfo.Wavelength / receiverInfo.Wavelength);
            double numerator = -1 * Math.Pow(mismatch, 2);
            double denominator = 2 * Math.Pow(receiverInfo.TuningFactor, 2);
            double resultEfficiency = Math.Exp(numerator / denominator);
            return resultEfficiency;
        }

        private static double CalculateBeamDispersion(GenericUtils.TransmitterInfo transmitterInfo, GenericUtils.ReceiverInfo receiverInfo)
        {
            // Issue 3: Handle division-by-zero for Area.
            if (transmitterInfo.Area <= 0)
            {
                return 0.0; // A zero-area transmitter cannot focus a beam.
            }

            double distance = Vector3d.Distance(transmitterInfo.parentProtoVessel.position, receiverInfo.parentProtoVessel.position);

            // Issue 3: Handle zero-distance case.
            if (distance == 0.0)
            {
                return 1.0; // If they are at the same spot, geometric efficiency is 100%.
            }

            // Issue 2: Corrected diameter calculation (Radius * 2).
            double transmitterDiameter = 2 * Math.Sqrt(transmitterInfo.Area / Math.PI);

            double divergenceAngleRadians = 1.22 * (transmitterInfo.Wavelength / transmitterDiameter);
            double beamSpotRadius = Math.Tan(divergenceAngleRadians) * distance;
            double beamSpotArea = Math.PI * Math.Pow(beamSpotRadius, 2);

            if (beamSpotArea == 0.0)
            {
                // Should not happen with distance check, but good for robustness.
                return 0.0;
            }

            return Math.Min(1.0, receiverInfo.Area / beamSpotArea);
        }
    }
}
