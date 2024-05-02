using System;


namespace Smartproj.Utils
{
    public class SetOperation : Operation
    {
        public SetOperation(Tag tag) : base(tag)
        {
        }
        internal override string ToArg() => $"-{Target.Name}=\"{Target.ValueToWrite()}\"";
    }
}