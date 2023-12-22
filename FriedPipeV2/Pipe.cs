using System;

namespace FriedPipeV2
{
    /// <summary>
    /// Default string pipe, used to achive Inter Proccess Communication
    /// </summary>
    public class Pipe : PipeBase<string>
    {
        public Pipe(string Name, string Channel = null, FriedPipeHandler<string> OnChange = null) : base(Name, Channel, OnChange)
        {
            base.Extend();
        }
    }
    /// <summary>
    /// Custom class pipe, used to achive Inter Proccess Communication
    /// </summary>
    public class Pipe<Type> : PipeBase<Type>
    {
        public Pipe(string Name, string Channel = null, FriedPipeHandler<Type> OnChange = null) : base(Name, Channel, OnChange)
        {
            base.Extend();
        }
    }
}
