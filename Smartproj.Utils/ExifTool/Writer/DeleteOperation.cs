using System;

namespace Smartproj.Utils
{
    public class DeleteOperation : Operation
    {
        public DeleteOperation(Tag tag) : base(tag)
        {

        }
        internal override string ToArg() => $"-{Target.Name}=";
    }
}