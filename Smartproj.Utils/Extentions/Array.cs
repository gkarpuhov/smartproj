using System;
using System.Runtime.InteropServices;

namespace Smartproj.Utils
{
    public static class ArrayEx
    {
        public static byte[] ToBuffer<T>(this T[] _array) where T : struct
        {
            unsafe
            {
                GCHandle handle = GCHandle.Alloc(_array, GCHandleType.Pinned);
                try
                {
                    int rawsize = sizeof(T);
                    IntPtr pointer = handle.AddrOfPinnedObject();
                    byte[]  rawdata = new byte[rawsize * _array.Length];
                    Marshal.Copy(pointer, rawdata, 0, rawdata.Length);
                    return rawdata;
                }
                finally
                {
                    if (handle.IsAllocated)
                    {
                        handle.Free();
                    }
                }
            }
        }
        public static T[] FromBuffer<T>(this byte[] _array) where T : struct
        {
            unsafe
            {
                int rawsize = sizeof(T);
                T[] outdata = new T[_array.Length / rawsize];

                GCHandle handle = GCHandle.Alloc(outdata, GCHandleType.Pinned);
                try
                {
                    IntPtr pointer = handle.AddrOfPinnedObject();
                    Marshal.Copy(_array, 0, pointer, _array.Length);
                    return outdata;
                }
                finally
                {
                    if (handle.IsAllocated)
                    {
                        handle.Free();
                    }
                }
            }
        }
        public static int Sum<T>(this Array _array, Func<T, int> _selector, Predicate<T> _predicate = null)
        {
            int ret = 0;
            foreach (T item in _array)
            {
                if (_predicate == null || _predicate(item)) ret = ret + _selector(item);
            }
            return ret;
        }
        public static int Count<T>(this Array _array, Predicate<T> _predicate)
        {
            int counter = 0;
            foreach (T item in _array)
            {
                if (_predicate(item)) counter++;
            }
            return counter;
        }
        public static bool Find<T>(this Array _array, Predicate<T> _predicate)
        {
            foreach (T item in _array)
            {
                if (_predicate(item)) return true;
            }
            return false;
        }
    }
}
