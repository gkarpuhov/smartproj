using lcmsNET;
using System.Runtime.InteropServices;
using System;
using System.Drawing;
using GdPicture14;

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
    public static class ColorUtils
    {
        public static byte[] CopyToByteArray(this GdPictureImaging _image, int _id)
        {
            int pixelsH = _image.GetHeight(_id);
            int stride = _image.GetStride(_id);

            if (stride >= 0)
            {
                byte[] buffer = new byte[stride * pixelsH];
                Marshal.Copy(_image.GetBits(_id), buffer, 0, buffer.Length);
                return buffer;
            }
            else
            {
                byte[] buffer = new byte[-stride * pixelsH];
                Marshal.Copy(IntPtr.Add(_image.GetBits(_id), stride * (pixelsH - 1)), buffer, 0, buffer.Length);
                return buffer;
            }
        }
        public static void GetFromByteArray(this GdPictureImaging _image, int _id, byte[] _buffer)
        {
            int pixelsH = _image.GetHeight(_id);
            int stride = _image.GetStride(_id);

            if (stride >= 0)
            {
                Marshal.Copy(_buffer, 0, _image.GetBits(_id), _buffer.Length);
            }
            else
            {
                Marshal.Copy(_buffer, 0, IntPtr.Add(_image.GetBits(_id), stride * (pixelsH - 1)), _buffer.Length);
            }
        }
        public static Lab FromWordBufferToLab(this byte[] _buffer, int _index)
        {
            return ColorUtils.Lab.FromWordToLab(BitConverter.ToUInt16(_buffer, _index + 0), BitConverter.ToUInt16(_buffer, _index + 2), BitConverter.ToUInt16(_buffer, _index + 4));
        }
        public static bool IsSkinColor(this Lab _color)
        {
            LCH lch = _color.ToLCH();
            return (SkinC.Contains(lch.C) && SkinH.Contains(lch.H) && SkinL.Contains(lch.L)) || (SkinLowC.Contains(lch.C) && SkinLowH.Contains(lch.H) && SkinLowL.Contains(lch.L)) || (SkinDarkC.Contains(lch.C) && SkinDarkH.Contains(lch.H) && SkinDarkL.Contains(lch.L));
        }
        static ColorUtils()
        {
            SkinC = new Interval<float>(5.0f, 65f);
            SkinH = new Interval<float>(0f, 90f);
            SkinL = new Interval<float>(40f, 93f);

            SkinLowC = new Interval<float>(5.0f, 40f);
            SkinLowH = new Interval<float>(320f, 360f);
            SkinLowL = new Interval<float>(40f, 95f);

            SkinDarkC = new Interval<float>(10f, 40f);
            SkinDarkH = new Interval<float>(0f, 90f);
            SkinDarkL = new Interval<float>(10f, 39.999f);


            SkyC = new Interval<float>(5.0f, 10000f);
            SkyH = new Interval<float>(190f, 310f);
            SkyL = new Interval<float>(10f, 100f);

            GreenC = new Interval<float>(5.0f, 10000f);
            GreenH = new Interval<float>(90.001f, 189.99f);
            GreenL = new Interval<float>(10f, 100f);

            BlueRedC = new Interval<float>(5.0f, 10000f);
            BlueRedH = new Interval<float>(310.001f, 360f);
            BlueRedL = new Interval<float>(0f, 100f);

            BlackLab = 0.99f;
            WhiteLab = 99.8f;
            GrayDelta = 1.0f;
            SepiaDelta = 5.0f;
        }

        public static readonly Interval<float> SkinC;
        public static readonly Interval<float> SkinH;
        public static readonly Interval<float> SkinL;

        public static readonly Interval<float> SkinLowC;
        public static readonly Interval<float> SkinLowH;
        public static readonly Interval<float> SkinLowL;

        public static readonly Interval<float> SkinDarkC;
        public static readonly Interval<float> SkinDarkH;
        public static readonly Interval<float> SkinDarkL;

        public static readonly Interval<float> SkyC;
        public static readonly Interval<float> SkyH;
        public static readonly Interval<float> SkyL;

        public static readonly Interval<float> GreenC;
        public static readonly Interval<float> GreenH;
        public static readonly Interval<float> GreenL;

        public static readonly Interval<float> BlueRedC;
        public static readonly Interval<float> BlueRedH;
        public static readonly Interval<float> BlueRedL;

        public static readonly float BlackLab;
        public static readonly float WhiteLab;
        public static readonly float GrayDelta;
        public static readonly float SepiaDelta;
        public struct Pixel
        {
            public Pixel(byte[] _bit16Labbuffer, int _width, int _shift, byte _gray) : this(new Point((_shift % (_width * 6)) / 6, _shift / (_width * 6)), new RGB(), _bit16Labbuffer.FromWordBufferToLab(_shift), _gray)
            {
            }
            public Pixel(byte[] _Labbuffer, int _width, int _shift, RGB _rgb, Lab _lab, byte _gray) : this(new Point((_shift % (_width * 6)) / 6, _shift / (_width * 6)), _rgb, _Labbuffer.FromWordBufferToLab(_shift), _gray)
            {
            }
            public Pixel(Point _loc, RGB _rgb, Lab _lab, byte _gray)
            {
                Position = _loc;
                RGB = _rgb;
                Lab = _lab;
                Gray = _gray;
                LCH = Lab.ToLCH();
                Flag = ColorPixelFlagEnum.None;

                if (Lab.L >= WhiteLab)
                {
                    if (Lab.L >= WhiteLab) Flag = Flag | ColorPixelFlagEnum.White;
                }
                else
                {
                    if (LCH.C < GrayDelta)
                    {
                        Flag = Flag | ColorPixelFlagEnum.Gray;
                    }
                    else
                    {
                        if ((SkinC.Contains(LCH.C) && SkinH.Contains(LCH.H) && SkinL.Contains(LCH.L)) || (SkinLowC.Contains(LCH.C) && SkinLowH.Contains(LCH.H) && SkinLowL.Contains(LCH.L)) || (SkinDarkC.Contains(LCH.C) && SkinDarkH.Contains(LCH.H) && SkinDarkL.Contains(LCH.L)))
                        {
                            Flag = Flag | ColorPixelFlagEnum.Skin;
                        }
                        else
                        {
                            if (SkyC.Contains(LCH.C) && SkyH.Contains(LCH.H) && SkyL.Contains(LCH.L))
                            {
                                Flag = Flag | ColorPixelFlagEnum.Sky;
                            }
                            else
                            {
                                if (GreenC.Contains(LCH.C) && GreenH.Contains(LCH.H) && GreenL.Contains(LCH.L))
                                {
                                    Flag = Flag | ColorPixelFlagEnum.Green;
                                }
                                else
                                {
                                    if (BlueRedC.Contains(LCH.C) && BlueRedH.Contains(LCH.H) && BlueRedL.Contains(LCH.L))
                                    {
                                        Flag = Flag | ColorPixelFlagEnum.BlueRed;
                                    }
                                    else
                                    {
                                        Flag = Flag | ColorPixelFlagEnum.NoInterest;
                                    }
                                }
                            }
                        }
                        if (LCH.C < SepiaDelta)
                        {
                            Flag = Flag | ColorPixelFlagEnum.Sepia;
                        }
                    }
                }
            }

            public Point Position;
            public RGB RGB;
            public Lab Lab;
            public LCH LCH;
            public byte Gray;
            public ColorPixelFlagEnum Flag { get; set; }
        }
        public struct RGB
        {
            public RGB(byte _r, byte _g, byte _b)
            {
                R = _r;
                G = _g;
                B = _b;
            }
            public byte R;
            public byte G;
            public byte B;

            public static float DeltaRGBFromByteBuffer(byte[] _buffer, int _index1, int _index2)
            {
                int ret = 0;
                for (int i = 0; i < 3; i++)
                {
                    ret = ret + (_buffer[_index1 + i] - _buffer[_index2 + i]) * (_buffer[_index1 + i] - _buffer[_index2 + i]);
                }

                return (float)Math.Sqrt((double)ret);
            }
            public static float DeltaRGBFromByteBuffer(byte[] _buffer, int _x1, int _y1, int _x2, int _y2, int _stride)
            {
                int ret = 0;
                for (int i = 0; i < 3; i++)
                {
                    int rgbStartPosition1 = _y1 * _stride + _x1 * 3;
                    int rgbStartPosition2 = _y2 * _stride + _x2 * 3;
                    ret = ret + (_buffer[rgbStartPosition1 + i] - _buffer[rgbStartPosition2 + i]) * (_buffer[rgbStartPosition1 + i] - _buffer[rgbStartPosition2 + i]);
                }

                return (float)Math.Sqrt((double)ret);
            }
        }
        public struct Lab
        {
            public float L;
            public float a;
            public float b;

            public ushort L16;
            public ushort a16;
            public ushort b16;

            public byte L8;
            public byte a8;
            public byte b8;

            public bool CheckOfsRGBGamut()
            {
                return false;
            }
            public LCH ToLCH()
            {
                LCH ret = new LCH()
                {
                    L = L,
                    C = (float)Math.Sqrt(a * a + b * b)
                };
                double atan = Math.Atan2(b, a);
                ret.H = (float)((180d / Math.PI) * atan);
                if (atan < 0) ret.H = ret.H + 360f;
                return ret;
            }
            public void ToBufferFromFloat(byte[] _buffer, int _shift)
            {
                float L1 = (65535f * L) / 100f;
                float a1 = (a + 128f) * (65535f / 255f);
                float b1 = (b + 128f) * (65535f / 255f);

                if (L1 < 0f) L1 = 0f;
                if (L1 > 65535f) L1 = 65535f;
                if (a1 < 0f) a1 = 0f;
                if (a1 > 65535f) a1 = 65535f;
                if (b1 < 0f) b1 = 0f;
                if (b1 > 65535f) b1 = 65535f;

                byte[] Lbytes = Number.WordToByteArray(Convert.ToUInt16(L1));
                byte[] abytes = Number.WordToByteArray(Convert.ToUInt16(a1));
                byte[] bbytes = Number.WordToByteArray(Convert.ToUInt16(b1));

                _buffer[_shift + 0] = Lbytes[0];
                _buffer[_shift + 1] = Lbytes[1];
                _buffer[_shift + 2] = abytes[0];
                _buffer[_shift + 3] = abytes[1];
                _buffer[_shift + 4] = bbytes[0];
                _buffer[_shift + 5] = bbytes[1];
            }
            public void ToBufferFromWord(byte[] _buffer, int _shift)
            {
                byte[] Lbytes = Number.WordToByteArray(L16);
                byte[] abytes = Number.WordToByteArray(a16);
                byte[] bbytes = Number.WordToByteArray(b16);

                _buffer[_shift + 0] = Lbytes[0];
                _buffer[_shift + 1] = Lbytes[1];
                _buffer[_shift + 2] = abytes[0];
                _buffer[_shift + 3] = abytes[1];
                _buffer[_shift + 4] = bbytes[0];
                _buffer[_shift + 5] = bbytes[1];
            }
            public static Lab FromWordBufferToLab(byte[] _buffer, int _index)
            {
                return FromWordToLab(BitConverter.ToUInt16(_buffer, _index + 0), BitConverter.ToUInt16(_buffer, _index + 2), BitConverter.ToUInt16(_buffer, _index + 4));
            }
            public static Lab FromByteBufferToLab(byte[] _buffer, int _index)
            {
                return FromByteToLab(_buffer[_index + 0], _buffer[_index + 1], _buffer[_index + 2]);
            }
            public static Lab FromWordToLab(ushort _L, ushort _a, ushort _b)
            {
                Lab ret = new Lab();

                ret.L16 = _L;
                ret.a16 = _a;
                ret.b16 = _b;

                ret.L8 = (byte)Math.Round(((float)ret.L16 / 65535f) * 255f);
                ret.a8 = (byte)Math.Round(((float)ret.a16 / 65535f) * 255f);
                ret.b8 = (byte)Math.Round(((float)ret.b16 / 65535f) * 255f);

                ret.L = ((float)ret.L16 / 65535f) * 100f;
                ret.a = ((float)ret.a16 / 65535f) * 255f - 128f;
                ret.b = ((float)ret.b16 / 65535f) * 255f - 128f;

                return ret;
            }
            public static Lab FromByteToLab(byte _L, byte _a, byte _b)
            {
                Lab ret = new Lab();

                ret.L8 = _L;
                ret.a8 = _a;
                ret.b8 = _b;

                ret.L16 = (ushort)Math.Round(((float)ret.L8 / 255f) * 65535f);
                ret.a16 = (ushort)Math.Round(((float)ret.a8 / 255f) * 65535f);
                ret.b16 = (ushort)Math.Round(((float)ret.b8 / 255f) * 65535f);

                ret.L = ((float)ret.L8 / 255f) * 100f;
                ret.a = ret.a8 - 128f;
                ret.b = ret.b8 - 128f;

                return ret;
            }
            public static Lab FromFloatToLab(float _L, float _a, float _b)
            {
                Lab ret = new Lab();

                ret.L = _L;
                ret.a = _a;
                ret.b = _b;

                float L1 = (65535f * _L) / 100f;
                float a1 = (_a + 128f) * (65535f / 255f);
                float b1 = (_b + 128f) * (65535f / 255f);

                if (L1 < 0f) L1 = 0f;
                if (L1 > 65535f) L1 = 65535f;
                if (a1 < 0f) a1 = 0f;
                if (a1 > 65535f) a1 = 65535f;
                if (b1 < 0f) b1 = 0f;
                if (b1 > 65535f) b1 = 65535f;

                ret.L16 = Convert.ToUInt16(L1);
                ret.a16 = Convert.ToUInt16(a1);
                ret.b16 = Convert.ToUInt16(b1);

                ret.L8 = (byte)Math.Round(((float)ret.L16 / 65535f) * 255f);
                ret.a8 = (byte)Math.Round(((float)ret.a16 / 65535f) * 255f);
                ret.b8 = (byte)Math.Round(((float)ret.b16 / 65535f) * 255f);

                return ret;
            }
        }
        public struct LCH
        {
            public float L;
            public float C;
            public float H;
        }
    }
}
