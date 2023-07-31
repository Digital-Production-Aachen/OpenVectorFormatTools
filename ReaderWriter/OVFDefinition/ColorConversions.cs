using System;
using System.Drawing;

namespace OVFDefinition
{
    /// <summary>
    /// Converts between System.Drawing.Color and VectorBlockMetaData.display_color
    /// as specified by open_vector_format.proto.
    /// 
    /// The OVF display color is represented by an int32, interpreted as byte[4] with
    /// byte[0] = red, byte[1] = green, byte[2] = blue, byte[3] = alpha
    /// (assuming little-endian byte order).
    /// </summary>
    public class ColorConversions
    {
        public static uint ColorToUInt(Color color)
        {
            byte[] bytes = ColorToBytes(color);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public static Color UIntToColor(uint ovfColor)
        {
            byte[] bytes = BitConverter.GetBytes(ovfColor);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BytesToColor(bytes);
        }

        public static int ColorToInt(Color color)
        {
            byte[] bytes = ColorToBytes(color);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static Color IntToColor(int ovfColor)
        {
            byte[] bytes = BitConverter.GetBytes(ovfColor);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BytesToColor(bytes);
        }

        private static byte[] ColorToBytes(Color color)
        {
            byte[] bytes = new byte[4];
            bytes[0] = color.R;
            bytes[1] = color.G;
            bytes[2] = color.B;
            bytes[3] = color.A;
            return bytes;
        }

        private static Color BytesToColor(byte[] bytes)
        {
            return Color.FromArgb(alpha: bytes[3], red: bytes[0], green: bytes[1], blue: bytes[2]);
        }

    }
}
