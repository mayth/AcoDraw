using System;
using System.Collections.Generic;
using System.Linq;

namespace AcoDraw
{
    /// <summary>
    /// Provides some utility methods.
    /// </summary>
    public static class Utility
    {
        /// <summary>Converts to an UInt16 value from a byte array that contains bytes with big-endian.</summary>
        /// <returns>A converted value.</returns>
        /// <param name='source'>A byte array</param>
        public static ushort ToUInt16(IEnumerable<byte> source)
        {
            return BitConverter.ToUInt16(source.Reverse().ToArray(), 0);
        }

        /// <summary>Converts to an Int16 value from a byte array that contains bytes with big-endian.</summary>
        /// <returns>A converted value.</returns>
        /// <param name='source'>A byte array</param>
        public static short ToInt16(IEnumerable<byte> source)
        {
            return BitConverter.ToInt16(source.Reverse().ToArray(), 0);
        }
    }
}

