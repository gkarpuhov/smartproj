using System;
using System.Text;

namespace Smartproj.Utils
{
    public static class Encoder
    {
        public static byte[] XorCrypt(string _passPhrase, byte[] _inputData)
        {
            var array = new byte[256];
            var swapIndex1 = 0;
            if (string.IsNullOrEmpty(_passPhrase))
            {
                throw new ArgumentNullException(nameof(_passPhrase));
            }
            if (_passPhrase.Length > 256)
            {
                _passPhrase = _passPhrase.Substring(0, 256);
            }
            var passPhraseArray = Encoding.ASCII.GetBytes(_passPhrase);
            // Initialize array with values 0 to 255
            for (var i = 0; i <= 255; i++)
            {
                array[i] = (byte)i;
            }
            // Swap the values around based on the licensekey
            for (var i = 0; i <= 255; i++)
            {
                swapIndex1 = (swapIndex1 + array[i] + passPhraseArray[i % _passPhrase.Length]) % 256;
                // Swap
                var b = array[i];
                array[i] = array[swapIndex1];
                array[swapIndex1] = b;
            }

            swapIndex1 = 0;
            var swapIndex2 = 0;
            var output = new byte[_inputData.Length - 1 + 1];

            for (var i = 0; i <= _inputData.Length - 1; i++)
            {
                swapIndex1 = (swapIndex1 + 1) % 256;
                swapIndex2 = (swapIndex2 + array[swapIndex1]) % 256;
                // Swap
                var b = array[swapIndex1];
                array[swapIndex1] = array[swapIndex2];
                array[swapIndex2] = b;
                output[i] = (byte)(_inputData[i] ^ array[(array[swapIndex1] + array[swapIndex2]) % 256]);
            }

            return output;
        }
    }
}
