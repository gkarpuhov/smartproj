using System.IO;

namespace Smartproj.Utils
{
    public class WriteResult
    {
        public bool Success { get; private set; }
        public Stream Output { get; private set; }
        public WriteResult(bool success, Stream output)
        {
            Success = success;
            Output = output;
        }
    }
}
