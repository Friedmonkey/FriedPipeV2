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
		public PipelineBase() { }
		public PipelineBase(string channel)
		{
			channel ??= "Pipe";
			DefaultPipeChannel = channel;
		}
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
		public void Add(string name, string channel = null)
		{
			channel ??= DefaultPipeChannel;
			Connect(new Pipe<Type>(name, channel));
		}
		public void Connect(params PipeBase<Type>[] pipes)
		{
			foreach (var pipe in pipes)
			{
				Connect(pipe);
			}
		}
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

		public void Remove(string name, string channel = null)
		{
			channel ??= DefaultPipeChannel;
			var pipe = GetPipe(name, channel);
			if (pipe != null)
				Disconnect(pipe);
		}
		public void Disconnect(PipeBase<Type> pipe)
		{
			pipe.OnChange -= OnAnyPipeChange;
			Pipes.Remove(pipe);
		}
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
		public void DisconnectAtPosition(int position)
		{
			if (position > Pipes.Count || position < 0)
				throw new ArgumentOutOfRangeException("We dont have that amount of pipes. " + nameof(position) + " was out of range!");

			Pipes[position].OnChange -= OnAnyPipeChange;
			Pipes.RemoveAt(position);
		}
		public int GetPipePosition(PipeBase<Type> pipe)
		{
			return Pipes.IndexOf(pipe);
		}
		#endregion

		#region comminication
		public void SendToAll(Type pipeObject)
		{
			foreach (PipeBase<Type> pipe in Pipes)
			{
				DummySend(pipe, pipeObject);
			}
		}
		public void SendToSpecific(Type pipeObject, string name, string channel = null)
		{
			channel ??= DefaultPipeChannel;
			var pipe = GetPipe(name, channel);
			if (pipe == null)
				throw new KeyNotFoundException($"Sorry this pipe (name: {name}, channel: {channel}) doest exist or isn't connected! make sure the channel is correct!");
			DummySend(pipe, pipeObject);
		}
		public void SetAll(Type pipeObject)
		{
			foreach (PipeBase<Type> pipe in Pipes)
			{
				pipe.Set(pipeObject);
			}
		}
		public void SetSpecific(Type pipeObject, string name, string channel = null)
		{
			channel ??= DefaultPipeChannel;
			var pipe = GetPipe(name, channel);
			pipe?.Set(pipeObject);
		}
		public void SendFromAll(Type pipeObject)
		{
			foreach (PipeBase<Type> pipe in Pipes)
			{
				pipe.Send(pipeObject);
			}
		}
		public void SendFromSpecific(Type pipeObject, string name, string channel = null)
		{
			channel ??= DefaultPipeChannel;
			var pipe = GetPipe(name, channel);
			pipe?.Send(pipeObject);
		}
		#endregion

		private void OnAnyPipeChange(PipeBase<Type> sender, FriedPipeEventArgs<Type> e)
		{
			OnAnyChange?.Invoke(sender, e);
		}

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
		public void DummySend(PipeBase<Type> Address, Type pipeObject)
		{
			Pipe<Type> dummy = new Pipe<Type>(Address.Name, Address.Channel);
			dummy.Send(pipeObject);
		}
		public PipeBase<Type> GetPipe(string name, string channel = null)
		{
			channel ??= DefaultPipeChannel;
			return Pipes.FirstOrDefault(p => p.Name == name && p.Channel == channel);
		}
	}
}
