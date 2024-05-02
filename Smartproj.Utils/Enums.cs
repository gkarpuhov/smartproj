using System;

namespace Smartproj.Utils
{
    [Flags]
    public enum ObjectDetectImageEnum
    {
        None = 0,
        FrontFace = 1,
        ProfileFace = 2,
        FullBody = 4,
        UpperBody = 8,
        LowerBody = 16
    }
    [Flags]
    public enum TagFileTypeEnum
    {
        UNDEFINED = 0,
        JPEG = 0x1,
        TIFF = 0x2,
        PNG = 0x4,
        HEIC = 0x8,
        GIF = 0x10,
        BMP = 0x20,
        CR2 = 0x40,
        PSD = 0x80,
        WEBP = 0x100,
        PDF = 0x200,
        XML = 0x400,
        HTML = 0x800,
        JSON = 0x1000,
        TEXT = 0x2000,
        DOC = 0x4000,
        EXCEL = 0x8000,
        PS = 0x10000,
        EPS = 0x20000,
        POSTSCRIPT = 0x40000, // если не PS и не EPS
        ZIP = 0x80000,
        EXE = 0x100000,
        DLL = 0x200000,
        BIN = 0x400000, // если не DLL и не EXE
        ICC = 0x800000,
        JFIF = 0x1000000,
        NOTSUPPORTED = 0x2000000,
        ANYFILES = 0x4000000
    }
    public enum TagMIMETypeEnum
    {
        UNDEFINED,
        JPEG,
        TIFF,
        PNG,
        HEIC,
        GIF,
        BMP,
        CR2,
        PSD,
        WEBP,
        PDF,
        XML,
        HTML,
        JSON,
        TEXT,
        DOC,
        EXCEL,
        POSTSCRIPT,
        ZIP,
        BIN,
        ICC,
        NOTSUPPORTED
    }
    public enum TagColorModeEnum
    {
        Bitmap = 0,
        Grayscale = 1,
        Indexed = 2,
        RGB = 3,
        CMYK = 4,
        Multichannel = 7,
        Duotone = 8,
        Lab = 9,
        Unknown = 10
    }
    public enum PngColorTypeEnum
    {
        Grayscale = 0,
        RGB = 2,
        Palette = 3,
        Grayscale_with_Alpha = 4,
        RGB_with_Alpha = 6,
        Unknown = 10
    }
    [Flags]
    public enum DetailLayoutTypeEnum
    {
        Undefined = 0,
        Single = 1,
        Page = 2,
        Spread = 4,
        Window = 8,
        Sticker = 16,
        Combo1x = 32,
        Combo2x = 64,
        Lainer = 128,
        Material = 256,
        Cover = 512
    }
    [Flags]
    public enum DetailTypeEnum
    {
        Undefined = 0,
        Cover = 1,
        Block = 2,
        Insert = 4,
        Flyleaf = 8,
        Tracing = 16,
        Background = 32
    }
    [Flags]
    public enum BindingEnum
    {
        None = 0,
        Glue = 1,
        ThreadStitching = 2,
        FlexBind = 4,
        LayFlat = 8,
        Premium = 16,
        Staple = 32,
        Spring = 64
    }
    [Flags]
    public enum PageSide
    {
        None = 0,
        Single = 1, // Не определено понятия левая/правая (карточки, календари)
        Left = 2, // Применимо к левой полосе
        Right = 4, // Применимо к правой полосе
        Front = 8, // Применимо к лицевой стороне листа
        Back = 16, // Применимо к оборотной стороне листа
        LeftAndRight = 32, // Формат включает в себя, и левую, и правую стороны (layflat)
        DefaultPage = Left | Right | Front | Back, // Классика, флекс
        DefaultPremium = Left | Right | Front, // Бабочки разрезные
        DefaultTwoPage = LeftAndRight | Front, // Бабочки
        DefaultDuplex = Single | Front | Back, // Карточки, календари двухстороннии
        DefaultSimplex = Single | Front, // Карточки, календари одностороннии
        DefaultFirst = Right | Front, // Первая страница классики
        DefaultLast = Left | Back // Последняя страница классики
    }
    public enum SegmentTypeEnum
    {
        Root,
        FileStructure,
        Directory,
        Geolocation,
        DateCreate,
        Segment
    }
    [Flags]
    public enum ImageStatusEnum
    {
        New = 0,
        Error = 1,
        NotSupported = 2,
        ExifData = 4,
        OriginalIsReady = 8,
        FormatTransformed = 16,
        ColorTransformed = 32,
        SizeTransformed = 64,
        SizeVerified = 128,
        FacesDetected = 256,
        Imposed = 512,
        Placed = 1024
    }
    [Flags]
    public enum LogModeEnum
    {
        Undefined = 0,
        Message = 1,
        Warning = 2,
        Error = 4,
        All = Message | Warning | Error
    }
    public enum HorizontalPositionEnum
    {
        Left,
        Right,
        Center
    }
    public enum VerticalPositionEnum
    {
        Top,
        Bottom,
        Center
    }
    public enum TextCaseEnum
    {
        Any,
        Lower,
        Upper
    }
    public enum GraphicTypeEnum
    {
        Fill,
        Ellipse,
        Path,
        Image,
        Clip,
        ImageFrame,
        TextFrame
    }
    public enum ImageFrameShapeEnum
    {
        Rectangle,
        Ellipse
    }
}
