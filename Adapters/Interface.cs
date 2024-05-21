using Smartproj.Utils;
using System;

namespace Smartproj
{
    public enum SourceParametersTypeEnum
    {
        None,
        XML,
        JSON,
        KeyValue
    }
    public interface IAdapter
    {
        SourceParametersTypeEnum MetadataType { get; }
        TagFileTypeEnum FileDataFilter { get; }
        Guid UID { get; }
        bool GetNext(Project _project, AbstractInputProvider _provider, out Job _job);
    }
}
