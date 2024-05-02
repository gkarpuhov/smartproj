using Newtonsoft.Json.Linq;
using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Smartproj.Utils
{
    public class ExifTaggedFile : IKeyd<string>
    {
        public readonly string GUID;
        public readonly TagColorModeEnum ColorSpace;
        public readonly TagFileTypeEnum ImageType;
        public readonly byte Bpc;
        public readonly bool HasLayers;
        public readonly bool HasTransparency;
        public readonly int CreateImageMinutes;
        public readonly bool HasGPS;
        public readonly ValueTuple<int, int> GpsPosition;
        public readonly Size ImageSize;
        public readonly bool HasTransform;
        public readonly byte[] ColorProfile;
        public readonly string FileName;
        public readonly string FilePath;
        public readonly List<Rectangle> ObjectDetect;
        public readonly IEnumerable<KeyValuePair<string, IEnumerable<JProperty>>> Tags;
        public bool HasColorProfile => ColorProfile != null && ColorProfile.Length != 0;
        public string KeyId => FileName;
        public int Index { get; }
        public int OrderBy { get; set; }
        private ImageStatusEnum mStatus;
        public ImageStatusEnum Status => mStatus;
        public ExifTaggedFile(int _index, string _file, string _dir)
        {
            mStatus = ImageStatusEnum.New;
            ObjectDetect = new List<Rectangle>();
            Index = _index;
            FileName = _file;
            FilePath = _dir;
            GUID = Guid.NewGuid().ToString();
            OrderBy = -1;
            ImageType = TagFileTypeEnum.UNDEFINED;
            ColorSpace = TagColorModeEnum.Unknown;
        }
        public ExifTaggedFile(int _index, string _file, string _dir, Size _size, DateTime _date, TagFileTypeEnum _type, IEnumerable<KeyValuePair<string, IEnumerable<JProperty>>> _tag, TagColorModeEnum _space, bool _hasgeo, ValueTuple<int, int> _gps, byte[] _profile = null, byte _bpc = 8, bool _layers = false, bool _transparency = false, bool _hastransform = false)
        {
            mStatus = ImageStatusEnum.New;
            ObjectDetect = new List<Rectangle>();
            Index = _index;
            FileName = _file;
            Tags = _tag;
            ColorSpace = _space;
            ColorProfile = _profile;
            ImageType = _type;
            Bpc = _bpc;
            HasLayers = _layers;
            HasTransparency = _transparency;
            // Количество минут начиная с 1970 года
            if (_date != default)
            {
                CreateImageMinutes = (int)((_date.Ticks / (10000000L * 60L)) - 1035432000L);
            }
            else
            {
                CreateImageMinutes = 0;
            }
            HasGPS = _hasgeo;
            GpsPosition = _gps;
            ImageSize = _size;
            HasTransform = _hastransform;
            FilePath = _dir;
            GUID = Guid.NewGuid().ToString();
            OrderBy = -1;
        }
        public ImageStatusEnum AddStatus(ImageStatusEnum _add)
        {
            return (mStatus = mStatus | _add);
        }
        public bool HasStatus(ImageStatusEnum _status)
        {
            return (mStatus & _status) == _status;
        }
    }

}
