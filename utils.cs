using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace UniversalResourceTransferRedux
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
            public readonly float Area;
            public readonly float Wavelength;
            public readonly float Efficiency;
            public readonly ProtoVessel parentProtoVessel;
            public readonly List<int> pairedTransmitters;
            public readonly bool isReceiving;
            public readonly double TuningFactor;

            private ReceiverInfo(float area, float wavelength, float efficiency, ProtoVessel vessel, List<int> pairs, bool receiving, double tuningFactor)
            {
                Area = area;
                Wavelength = wavelength;
                Efficiency = efficiency;
                parentProtoVessel = vessel;
                pairedTransmitters = pairs;
                isReceiving = receiving;
                TuningFactor = tuningFactor;
            }

            /// <summary>
            /// Factory method to create a valid, fully initialized ReceiverInfo struct.
            /// </summary>
            public static ReceiverInfo Create(float area, float wavelength, float efficiency, ProtoVessel vessel, List<int> pairs, bool receiving, double tuningFactor)
            {
                if (vessel == null)
                {
                    throw new ArgumentNullException(nameof(vessel), "A ReceiverInfo snapshot cannot be created without a parent ProtoVessel.");
                }
                return new ReceiverInfo(area, wavelength, efficiency, vessel, pairs ?? new List<int>(), receiving, tuningFactor);
            }
        }

        public struct TransmitterInfo
        {
            public readonly float Area;
            public readonly float Wavelength;
            public readonly float Efficiency;
            public readonly float Power;
            public readonly bool isTransmitting;
            public readonly ProtoVessel parentProtoVessel;

            private TransmitterInfo(float area, float wavelength, float efficiency, ProtoVessel vessel, float power, bool transmitting)
            {
                Area = area;
                Wavelength = wavelength;
                Efficiency = efficiency;
                parentProtoVessel = vessel;
                isTransmitting = transmitting;
                Power = power;
            }

            /// <summary>
            /// Factory method to create a valid, fully initialized TransmitterInfo struct.
            /// </summary>
            public static TransmitterInfo Create(float area, float wavelength, float efficiency, ProtoVessel vessel, float power, bool transmitting)
            {
                if (vessel == null)
                {
                    throw new ArgumentNullException(nameof(vessel), "A TransmitterInfo snapshot cannot be created without a parent ProtoVessel.");
                }
                return new TransmitterInfo(area, wavelength, efficiency, vessel, power, transmitting);
            }
        }
    }
}
