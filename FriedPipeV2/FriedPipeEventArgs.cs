using System;

namespace FriedPipeV2
{
    public class FriedPipeEventArgs<Type> : EventArgs
    {
        public FriedPipeEventArgs(FriedPipeInfo<Type> pipeInfo)
        {
            this.PipeInfo = pipeInfo;
        }

        public FriedPipeInfo<Type> PipeInfo { get; }
    }
}
    