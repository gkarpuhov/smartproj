using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Smartproj.Utils
{
    [StructLayout(LayoutKind.Explicit)]
    public struct WordToByteStruct
    {
        [FieldOffset(0)]
        public UInt16 Value;

        [FieldOffset(0)]
        public byte B1;

        [FieldOffset(1)]
        public byte B2;
    }
    public static class Number
    {
        public static byte[] WordToByteArray(ushort _int16)
        {
            WordToByteStruct wordToByte;
            wordToByte.B1 = 0;
            wordToByte.B2 = 0;
            wordToByte.Value = _int16;

            return new byte[] { wordToByte.B1, wordToByte.B2 };
        }
    }
}
