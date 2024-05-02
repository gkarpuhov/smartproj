using GdPicture14;
using System;
using System.Runtime.InteropServices;

namespace Smartproj.Utils
{
    public static class ImageEx
    {
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
    }
}
