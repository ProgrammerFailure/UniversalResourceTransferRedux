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
    }
}