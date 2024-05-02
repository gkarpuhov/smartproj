namespace Smartproj.Utils
{
    public class SubtractOperation : Operation
    {
        public SubtractOperation(Tag tag)
            : base(tag)
        {

        }
        internal override string ToArg() => $"-{Target.Name}-=\"{Target.ValueToWrite()}\"";
    }
}