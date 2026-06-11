using Contracts.Agents.Mentalities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace UniversalResourceTransferRedux.Core
{
    /// <summary>
    /// A static utility class containing extension methods for the ConfigNode class.
    /// This provides a more convenient and safe way to parse common data types
    /// from ConfigNode values, avoiding repetitive parsing and error-handling code.
    /// </summary>
    public static class ConfigNodeUtils
    {
        #region Integer
        /// <summary>
        /// Safely reads an integer value from the node.
        /// </summary>
        /// <param name="name">The name of the value to read.</param>
        /// <param name="defaultValue">The value to return if the specified value does not exist or fails to parse.</param>
        /// <returns>The parsed integer, or the default value on failure.</returns>
        public static int GetInt(this ConfigNode node, string name, int defaultValue = 0)
        {
            if (node.HasValue(name))
            {
                if (int.TryParse(node.GetValue(name), out int result))
                {
                    return result;
                }
            }
            return defaultValue;
        }
        #endregion

        #region Float
        /// <summary>
        /// Safely reads a float value from the node using culture-invariant parsing.
        /// </summary>
        /// <param name="name">The name of the value to read.</param>
        /// <param name="defaultValue">The value to return if the specified value does not exist or fails to parse.</param>
        /// <returns>The parsed float, or the default value on failure.</returns>
        public static float GetFloat(this ConfigNode node, string name, float defaultValue = 0f)
        {
            if (node.HasValue(name))
            {
                if (float.TryParse(node.GetValue(name), NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
                {
                    return result;
                }
            }
            return defaultValue;
        }
        #endregion

        #region Double
        /// <summary>
        /// Safely reads a double value from the node using culture-invariant parsing.
        /// </summary>
        /// <param name="name">The name of the value to read.</param>
        /// <param name="defaultValue">The value to return if the specified value does not exist or fails to parse.</param>
        /// <returns>The parsed double, or the default value on failure.</returns>
        public static double GetDouble(this ConfigNode node, string name, double defaultValue = 0.0)
        {
            if (node.HasValue(name))
            {
                if (double.TryParse(node.GetValue(name), NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                {
                    return result;
                }
            }
            return defaultValue;
        }
        #endregion

        #region Boolean
        /// <summary>
        /// Safely reads a boolean value from the node.
        /// </summary>
        /// <param name="name">The name of the value to read.</param>
        /// <param name="defaultValue">The value to return if the specified value does not exist or fails to parse.</param>
        /// <returns>The parsed boolean, or the default value on failure.</returns>
        public static bool GetBool(this ConfigNode node, string name, bool defaultValue = false)
        {
            if (node.HasValue(name))
            {
                if (bool.TryParse(node.GetValue(name), out bool result))
                {
                    return result;
                }
            }
            return defaultValue;
        }
        #endregion

        #region String
        /// <summary>
        /// Safely reads a string value from the node. This is mainly useful for providing a default
        /// value if the key does not exist, as GetValue returns null in that case.
        /// </summary>
        /// <param name="name">The name of the value to read.</param>
        /// <param name="defaultValue">The value to return if the specified value does not exist.</param>
        /// <returns>The string, or the default value on failure.</returns>
        public static string GetString(this ConfigNode node, string name, string defaultValue = "")
        {
            if (node.HasValue(name))
            {
                return node.GetValue(name);
            }
            return defaultValue;
        }
        #endregion
    }

    public static class GenericUtils
    {
        public struct ReceiverInfo
        {
            public readonly float diameter;
            public readonly float Wavelength;
            public readonly float Efficiency;
            public readonly double TuningFactor;
            public readonly string[] ResourceTypeTags;

            public ReceiverInfo(float diameter, float wavelength, float efficiency, double tuningFactor, string[] resourceTypeTags)
            {
                this.diameter = diameter;
                Wavelength = wavelength;
                Efficiency = efficiency;
                TuningFactor = tuningFactor;
                ResourceTypeTags = resourceTypeTags;
            }

            public override string ToString()
            {
                return $"ReceiverInfo(Diameter: {diameter}, Wavelength: {Wavelength}, Efficiency: {Efficiency}, TuningFactor: {TuningFactor})";
            }
        }

        public struct TransmitterInfo
        {
            public readonly float Diameter;
            public readonly float Wavelength;
            public readonly float Efficiency;
            public readonly float MaxPower;
            public readonly string[] ResourceTypeTags;
            public readonly float DiffractionConstant;
            public TransmitterInfo(float diameter, float wavelength, float efficiency, float maxPower, string[] resourceTypeTags, float diffractionConstant)
            {
                Diameter = diameter;
                Wavelength = wavelength;
                Efficiency = efficiency;
                MaxPower = maxPower;
                ResourceTypeTags = resourceTypeTags;
                DiffractionConstant = diffractionConstant;
            }

            public override string ToString()
            {
                return $"TransmitterInfo(Diameter: {Diameter}, Wavelength: {Wavelength}, Efficiency: {Efficiency}, MaxPower: {MaxPower})";
            }
        }

        public static double CalculateBaseScaleHeight(CelestialBody body)
        {
            // This code comes from the following formula
            // H = RT / Mg
            // Where H = scale height, R = ideal gas constant, T = temp in kelvins,
            // M = mean molar mass of gas particles, g = acceleration due to gravity at that location
            // https://en.wikipedia.org/wiki/Scale_height
            if (!body.atmosphere) return 0.0;

            double r_asl = body.Radius;
            double g_asl = body.gravParameter / (r_asl * r_asl);

            double T_asl = body.atmosphereTemperatureSeaLevel;
            if (T_asl <= 0)
            {
                T_asl = body.GetTemperature(0.0);
            }

            double R_universal = PhysicsGlobals.IdealGasConstant;
            double molarMass = body.atmosphereMolarMass;

            if (molarMass <= 0 || g_asl <= 0) return 5000.0; // Safety fallback

            return (R_universal * T_asl) / (g_asl * molarMass);
        }
        public static CelestialBody FindLowestSharedParent(CelestialBody a, CelestialBody b)
        {
            if (a == b) return a;

            var sun = Planetarium.fetch.Sun;
            if (sun == a || sun == b) return sun;

            if (a.HasParent(b)) return b;
            if (b.HasParent(a)) return a;


            return FindLowestSharedParent(a.referenceBody, b);
        }

        public static Vector3d? GetProtoVesselWorldPosAtTime(ProtoVessel pv, double universalTime)
        {
            if (pv.vesselRef != null)
            {
                return pv.vesselRef.GetWorldPos3D();
            }

            var referenceBody = FlightGlobals.Bodies[pv.orbitSnapShot.ReferenceBodyIndex];

            if (pv.landed || pv.splashed)
            {
                return referenceBody.GetWorldSurfacePosition(pv.latitude, pv.longitude, pv.altitude);
            }
            LoadSnapshotIntoTemp(pv.orbitSnapShot, referenceBody);

            Vector3d relativePosition = tempOrbit.getPositionAtUT(universalTime);
            return relativePosition + referenceBody.position;
        }

        private static Orbit tempOrbit = new Orbit();

        private static void LoadSnapshotIntoTemp(OrbitSnapshot snap, CelestialBody referenceBody)
        {
            tempOrbit.inclination = snap.inclination;
            tempOrbit.eccentricity = snap.eccentricity;
            tempOrbit.semiMajorAxis = snap.semiMajorAxis;
            tempOrbit.LAN = snap.LAN;
            tempOrbit.argumentOfPeriapsis = snap.argOfPeriapsis;
            tempOrbit.meanAnomalyAtEpoch = snap.meanAnomalyAtEpoch;
            tempOrbit.epoch = snap.epoch;
            tempOrbit.referenceBody = referenceBody;

            // Recalculates internal orbital constants (period, mean motion, etc.)
            tempOrbit.Init();
        }
        // Fast approximation of the Error Function (Abramowitz & Stegun)
        private static double Erf(double x)
        {
            // Save the sign of x
            double sign = (x < 0) ? -1.0 : 1.0;
            double absX = Math.Abs(x);

            // Constants
            const double a1 = 0.254829592;
            const double a2 = -0.284496736;
            const double a3 = 1.421413741;
            const double a4 = -1.453152027;
            const double a5 = 1.061405429;
            const double p = 0.3275911;

            double t = 1.0 / (1.0 + p * absX);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-absX * absX);

            return sign * y;
        }

        public static double GetDensityAtAltitude(CelestialBody body, double altitude)
        {
            double pressure = body.GetPressure(altitude);
            double temperature = body.GetTemperature(altitude);
            return body.GetDensity(pressure, temperature);
        }

        //Cheaper calculation based on the same approximation used in the Chapman function
        public static double GetDensityAtAltitude(double altitude, double seaLevelDensity, double scaleHeight)
        {
            if (scaleHeight <= 0 || seaLevelDensity <= 0) return 0.0;

            double safeAltitude = Math.Max(0.0, altitude);

            return seaLevelDensity * Math.Exp(-safeAltitude / scaleHeight);
        }

        /// <summary>
        /// Calculates the integrated atmospheric column density along a beam path.
        /// </summary>
        /// <param name="start">Transmitter position relative to planet center (meters)</param>
        /// <param name="end">Receiver position relative to planet center (meters)</param>
        /// <param name="scaleHeight">Atmospheric scale height H (meters)</param>
        /// <param name="rho_hmin">Atmospheric density at the closest approach altitude (kg/m^3)</param>
        /// <param name="d_min">Closest approach distance to planet center (meters)</param>
        /// <param name="r_atmo">Total radius of the atmosphere (meters)</param>
        /// <returns>Integrated atmospheric mass density along the path (kg/m^2)</returns>
        public static double CalculateColumnDensity(
        Vector3d start,
        Vector3d end,
        double scaleHeight,
        double rho_hmin,
        double d_min_square,
        double r_atmo,
        double r_body,
        double densityAsl)
        {
            /// <summary>
            /// Calculates the integrated atmospheric column density (slant mass) along a 3D beam segment.
            /// Physically, this quantity represents the total amount of matter the beam passed through
            /// 
            /// Workflow:
            /// 1. Simplification: 
            ///    The 3D coordinates are projected onto a 1D coordinate system along the infinite beam path. 
            ///    The point of closest approach to the planet's center is designated as the origin (s = 0).
            ///    This allows us to approach the problem by integrating along the one dimensional path,
            ///    NOT dealing with trigonometry using triangles with the closest approach point, planet center, and beam endpoints/
            ///    atmo intersectons
            /// 2. Active Path Clamping: 
            ///    The 1D segment boundaries [s_T, s_D] are clamped to the atmospheric shell boundary 
            ///    [-s_atmo, s_atmo], yielding the active atmospheric path [a, b].
            /// 3. Physical Regime Bifurcation:
            ///    - Steep Paths (d_min < r_body): 
            ///      When the line of sight passes deep inside the planet, the curvature is negligible relative to 
            ///      the steep radial climb. We model this as a local "flat-Earth" plane-parallel atmosphere, 
            ///      integrating exactly over the altitude gradient to yield the Slant-Exponential formula.
            ///    - Grazing Paths (d_min >= r_body): 
            ///      When the beam skims the atmosphere horizontally, planetary curvature is the primary limiting factor. 
            ///      The spherical shell is locally approximated as a parabola, casting the density integral into a 
            ///      Gaussian bell curve that is solved analytically using the Error Function (erf).
            /// 
            /// See attached .tex and pdf for details
            /// </summary>
            // 1. Calculate direction and length of the beam segment
            Vector3d pathVec = end - start;
            double pathLength = pathVec.magnitude;
            if (pathLength < 1e-3) return 0.0;

            Vector3d dir = pathVec / pathLength;

            // 2. Project endpoints onto the 1D line relative to closest approach (origin s=0)
            double s_T = Vector3d.Dot(start, dir);
            double s_D = Vector3d.Dot(end, dir);

            // 3. Defensive check: If the infinite line misses the atmosphere completely
            if (d_min_square >= r_atmo * r_atmo) return 0.0;

            // Find the atmospheric boundary limits along this 1D line
            double safe_d_min_square = Math.Max(0.0, d_min_square);
            double s_atmo = Math.Sqrt(r_atmo * r_atmo - safe_d_min_square);

            // 4. Clamp the endpoints to the atmosphere boundary
            double a = Math.Max(-s_atmo, Math.Min(s_T, s_D));
            double b = Math.Min(s_atmo, Math.Max(s_T, s_D));

            if (a >= b) return 0.0;

            // 5. Approximation 1: Steep/Vertical Path Fallback
            // If the closest approach of the infinite line is inside the physical body
            if (safe_d_min_square < r_body * r_body)
            {
                // Find the altitudes at the start and end of the active atmosphere segment
                double h_a = Math.Sqrt(safe_d_min_square + a * a) - r_body;
                double h_b = Math.Sqrt(safe_d_min_square + b * b) - r_body;

                double deltaH = Math.Abs(h_b - h_a);

                // If there is a meaningful vertical change, use the Slant-Exponential model
                if (deltaH > 1e-3)
                {
                    double rho_a = densityAsl * Math.Exp(-Math.Max(0.0, h_a) / scaleHeight);
                    double rho_b = densityAsl * Math.Exp(-Math.Max(0.0, h_b) / scaleHeight);

                    double activePathLength = Math.Abs(b - a);

                    return (activePathLength / deltaH) * scaleHeight * Math.Abs(rho_a - rho_b);
                }
                // If deltaH <= 1e-3, the path is nearly horizontal (grazing), 
                // so we fall through to the Error Function approximation below.
            }

            // 6. Approximation 2: Grazing Path (Chapman/Erf model)
            double d_min = Math.Sqrt(safe_d_min_square);
            double safe_d_min = Math.Max(d_min, 1.0);
            double sigma = Math.Sqrt(2.0 * scaleHeight * safe_d_min);
            double F_coefficient = rho_hmin * Math.Sqrt(Math.PI * scaleHeight * safe_d_min / 2.0);

            // Evaluate F(b) - F(a)
            double F_a = F_coefficient * Erf(a / sigma);
            double F_b = F_coefficient * Erf(b / sigma);

            return Math.Abs(F_b - F_a);
        }
    }
}