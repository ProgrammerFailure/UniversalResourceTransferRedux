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
            List<URT_ActiveLink> links, //The double is how much power is INTENDED TO BE RECEIVED through this link
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
                var link = linkAndPower.Link;
                var power = linkAndPower.ReceivedPower;
                var txPos = registry.GetTransmitterWorldPos(link.TransmitterId);
                var rxPos = registry.GetReceiverWorldPos(link.ReceiverId);
                if (!txPos.HasValue || !rxPos.HasValue)
                {
                    transmitterTransmittedAmounts[link.TransmitterId] = 0;
                    continue;
                }

                var opticalEfficiency = Math.Min(1, link.ConstantLinkFactor / ((txPos.Value - rxPos.Value).sqrMagnitude));
                var efficiency = opticalEfficiency * link.MaxEfficiencyLimit * linkAndPower.OcclusionImpact;
                var maxReceivedPower = efficiency * transmitterMaxPowers[link.TransmitterId];
                if (efficiency <= 1e-5)
                {
                    transmitterTransmittedAmounts[link.TransmitterId] = 0.0;
                }
                else if (maxReceivedPower < power)
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

        /*
         * Pseudocode:
         * Given two vectors and a lowest shared parent:
         * Find the line between the two vectors
         * Test occlusion with the lowest shared parent's physical radius
         * For each child with no children, check intersection
         * For each child with children, check SOI intersection
         * If it has an SOI intersection, call the function on that again
         */

        public static double OcclusionImpact(Vector3d a,
            Vector3d b,
            CelestialBody lowestSharedParent,
            double atmoAttenuationCoeff)
        {
            var dists = SqrDistSegmentAndLine(a, b, lowestSharedParent.position);
            return OcclusionImpact(a, b, lowestSharedParent, dists.Item1, dists.Item2, atmoAttenuationCoeff);
        }
        private static double OcclusionImpact(
    Vector3d a,
    Vector3d b,
    CelestialBody lowestSharedParent,
    double sqrDistSegment,
    double sqrDistLine,
    double atmoAttenuationCoeff
)
        {
            var registryBodyData = URT_Registry.BodySquaredRadiiAndAtmoRadii[lowestSharedParent.flightGlobalsIndex];
            Vector3d planetPos = lowestSharedParent.position;
            Vector3d beamVec = b - a;

            // Determine if the closest approach of the infinite line lies physically between the two vessels
            bool isPlanetBetween = Vector3d.Dot(a - planetPos, beamVec) * Vector3d.Dot(b - planetPos, beamVec) < 0;
            /*
#if DEBUG
            if (lowestSharedParent.name == "Kerbin")
            {
                double realDist = Math.Sqrt(sqrDistLine);
                UnityEngine.Debug.Log($"[URT] Occlusion Test: Planet={lowestSharedParent.name}, " +
                                      $"txPos={a}, rxPos={b}, planetPos={planetPos}, " +
                                      $"d_min={realDist:F1}m, Radius={lowestSharedParent.Radius:F1}m, " +
                                      $"isBetween={isPlanetBetween}, sqrDistLine={sqrDistLine:F1}, " +
                                      $"limit={registryBodyData.SquaredBodyRadius:F1}");
            }
#endif
*/
            if (isPlanetBetween && sqrDistLine <= registryBodyData.SquaredBodyRadius)
            {
                return 0;
            }

            // Defensive check: If either vessel is glitched/spawned deep inside the core (more than 100m below sea level)
            double clearanceRadiusSqr = (lowestSharedParent.Radius - 100.0) * (lowestSharedParent.Radius - 100.0);
            if ((a - planetPos).sqrMagnitude < clearanceRadiusSqr || (b - planetPos).sqrMagnitude < clearanceRadiusSqr)
            {
                return 0;
            }

            double maxEff = 1.0;
            if (lowestSharedParent.atmosphere && sqrDistSegment <= registryBodyData.SquaredBodyAtmoTotalRadius)
            {
                double altitude = Math.Sqrt(sqrDistSegment) - lowestSharedParent.Radius;
                var atmoImpact = AtmosphereImpact(a, b, sqrDistLine, sqrDistSegment, lowestSharedParent, atmoAttenuationCoeff, registryBodyData);
                maxEff *= atmoImpact;
                if (maxEff <= 1e-5) return 0;
            }

            foreach (var body in lowestSharedParent.orbitingBodies)
            {
                var dists = SqrDistSegmentAndLine(a, b, body.position);
                var tempSquaredDistToBody = dists.Item1;
                var tempRegistryBodyData = URT_Registry.BodySquaredRadiiAndAtmoRadii[body.flightGlobalsIndex];

                if (body.orbitingBodies.Count == 0)
                {
                    Vector3d childPos = body.position;
                    bool isChildBetween = Vector3d.Dot(a - childPos, beamVec) * Vector3d.Dot(b - childPos, beamVec) < 0;

                    if (isChildBetween && dists.Item2 <= tempRegistryBodyData.SquaredBodyRadius) return 0.0;

                    double childClearance = (body.Radius - 100.0) * (body.Radius - 100.0);
                    if ((a - childPos).sqrMagnitude < childClearance || (b - childPos).sqrMagnitude < childClearance) return 0.0;

                    if (!body.atmosphere) continue;
                    if (tempSquaredDistToBody > tempRegistryBodyData.SquaredBodyAtmoTotalRadius) continue;

                    var atmoImpact = AtmosphereImpact(
                        a,
                        b,
                        dists.Item2,
                        dists.Item1,
                        body,
                        atmoAttenuationCoeff,
                        tempRegistryBodyData
                    );
                    maxEff *= atmoImpact;
                    if (maxEff <= 1e-5) return 0;
                }
                else if (tempSquaredDistToBody <= tempRegistryBodyData.SquaredSOIRadius)
                {
                    maxEff *= OcclusionImpact(a, b, body, dists.Item1, dists.Item2, atmoAttenuationCoeff);
                    if (maxEff <= 1e-5) return 0;
                }
            }
            return maxEff;
        }
        
        private static (double, double) SqrDistSegmentAndLine(Vector3d a, Vector3d b, Vector3d point)
        {
            Vector3d ab = b - a;
            Vector3d ac = point - a;

            double dotAB_AB = Vector3d.Dot(ab, ab);

            // Prevent division by zero if the transmitter and receiver are at the exact same location
            if (dotAB_AB < 1e-9)
            {
                return ((a - point).sqrMagnitude, (a - point).sqrMagnitude);
            }

            // Project the planet onto the beam line to find the percentage 't' along the segment
            double dotAC_AB = Vector3d.Dot(ac, ab);
            double t = dotAC_AB / dotAB_AB;
            double tClamped;
            // Clamp 't' to the boundaries of the segment [0.0, 1.0]
            if (t < 0.0) tClamped = 0.0;
            else if (t > 1.0) tClamped = 1.0;
            else tClamped = t;

            double acSqr = ac.sqrMagnitude;
            double sqrDistSegment = acSqr + tClamped * (tClamped * dotAB_AB - 2.0 * dotAC_AB);
            double sqrDistLine = acSqr - t * dotAC_AB;

            return (sqrDistSegment, sqrDistLine);
        }

        private static double AtmosphereImpact(Vector3d a, Vector3d b, double minSqrDistLine, double minSqrDistSegment, CelestialBody body, double atmoAttenuationCoeff, URT_BodyValues bodyData)
        {
            double altitude = Math.Sqrt(minSqrDistSegment) - body.Radius;
            double rho_hmin = GetDensityAtAltitude(altitude, bodyData.ASLDensity, bodyData.ScaleHeight);
            var columnDensity = CalculateColumnDensity(
                a - body.position,
                b - body.position,
                bodyData.ScaleHeight,
                rho_hmin,
                minSqrDistLine,
                body.atmosphereDepth + body.Radius,
                body.Radius,
                body.atmDensityASL
            );

            double opticalDepth = columnDensity * atmoAttenuationCoeff;
            
            //Beer-Lambert
            return Math.Exp(-opticalDepth);

        }
    }
}
