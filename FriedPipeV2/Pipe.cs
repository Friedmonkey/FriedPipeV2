using System;

namespace FriedPipeV2
{
    public class Pipe : PipeBase<string>
    {
        public Pipe(string Name, string Channel = null, FriedPipeHandler<string> OnChange = null) : base(Name, Channel, OnChange)
        {
            base.Extend();
        }
    }
    public class Pipe<Type> : PipeBase<Type>
    {
        public Pipe(string Name, string Channel = null, FriedPipeHandler<Type> OnChange = null) : base(Name, Channel, OnChange)
        {
            base.Extend();
        }
    }
}
