using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Medallion.Shell;


namespace Smartproj.Utils
{
    public class FileToFileRunner : Runner
    {
        readonly string mSrc;
        readonly string mDst;
        readonly FileWriteMode mWriteMode;
        public FileToFileRunner(ExifToolOptions opts, string src, string dst) : base(opts)
        {
            mSrc = src ?? throw new ArgumentNullException(nameof(src));
            mDst = dst ?? throw new ArgumentNullException(nameof(dst));
            mWriteMode = FileWriteMode.WriteNew;
        }
        public FileToFileRunner(ExifToolOptions opts, string src, FileWriteMode writeMode) : base(opts)
        {
            mSrc = src ?? throw new ArgumentNullException(nameof(src));
            mDst = src;

            if (!IsOverwriteMode(writeMode))
            {
                throw new ArgumentException("Please specify one of the Overwrite FileWriteModes when using this constructor.");
            }

            mWriteMode = writeMode;
        }
        public override async Task<WriteResult> RunProcessAsync(IEnumerable<Operation> updates)
        {
            var updateArgs = GetUpdateArgs(updates);
            string args = null;

            if (IsOverwriteMode(mWriteMode))
            {
                args = $"{updateArgs} {GetOverwriteArgument(mWriteMode)} {EscapeFilePath(mSrc)}";
            }
            else
            {
                args = $"{updateArgs} -o {EscapeFilePath(mDst)} {EscapeFilePath(mSrc)}";
            }

            try
            {
                var cmd = Command.Run(_options.ExifToolPath, options: opts => { opts.StartInfo(si => si.Arguments = args); });

                var result = await cmd.Task.ConfigureAwait(false);

                return new WriteResult(result.Success, null);
            }
            catch (Win32Exception ex)
            {
                throw new Exception("Error when trying to start the exiftool process.  Please make sure exiftool is installed, and its path is properly specified in the options.", ex);
            }
        }
        protected string GetOverwriteArgument(FileWriteMode mode)
        {
            switch (mode)
            {
                case FileWriteMode.OverwriteOriginalInPlace:
                    return "-overwrite_original_in_place";
                case FileWriteMode.OverwriteOriginal:
                    return "-overwrite_original";
                default:
                    throw new InvalidOperationException("Unexpected overwrite mode!");
            }
        }
        bool IsOverwriteMode(FileWriteMode mode)
        {
            switch (mode)
            {
                case FileWriteMode.OverwriteOriginal:
                case FileWriteMode.OverwriteOriginalInPlace:
                    return true;
                default:
                    return false;
            }
        }
    }
}
