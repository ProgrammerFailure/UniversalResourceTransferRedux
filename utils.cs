using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace UniversalResourceTransferRedux
{
    using Microsoft.Win32;
    using System.Globalization;
    using UnityEngine;

    /// <summary>
    /// A static utility class containing extension methods for the ConfigNode class.
    /// This provides a more convenient and safe way to parse common data types
    /// from ConfigNode values, avoiding repetitive parsing and error-handling code.
    /// </summary>
    public static class ConfigNodeUtils
    {
        #region Integer
        /// <summary>
        /// Reads an integer value from the node.
        /// </summary>
        /// <param name="name">The name of the value to read.</param>
        /// <returns>The parsed integer value.</returns>
        /// <exception cref="System.Exception">Throws if the value does not exist or cannot be parsed.</exception>
        public static int GetInt(this ConfigNode node, string name)
        {
            return int.Parse(node.GetValue(name));
        }

        /// <summary>
        /// Safely reads an integer value from the node.
        /// </summary>
        /// <param name="name">The name of the value to read.</param>
        /// <param name="defaultValue">The value to return if the specified value does not exist or fails to parse.</param>
        /// <returns>The parsed integer, or the default value on failure.</returns>
        public static int GetInt(this ConfigNode node, string name, int defaultValue)
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
        /// Reads a float value from the node using culture-invariant parsing.
        /// </summary>
        /// <param name="name">The name of the value to read.</param>
        /// <returns>The parsed float value.</returns>
        /// <exception cref="System.Exception">Throws if the value does not exist or cannot be parsed.</exception>
        public static float GetFloat(this ConfigNode node, string name)
        {
            return float.Parse(node.GetValue(name), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Safely reads a float value from the node using culture-invariant parsing.
        /// </summary>
        /// <param name="name">The name of the value to read.</param>
        /// <param name="defaultValue">The value to return if the specified value does not exist or fails to parse.</param>
        /// <returns>The parsed float, or the default value on failure.</returns>
        public static float GetFloat(this ConfigNode node, string name, float defaultValue)
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
        /// Reads a double value from the node using culture-invariant parsing.
        /// </summary>
        /// <param name="name">The name of the value to read.</param>
        /// <returns>The parsed double value.</returns>
        /// <exception cref="System.Exception">Throws if the value does not exist or cannot be parsed.</exception>
        public static double GetDouble(this ConfigNode node, string name)
        {
            return double.Parse(node.GetValue(name), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Safely reads a double value from the node using culture-invariant parsing.
        /// </summary>
        /// <param name="name">The name of the value to read.</param>
        /// <param name="defaultValue">The value to return if the specified value does not exist or fails to parse.</param>
        /// <returns>The parsed double, or the default value on failure.</returns>
        public static double GetDouble(this ConfigNode node, string name, double defaultValue)
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
        /// Reads a boolean value from the node.
        /// </summary>
        /// <param name="name">The name of the value to read.</param>
        /// <returns>The parsed boolean value.</returns>
        /// <exception cref="System.Exception">Throws if the value does not exist or cannot be parsed.</exception>
        public static bool GetBool(this ConfigNode node, string name)
        {
            return bool.Parse(node.GetValue(name));
        }

        /// <summary>
        /// Safely reads a boolean value from the node.
        /// </summary>
        /// <param name="name">The name of the value to read.</param>
        /// <param name="defaultValue">The value to return if the specified value does not exist or fails to parse.</param>
        /// <returns>The parsed boolean, or the default value on failure.</returns>
        public static bool GetBool(this ConfigNode node, string name, bool defaultValue)
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
        public static string GetString(this ConfigNode node, string name, string defaultValue)
        {
            if (node.HasValue(name))
            {
                return node.GetValue(name);
            }
            return defaultValue;
        }
        #endregion
    }

    public class GenericUtils
    {
        /// <summary>
        /// A lightweight struct to hold a snapshot of a target receiver's properties.
        /// As a struct, it's a value type, meaning copies are created on assignment,
        /// ensuring that modifying this data does not affect the original receiver.
        /// </summary>
        /// 
        public struct ReceiverInfo
        {
            /// <summary>
            /// The surface area of the receiver.
            /// </summary>
            public float Area;

            /// <summary>
            /// The tuned wavelength of the receiver.
            /// </summary>
            public float Wavelength;

            /// <summary>
            /// The power conversion efficiency of the receiver.
            /// </summary>
            public float Efficiency;

            public ProtoVessel parentProtoVessel;

            public List<int> pairedTransmitters;

            public ReceiverInfo(float _Area, float _Wavelength, float _Efficiency, ProtoVessel _parentProtoVessel, List<int> _pairedTransmitters)
            {
                Area = _Area;
                Wavelength = _Wavelength;
                Efficiency = _Efficiency;
                parentProtoVessel = _parentProtoVessel;
                pairedTransmitters = _pairedTransmitters;
            }

        }

        public struct TransmitterInfo
        {
            /// <summary>
            /// The surface area of the transmitter.
            /// </summary>
            public float Area;
            /// <summary>
            /// The tuned wavelength of the transmitter.
            /// </summary>
            public float Wavelength;
            /// <summary>
            /// The power conversion efficiency of the transmitter.
            /// </summary>
            public float Efficiency;
            ///<summary>
            /// The current input power of the transmitter
            ///</summary>
            public float Power;
            ///<summary>
            /// Whether the transmitter is transmitting
            /// </summary>
            public bool isTransmitting;
            ///<summary>
            ///The parent vessel of the transmitter
            /// </summary>
            public ProtoVessel parentProtoVessel;
            ///<summary>
            ///The receiverId of the target receiver of the transmitter
            ///</summary>
            public TransmitterInfo(float _Area, float _Wavelength, float _Efficiency, ProtoVessel _parentProtoVessel, float _Power, bool _isTransmitting)
            {
                Area = _Area;
                Wavelength = _Wavelength;
                Efficiency = _Efficiency;
                parentProtoVessel = _parentProtoVessel;
                isTransmitting = _isTransmitting;
                Power = _Power;
            }
        }
    }
}