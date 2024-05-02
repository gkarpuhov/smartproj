using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Smartproj.Utils
{
    public class ExifTool
    {
        readonly ExifToolOptions mOpts;
        public ExifTool(ExifToolOptions opts)
        {
            mOpts = opts ?? throw new ArgumentNullException(nameof(opts));
        }
        public async Task<IDictionary<string, IEnumerable<Tag>>> GetTagsAllAsync(string srcPath)
        {
            try
            {
                VerifySourceFile(srcPath);

                var reader = new ExifReader(mOpts);
                var parser = new ExifParser();
                var exifJson = await reader.ReadExifAllAsync(srcPath);

                return parser.ParseTags(exifJson);
            }
            catch (Exception ex) 
            {
                return new Dictionary<string, IEnumerable<Tag>>();
            }
        }
        public async Task<IDictionary<string, IEnumerable<KeyValuePair<string, IEnumerable<JProperty>>>>> ExtractAllAsync(string srcPath)
        {
            try
            {
                VerifySourceFile(srcPath);

                var reader = new ExifReader(mOpts);
                var parser = new ExifParser();
                var exifJson = await reader.ReadExifAllAsync(srcPath);

                return parser.ExtractTags(exifJson);
            }
            catch (Exception ex)
            {
                return new Dictionary<string, IEnumerable<KeyValuePair<string, IEnumerable<JProperty>>>>();
            }
        }
        public async Task<IEnumerable<Tag>> GetTagsAsync(string srcPath)
        {
            VerifySourceFile(srcPath);

            var reader = new ExifReader(mOpts);
            var parser = new ExifParser();
            var exifJson = await reader.ReadExifAsync(srcPath).ConfigureAwait(false);
        
            return parser.ParseTags(exifJson);
        }
        public async Task<IEnumerable<Tag>> GetTagsAsync(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var reader = new ExifReader(mOpts);
            var parser = new ExifParser();
            var exifJson = await reader.ReadExifAsync(stream).ConfigureAwait(false);

            return parser.ParseTags(exifJson);
        }
        public Task<WriteResult> OverwriteTagsAsync(string srcPath, IEnumerable<Operation> updates, FileWriteMode writeMode)
        {
            VerifySourceFile(srcPath);
            VerifyUpdates(updates);

            var runner = new FileToFileRunner(mOpts, srcPath, writeMode);

            return runner.RunProcessAsync(updates);
        }
        public Task<WriteResult> WriteTagsAsync(string srcPath, IEnumerable<Operation> updates, string dstPath)
        {
            VerifySourceFile(srcPath);
            VerifyUpdates(updates);

            var runner = new FileToFileRunner(mOpts, srcPath, dstPath);

            return runner.RunProcessAsync(updates);
        }
        public Task<WriteResult> WriteTagsAsync(string srcPath, IEnumerable<Operation> updates)
        {
            VerifySourceFile(srcPath);
            VerifyUpdates(updates);

            var runner = new FileToStreamRunner(mOpts, srcPath);

            return runner.RunProcessAsync(updates);
        }
        public Task<WriteResult> WriteTagsAsync(Stream src, IEnumerable<Operation> updates)
        {
            VerifyUpdates(updates);

            var runner = new StreamToStreamRunner(mOpts, src);

            return runner.RunProcessAsync(updates);
        }
        public Task<WriteResult> WriteTagsAsync(Stream src, IEnumerable<Operation> updates, string dstPath)
        {
            VerifyUpdates(updates);

            var runner = new StreamToFileRunner(mOpts, src, dstPath);

            return runner.RunProcessAsync(updates);
        }
        void VerifySourceFile(string srcPath)
        {
            if (!File.Exists(srcPath) && !Directory.Exists(srcPath))
            {
                throw new FileNotFoundException("Please make sure the image exists.", srcPath);
            }
        }
        void VerifyUpdates(IEnumerable<Operation> updates)
        {
            if (updates == null)
            {
                throw new ArgumentNullException();
            }

            if (updates.Count() == 0)
            {
                throw new ArgumentException("No update operations specified!", nameof(updates));
            }
        }
    }
}
