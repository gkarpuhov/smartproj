using Smartproj.Utils;

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
        AbstractInputProvider Owner { get; }
        SourceParametersTypeEnum MetadataType { get; }
        TagFileTypeEnum FileDataFilter { get; }
        bool GetNext(Project _project, out Job _job);
    }
}
