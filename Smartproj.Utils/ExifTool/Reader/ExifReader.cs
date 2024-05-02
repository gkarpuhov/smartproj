using Medallion.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Smartproj.Utils
{
    public class ExifReader
    {
        readonly ExifToolOptions mOpts;
        public ExifReader(ExifToolOptions opts)
        {
            mOpts = opts ?? throw new ArgumentNullException(nameof(opts));
        }
        public async Task<JObject> ReadExifAsync(Stream imageStream)
        {
            var args = GetArguments(imageStream);
            var exifDataStream = await GetExifDataAsync(args, imageStream).ConfigureAwait(false);

            return await GetJsonAsync(exifDataStream).ConfigureAwait(false);
        }
        public async Task<JObject> ReadExifAsync(string imagePath)
        {
            var args = GetArguments(imagePath);
            var exifDataStream = await GetExifDataAsync(args, null).ConfigureAwait(false);

            return await GetJsonAsync(exifDataStream).ConfigureAwait(false);
        }
        public async Task<JArray> ReadExifAllAsync(string imagePath)
        {
            var args = GetArguments(imagePath);
            var  exifDataStream = await GetExifDataAsync(args, null).ConfigureAwait(false);

            using (var sr = new StreamReader(exifDataStream))
            {
                string exifDataString = sr.ReadToEnd();

                if (exifDataString != "")
                {
                    var jtoken = JContainer.Parse(exifDataString);

                    if (jtoken.Type == JTokenType.Array)
                    {
                        return (JArray)jtoken;
                    }

                    if (jtoken.Type == JTokenType.Object)
                    {
                        if (jtoken.Children().Count() == 1)
                        {
                            return new JArray() { jtoken.Children().First() as JObject };
                        }
                    }
                }
            }

            return new JArray();
        }
        async Task<JObject> GetJsonAsync(Stream exifDataStream)
        {
            JToken token = null;

            using (var sr = new StreamReader(exifDataStream))
            {
                token = await JToken.ReadFromAsync(new JsonTextReader(sr)).ConfigureAwait(false);
            }

            if (token == null)
            {
                return null;
            }

            if (token.Children().Count() == 1)
            {
                return token.Children().First() as JObject;
            }

            return null;
        }
        async Task<Stream> GetExifDataAsync(string[] args, Stream stream)
        {
            Command cmd = null;
            try
            {
                if (stream == null)
                {
                    cmd = Command.Run(mOpts.ExifToolPath, args);
                }
                else
                {
                    cmd = Command.Run(mOpts.ExifToolPath, args) < stream;
                }

                await cmd.Task.ConfigureAwait(false);

                return cmd.StandardOutput.BaseStream;
            }
            catch (Win32Exception ex)
            {
                throw new Exception("Error when trying to start the exiftool process.  Please make sure exiftool is installed, and its path is properly specified in the options.", ex);
            }
        }
        string[] GetArguments(string rawFile)
        {
            var args = GetDefaultArguments();

            if ((File.GetAttributes(rawFile) & FileAttributes.Directory) == FileAttributes.Directory)
            {
                rawFile = $"{rawFile}\\*.*";
            }
            args.Add(rawFile);

            return args.ToArray();
        }
        string[] GetArguments(Stream stream)
        {
            var args = GetDefaultArguments();

            args.Add("-");

            return args.ToArray();
        }
        List<string> GetDefaultArguments()
        {
            var list = new List<string>
            {
                "-j", "-t", "-l", "-m", "-G:0", "-ALL", "-ALL#"
            };

            if (mOpts.IncludeBinaryTags)
            {
                list.Add("-b");
            }

            if (mOpts.EscapeTagValues)
            {
                list.Add("-E");
            }

            if (mOpts.ExtractICCProfile)
            {
                if (!mOpts.IncludeBinaryTags)
                {
                    mOpts.IncludeBinaryTags = true;
                    list.Add("-b");
                }
                list.Add("-icc_profile");
            }

            return list;
        }
    }
}