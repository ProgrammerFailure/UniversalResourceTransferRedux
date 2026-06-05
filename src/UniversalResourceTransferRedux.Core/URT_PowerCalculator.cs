using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalResourceTransferRedux.Core.RegistryComponents;
using static UniversalResourceTransferRedux.Core.GenericUtils;

namespace UniversalResourceTransferRedux.Core
{
    internal static class URT_PowerCalculator
    {
        public static (double, double) CalculateConstantLinkFactor( //Double 1: beam mismatch precursor, double 2, max efficiency
            GenericUtils.TransmitterInfo txInfo,
            GenericUtils.ReceiverInfo rxInfo
            )
        {
            if (rxInfo.TuningFactor == 0.0)
            {
                // A tuning factor of 0 means perfect precision is required.
                return (txInfo.Wavelength == rxInfo.Wavelength ? 1.0 : 0.0, rxInfo.Efficiency * txInfo.Efficiency);
            }
            if (txInfo.Wavelength <= 0 || rxInfo.Wavelength <= 0)
            {
                return (0.0, txInfo.Efficiency * rxInfo.Efficiency); //Avoided undefined errors
            }
            var mismatch = Math.Log(txInfo.Wavelength / rxInfo.Wavelength);
            var numerator = -1 * Math.Pow(mismatch, 2);
            var denominator = 2 * Math.Pow(rxInfo.TuningFactor, 2);
            var wavelengthMismatchEfficiency = Math.Exp(numerator / denominator);

            if (txInfo.Diameter <= 0)
            {
                return (0.0, txInfo.Efficiency * rxInfo.Efficiency * wavelengthMismatchEfficiency); // A zero-area transmitter cannot focus a beam.
            }
            double divergenceAngleRadians = txInfo.DiffractionConstant * (txInfo.Wavelength / txInfo.Diameter);
            double beamSpotAreaPrecursor = Math.PI * Math.Pow(Math.Tan(divergenceAngleRadians), 2);
            double beamMismatchPrecursor = (Math.Pow((rxInfo.diameter) / 2, 2) * Math.PI) / beamSpotAreaPrecursor;

            return (beamMismatchPrecursor, wavelengthMismatchEfficiency * txInfo.Efficiency * rxInfo.Efficiency);

            /* 
            Original code:
            double beamSpotRadius = Math.Tan(divergenceAngleRadians) * distance;
            double beamSpotArea = Math.PI * Math.Pow(beamSpotRadius, 2);

            beamSpotRadius^2 = Math.Tan(divergenceAngleRadians)^2 * distance^2
            thus we need only multiply this by distance^2, which is the sqrMagnitude of a vector!

            Now, beamMismatchEfficiency = receiverArea / beamSpotArea
                                        = pi(receiverDiameter/2)^2 / (beamSpotPrecursor * distance^2)
            since here we divide by the precursor, later we can just divide the product by distance squared!
            */

        }
        //Returns dictionary transmitterId, transmittedPower, receiverId, receivedPower
        public static void ProcessLinks(
            List<(URT_Link, double)> links, //The double is how much power is BEING RECEIVED through this link
            Dictionary<int, double> transmitterMaxPowers,
            Dictionary<int, double> receiverReceivedPowers,
            Dictionary<int, double> transmitterTransmittedAmounts,
            Dictionary<int, double> tempReceiverDict,
            System.Collections.Generic.Dictionary<int, uint>.KeyCollection receiverIds,
            System.Collections.Generic.Dictionary<int, uint>.KeyCollection transmitterIds
        )
        {
            tempReceiverDict.Clear();
            foreach (var a in receiverIds) tempReceiverDict[a] = 0.0;
            foreach (var a in transmitterIds) transmitterTransmittedAmounts[a] = 0.0;
            var registry = URT_Registry.Instance;
            foreach (var linkAndPower in links)
            {
                (var link, var power) = linkAndPower;
                var txPos = registry.GetTransmitterWorldPos(link.TransmitterId);
                var rxPos = registry.GetReceiverWorldPos(link.ReceiverId);
                if (!txPos.HasValue || !rxPos.HasValue)
                {
                    transmitterTransmittedAmounts[link.TransmitterId] = 0;
                    continue;
                }

                var opticalEfficiency = Math.Min(1, link.ConstantLinkFactor / ((txPos.Value - rxPos.Value).sqrMagnitude));
                var efficiency = opticalEfficiency * link.MaxEfficiencyLimit;
                var maxReceivedPower = efficiency * transmitterMaxPowers[link.TransmitterId];
                if (maxReceivedPower < power)
                {
                    transmitterTransmittedAmounts[link.TransmitterId] = transmitterMaxPowers[link.TransmitterId];
                    tempReceiverDict[link.ReceiverId] += maxReceivedPower;
                }
                else
                {
                    transmitterTransmittedAmounts[link.TransmitterId] = power / efficiency;
                    tempReceiverDict[link.ReceiverId] += power;
                }
            }
            receiverReceivedPowers.Clear();
            foreach (var pair in tempReceiverDict)
            {
                receiverReceivedPowers[pair.Key] = pair.Value;
            }
        }
    }


}
