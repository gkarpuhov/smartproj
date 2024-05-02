using lcmsNET;
using System.Runtime.InteropServices;
using System;
using System.Drawing;

namespace Smartproj.Utils
{
    public static class CMS
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct ARGBStruct
        {
            [FieldOffset(0)]
            public UInt32 Value;
            [FieldOffset(0)]
            public byte A;
            [FieldOffset(1)]
            public byte R;
            [FieldOffset(2)]
            public byte G;
            [FieldOffset(3)]
            public byte B;
        }
        public static byte[] ARGBToByteArray(UInt32 _int32)
        {
            ARGBStruct toByte;
            toByte.A = 0;
            toByte.R = 0;
            toByte.G = 0;
            toByte.B = 0;
            toByte.Value = _int32;

            return new byte[] { toByte.A, toByte.R, toByte.G, toByte.B };
        }
        public static Color FromUint32(this UInt32 _int32)
        {
            var array = ARGBToByteArray(_int32);
            return Color.FromArgb(array[0], array[1], array[2], array[3]);
        }
        public static UInt32 ToUint32(this Color _color)
        {
            return BitConverter.ToUInt32(new byte[] { _color.A, _color.R, _color.G, _color.B }, 0);
        }
        public static bool CompareProfiles(Profile _x, Profile _y)
        {
            if (_x != null && _y != null && _x.ColorSpace == _y.ColorSpace && _x.HeaderProfileID.Length > 0 && _x.HeaderProfileID.Length == _y.HeaderProfileID.Length)
            {
                bool itemnotnul = false;
                for (int i = 0; i < _x.HeaderProfileID.Length; i++)
                {
                    if (_x.HeaderProfileID[i] != 0) itemnotnul = true;
                    if (_x.HeaderProfileID[i] != _y.HeaderProfileID[i])
                    {
                        return false;
                    }
                }
                if (!itemnotnul)
                {
                    string xdescription = _x.GetProfileInfo(InfoType.Description, "en", "US");
                    string ydescription = _y.GetProfileInfo(InfoType.Description, "en", "US");
                    if (xdescription == "" || xdescription != ydescription || _x.Version != _y.Version)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
