using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace Smartproj.Utils
{
    public class StreamToFileRunner : Runner
    {
        readonly Stream mSrc;
        readonly string mDst;
        public StreamToFileRunner(ExifToolOptions opts, Stream src, string dst) : base(opts)
        {
            mSrc = src ?? throw new ArgumentNullException(nameof(src));
            mDst = dst ?? throw new ArgumentNullException(nameof(dst));
        }
        public override async Task<WriteResult> RunProcessAsync(IEnumerable<Operation> updates)
        {
            var updateArgs = GetUpdateArgs(updates);
            var runner = new StreamToStreamRunner(_options, mSrc);
            var result = await runner.RunProcessAsync(updates).ConfigureAwait(false);

            if (result.Success)
            {
                await result.Output.CopyToAsync(new FileStream(mDst, FileMode.CreateNew, FileAccess.ReadWrite)).ConfigureAwait(false);

                return new WriteResult(true, null);
            }

            return new WriteResult(false, null);
        }
    }
}
