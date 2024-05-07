using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Smartproj
{
    public enum SourceParametersTypeEnum
    {
        XML,
        JSON,
        KeyValue
    }
    public interface IAdapter
    {
        AbstractInputProvider Owner { get; }
        SourceParametersTypeEnum ParametersType { get; }
        TagFileTypeEnum FileDataFilter { get; }
        bool GetNext(Job _job, out string _link, out string _data);
    }
}
