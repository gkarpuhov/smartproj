using System;

namespace Smartproj.Utils
{
    public class AddOperation : Operation
    {
        public AddOperation(Tag tag)
            : base(tag)
        {

        }
        internal override string ToArg() => $"-{Target.Name}+=\"{Target.ValueToWrite()}\"";
    }
}