using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FriedPipeV2
{
	public delegate void FriedPipelineHandler<Type>(PipeBase<Type> callerPipe, FriedPipeEventArgs<Type> e);
	public class PipelineBase<Type>
	{
		/// <summary>
		/// Creates an empty base pipeline
		/// </summary>
		public PipelineBase() { }

		/// <summary>
		/// Creates an empty base pipeline but sets its DefaultPipeChannel
		/// </summary>
		/// <param name="channel">the default pipe channel (every pipe added without a channel will get this channel)</param>
		public PipelineBase(string channel)
		{
			channel ??= "Pipe";
			DefaultPipeChannel = channel;
		}
        /// <summary>
        /// Creates an base pipeline but sets its DefaultPipeChannel, and fills the pipeline with a bunch of pipes with the names specified in pipeNames
        /// </summary>
        /// <param name="channel">the default pipe channel (every pipe added without a channel will get this channel)</param>
        /// <param name="pipeNames"></param>
        public PipelineBase(string channel = null, params string[] pipeNames)
		{
			channel ??= "Pipe";
			DefaultPipeChannel = channel;

			foreach (var pipeName in pipeNames)
			{
				this.Add(pipeName, channel);
			}
		}
		public string DefaultPipeChannel { get; protected set; } = "Pipe";

		public event FriedPipelineHandler<Type> OnAnyChange;

		private List<PipeBase<Type>> Pipes = new List<PipeBase<Type>>();

		#region connection
		/// <summary>
		/// Creates a new pipe and adds it to the pipeline
		/// </summary>
		/// <param name="name">the name of this pipe</param>
		/// <param name="channel">the channel of this pipe (leave empty to use the pipeline's default channel)</param>
		public void Add(string name, string channel = null)
		{
			channel ??= DefaultPipeChannel;
			Connect(new Pipe<Type>(name, channel));
		}
		/// <summary>
		/// Connect an already existing pipe to the pipeline
		/// </summary>
		/// <param name="pipes">The pipe(s) you want to connect to this pipeline</param>
		public void Connect(params PipeBase<Type>[] pipes)
		{
			foreach (var pipe in pipes)
			{
				Connect(pipe);
			}
		}
        /// <summary>
        /// Connect an already existing pipe to the pipeline
        /// </summary>
        /// <param name="pipe">The pipe to connect to this pipeline</param>
        /// <exception cref="OverflowException">if the pipe already exists</exception>
        public void Connect(PipeBase<Type> pipe)
		{
			if (GetPipe(pipe.Name, pipe.Channel) == null) //if it doest exist yet
			{
				pipe.OnChange += OnAnyPipeChange;
				Pipes.Add(pipe);
			}
			else
				throw new OverflowException($"A pipe with the same signature (name: {pipe.Name}, channel: {pipe.Channel}) already exists!");
		}
        /// <summary>
        /// Remove an connected pipe from the pipeline based on its name and channel
        /// </summary>
        /// <param name="name">the name of the pipe</param>
        /// <param name="channel">the channel of the pipe (leave empty to use the pipeline's default channel)</param>
        public void Remove(string name, string channel = null)
		{
			channel ??= DefaultPipeChannel;
			var pipe = GetPipe(name, channel);
			if (pipe != null)
				Disconnect(pipe);
		}
		/// <summary>
		/// Disconnect a pipe from the pipeline
		/// </summary>
		/// <param name="pipe">The pipe to disconnect</param>
		public void Disconnect(PipeBase<Type> pipe)
		{
			pipe.OnChange -= OnAnyPipeChange;
			Pipes.Remove(pipe);
		}
		/// <summary>
		/// Connect a pipe to this pipeline but at a certain position
		/// </summary>
		/// <param name="pipe">the pipe to connect</param>
		/// <param name="position">the position to place the pipe</param>
		/// <exception cref="ArgumentOutOfRangeException">if the position cant fit in the pipeline</exception>
		public void ConnectAtPosition(PipeBase<Type> pipe, int position)
		{
			if (GetPipe(pipe.Name, pipe.Channel) == null) //if it doest exist yet
			{

				if (position > Pipes.Count || position < 0)
					throw new ArgumentOutOfRangeException("We dont have that amount of pipes. " + nameof(position) + " was out of range!");
				pipe.OnChange += OnAnyPipeChange;
				Pipes.Insert(position, pipe);
			}
		}
		/// <summary>
        /// Disconnect a pipe from this pipeline but from a certain position
        /// </summary>
        /// <param name="position">the position from where to remove the pipe from</param>
        /// <exception cref="ArgumentOutOfRangeException">if the position is out of the range of the amount of connected pipes</exception>
        public void DisconnectAtPosition(int position)
		{
			if (position > Pipes.Count || position < 0)
				throw new ArgumentOutOfRangeException("We dont have that amount of pipes. " + nameof(position) + " was out of range!");

			Pipes[position].OnChange -= OnAnyPipeChange;
			Pipes.RemoveAt(position);
		}
		/// <summary>
		/// Gets the position of a pipe
		/// </summary>
		/// <param name="pipe">The pipe to find</param>
		/// <returns>The index of the pipe if found, if not found then it returns -1</returns>
		public int GetPipePosition(PipeBase<Type> pipe)
		{
			return Pipes.IndexOf(pipe);
		}
		#endregion

		#region comminication
		/// <summary>
		/// Send pipe object to all pipes connected to this pipeline
		/// </summary>
		/// <param name="pipeObject">The object to send</param>
		public void SendToAll(Type pipeObject)
		{
			foreach (PipeBase<Type> pipe in Pipes)
			{
				DummySend(pipe, pipeObject);
			}
		}
        /// <summary>
        /// Sends a pipeobject to a specific pipe in this pipeline
        /// E.G. when disconnecting to tell this pipe to stop or something.
        /// </summary>
        /// <param name="pipeObject">The object to send</param>
        /// <param name="name">The name of the pipe to target</param>
        /// <param name="channel">the channel of the pipe to target (leave empty to use the pipeline's default channel)</param>
        /// <exception cref="KeyNotFoundException">If the pipe doest not exist</exception>
        public void SendToSpecific(Type pipeObject, string name, string channel = null)
		{
			channel ??= DefaultPipeChannel;
			var pipe = GetPipe(name, channel);
			if (pipe == null)
				throw new KeyNotFoundException($"Sorry this pipe (name: {name}, channel: {channel}) doest exist or isn't connected! make sure the channel is correct!");
			DummySend(pipe, pipeObject);
		}
		/// <summary>
		/// Sets the pipeobject of all pipes in this pipeline
		/// E.G. when resetting or something to set the value without triggering the OnChange
		/// </summary>
		/// <param name="pipeObject">The pipeobject to set</param>
		public void SetAll(Type pipeObject)
		{
			foreach (PipeBase<Type> pipe in Pipes)
			{
				pipe.Set(pipeObject);
			}
		}
        /// <summary>
        /// Sets the value of a specific pipe in this pipeline without triggering the OnChange
        /// E.G. when resetting or something to set the value without triggering the OnChange
        /// </summary>
        /// <param name="pipeObject">The pipeobject to set</param>
        /// <param name="name">the name of the pipe</param>
        /// <param name="channel">the channel of the pipe (leave empty to use the pipeline's default channel)</param>
        public void SetSpecific(Type pipeObject, string name, string channel = null)
		{
			channel ??= DefaultPipeChannel;
			var pipe = GetPipe(name, channel);
			pipe?.Set(pipeObject);
		}
		/// <summary>
		/// Makes EVERY pipe in this pipeline SEND a message
		/// </summary>
		/// <param name="pipeObject">The object to send from all</param>
		public void SendFromAll(Type pipeObject)
		{
			foreach (PipeBase<Type> pipe in Pipes)
			{
				pipe.Send(pipeObject);
			}
		}
        /// <summary>
        /// Makes this single pipe SEND a message
        /// </summary>
        /// <param name="pipeObject">the object to send</param>
        /// <param name="name">the name of the pipe to send from</param>
        /// <param name="channel">the channel of the pipe to send from (leave empty to use the pipeline's default channel)</param>
        public void SendFromSpecific(Type pipeObject, string name, string channel = null)
		{
			channel ??= DefaultPipeChannel;
			var pipe = GetPipe(name, channel);
			pipe?.Send(pipeObject);
		}

   //     public void RequestAll(Type pipeObject)
   //     {
			//List
   //         foreach (PipeBase<Type> pipe in Pipes)
   //         {
   //             DummyRequest(pipe, pipeObject);
   //         }
   //     }
        /// <summary>
        /// Sends a pipeobject to a specific pipe in this pipeline
        /// E.G. when disconnecting to tell this pipe to stop or something.
        /// </summary>
        /// <param name="pipeObject">The object to send</param>
        /// <param name="name">The name of the pipe to target</param>
        /// <param name="channel">the channel of the pipe to target (leave empty to use the pipeline's default channel)</param>
        /// <exception cref="KeyNotFoundException">If the pipe doest not exist</exception>
        public async Task<Type> RequestSpecific(Type pipeObject, string name, string channel = null)
        {
            channel ??= DefaultPipeChannel;
            var pipe = GetPipe(name, channel);
            if (pipe == null)
                throw new KeyNotFoundException($"Sorry this pipe (name: {name}, channel: {channel}) doest exist or isn't connected! make sure the channel is correct!");
            return await DummyRequest(pipe, pipeObject);
        }
        #endregion

        /// <summary>
        /// when any pipe gets changed this will trigger
        /// </summary>
        private void OnAnyPipeChange(PipeBase<Type> sender, FriedPipeEventArgs<Type> e)
		{
			OnAnyChange?.Invoke(sender, e);
		}
        /// <summary>
        /// An indexer to go trough this pipeline and get a pipe
        /// </summary>
        /// <param name="name">the name of the pipe to get</param>
        /// <param name="channel">the channel of the pipe to get (leave empty to use the pipeline's default channel)</param>
        /// <returns>the pipe you wanted</returns>
        /// <exception cref="KeyNotFoundException">if the pipe doest exist</exception>
        public PipeBase<Type> this[string name, string channel = null]
		{
			get
			{
				channel ??= DefaultPipeChannel;
				var pipe = GetPipe(name, channel);
				if (pipe == null)
					throw new KeyNotFoundException($"Sorry this pipe (name: {name}, channel: {channel}) doest exist or isn't connected!");
				return pipe;
			}
		}
		/// <summary>
		/// Send an pipeobject to an pipe
		/// </summary>
		/// <param name="Address">the pipe to send to</param>
		/// <param name="pipeObject">the pipeobject to send</param>
		public void DummySend(PipeBase<Type> Address, Type pipeObject)
		{
			Pipe<Type> dummy = new Pipe<Type>(Address.Name, Address.Channel);
			dummy.Send(pipeObject);
		}
        public async Task<Type> DummyRequest(PipeBase<Type> Address, Type pipeObject)
        {
            Pipe<Type> dummy = new Pipe<Type>(Address.Name, Address.Channel);
            return await dummy.Request(pipeObject);
        }
        /// <summary>
        /// Gets a pipe
        /// </summary>
        /// <param name="name">The name of the pipe to get</param>
        /// <param name="channel">the channel of the pipe to get (leave empty to use the pipeline's default channel)</param>
        /// <returns>the pipe you wanted or null if it doest exist</returns>
        public PipeBase<Type> GetPipe(string name, string channel = null)
		{
			channel ??= DefaultPipeChannel;
			return Pipes.FirstOrDefault(p => p.Name == name && p.Channel == channel);
		}
	}
}
