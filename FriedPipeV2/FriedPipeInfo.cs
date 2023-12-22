using Newtonsoft.Json;
using System;
using System.Linq;

namespace FriedPipeV2
{
    [Serializable]
    public class FriedPipeInfo<Type>
    {
        public FriedPipeInfo(string compChannel, string assemblyQualifiedName, Type pipeObject, bool requestMode)
        {
			CompChannel = compChannel;
			AssemblyQualifiedName = assemblyQualifiedName;
            PipeObject = pipeObject;
            RequestMode = requestMode;
        }
        [JsonIgnore]
        public bool IsValid => !string.IsNullOrEmpty(Channel) && (PipeObject != null);
		public string CompChannel { get; protected set; }
		public string Name => CompChannel.Split('-').Last();
		public string Channel => CompChannel.Split('-').First();    
		public string AssemblyQualifiedName { get; protected set; }
        public Type PipeObject { get; protected set; }
        public bool RequestMode { get; protected set; }
    }
}
