using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FriedPipeV2
{
	public delegate void FriedPipeHandler<Type>(PipeBase<Type> callerPipe, FriedPipeEventArgs<Type> e);
	public delegate Type FriedPipeRequestHandler<Type>(PipeBase<Type> callerPipe, FriedPipeEventArgs<Type> e);

	public class PipeBase<Type>
	{
		public event FriedPipeHandler<Type> OnChange;
		public event FriedPipeRequestHandler<Type> OnRequest;
		public PipeBase(string Name, string Channel = null, FriedPipeHandler<Type> OnChange = null)
		{
			if (Name != null)
			{
				this.Name = Name;
				if (Channel != null)
				{
					this.Channel = Channel;
				}
			}
			GetChannelDirectory(this.CompChannel); //make path
			watcherList = new Dictionary<string, FileSystemWatcher>(StringComparer.InvariantCultureIgnoreCase);
			Task.Run(() => CleanUpOldMessagesForAllChannels());
			if (OnChange is not null)
				this.OnChange += OnChange;
		}
		public string Channel { get; protected set; } = "Pipe";
		public string Name { get; protected set; } = "p";
		private string CompChannel => $"{Channel}-{Name}";

		private Type InternalValue;
		private Type TempValue;
		private bool requestMode = false;
		public bool isExternal { get; protected set; } = false;
		public bool isInverted { get; protected set; } = false;
		private bool Changed { get; set; } = false;
		private void intercept(FriedPipeInfo<Type> pipeInfo)
		{
			if (pipeInfo.RequestMode)//was it a request message?
			{
				if (requestMode) //are we the one that sent it and were waiting for repsonse?
				{
					this.TempValue = pipeInfo.PipeObject;
					this.Changed = true;
				}
				else //request but we  didnt request, so we must send a response
				{
					if (OnRequest == null)
						return;
					var value = OnRequest.Invoke(this, new FriedPipeEventArgs<Type>(pipeInfo));
					send(value, true);
				}
			}
			else
			{
				this.InternalValue = pipeInfo.PipeObject;
				OnChange?.Invoke(this, new FriedPipeEventArgs<Type>(pipeInfo));
			}
		}


		public void Set(Type Object)
		{
			set(Object, false);
		}
		public Type Get()
		{
			if (isExternal)
			{
				return InternalValue;
			}
			else
			{
				return InternalValue;
			}
		}
		public Type Value
		{
			set
			{
				this.Set(value);
			}

			get
			{
				return this.Get();
			}
		}

		public void Extend()
		{
			if (isExternal)
				throw new Exception("pipe cant be extended");
			else
			{
				RegisterChannel(this.CompChannel);
				isExternal = true;
			}
		}
		public void Retract()
		{
			if (isExternal)
			{
				UnRegisterChannel(this.CompChannel);
				isExternal = false;
			}
			else
				throw new Exception("pipe cant be retracted");
		}
		public void Invert()
		{
			isInverted = !isInverted;
			isExternal = !isExternal;
		}

		public void Send(Type Object)
		{
			send(Object, false);
		}
		public async Task<Type> Request(Type Object)
		{
			bool wasExternal = this.isExternal;
			bool wasInverted = this.isInverted;

			if (wasInverted)
				this.Invert();

			if (wasExternal)
				this.Retract();

			this.Extend();
			this.requestMode = true;
			this.send(Object, true);
			while (!this.Changed)
			{
				await Task.Delay(10);
			}
			this.Retract();

			this.Changed = false;
			this.requestMode = false;

			if (wasInverted)
				this.Invert();

			if (wasExternal)
				this.Extend();

			return this.TempValue;
		}


		private void send(Type obj, bool request)
		{
			bool wasExternal = this.isExternal;
			bool wasInverted = this.isInverted;

			if (wasInverted)
				this.Invert();

			if (wasExternal)
				this.Retract();

			this.Invert();
			this.set(obj, request);
			this.Invert();

			if (wasInverted)
				this.Invert();

			if (wasExternal)
				this.Extend();
		}
		private void set(Type obj, bool request)
		{
			if (isExternal)
			{
				SendToChannelB(this.CompChannel, obj, request);
			}
			else
			{
				//send internal pipe
				InternalValue = obj;
			}
		}



		#region pipe buisness
		#region Sender
		private readonly int messageTimeoutInMilliseconds = 1000;

		private const string MutexCleanUpKey = @"Global\FriedPipeSender.Cleanup";

		private static readonly char[] InvalidChannelChars = Path.GetInvalidFileNameChars();

		private static readonly string TemporaryFolder =
			Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				"FriedPipe"
				);
		#region Message cleanup
		private void CleanUpMessages(object state)
		{
			var directory = (DirectoryInfo)state;

			bool createdNew;
			var mutexName = string.Concat(MutexCleanUpKey, ".", directory.Name);
			var accessControl = new MutexSecurity();
			var sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
			accessControl.SetAccessRule(new MutexAccessRule(sid, MutexRights.FullControl, AccessControlType.Allow));
			using (var mutex = new Mutex(true, mutexName, out createdNew, accessControl))
			{
				if (createdNew)
				{
					try
					{
						Thread.Sleep(messageTimeoutInMilliseconds);
					}
					catch (ThreadInterruptedException)
					{
					}
					CleanUpMessages(directory, messageTimeoutInMilliseconds);
					mutex.ReleaseMutex();
				}
			}
			if (createdNew)
			{
				ThreadPool.QueueUserWorkItem(CleanUpMessages, directory);
			}
		}
		private void CleanUpOldMessagesForAllChannels()
		{
			Parallel.ForEach(Directory.EnumerateDirectories(TemporaryFolder, "*", SearchOption.TopDirectoryOnly), x =>
			{
				var directory = new DirectoryInfo(x);
				CleanUpMessages(directory, messageTimeoutInMilliseconds);
				if (!directory.GetFiles("*.*").Any() && directory.LastAccessTime < DateTime.UtcNow.AddDays(-30))
				{
					directory.Delete();
				}
			});
		}
		private static void CleanUpMessages(DirectoryInfo directory, int fileTimeoutMilliseconds)
		{
			try
			{
				if (!Directory.Exists(directory.FullName))
				{
					return;
				}

				foreach (var file in directory.GetFiles("*.fp"))
				{
					if (file.CreationTimeUtc > DateTime.UtcNow.AddMilliseconds(-fileTimeoutMilliseconds)
						|| !File.Exists(file.FullName))
					{
						continue;
					}

					try
					{
						file.Delete();
					}
					catch (IOException)
					{
					}
					catch (UnauthorizedAccessException)
					{
					}
				}
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
		#endregion
		#region send to channel
		private void SendToChannelB(string channelName, Type message, bool request)
		{
			SendToChannelC(channelName, typeof(string).AssemblyQualifiedName, message, request);
		}
		private void SendToChannelC(string channelName, string dataType, Type Object, bool request)
		{
			if (string.IsNullOrWhiteSpace(channelName) || string.IsNullOrWhiteSpace(dataType) || Object == null)
			{ return; }

			var fileName = Guid.NewGuid().ToString();
			var folder = GetChannelDirectory(channelName);
			var filePath = Path.Combine(folder, string.Concat(fileName, ".fp"));

			using (var writer = File.CreateText(filePath))
			{
				var pipeInfo = new FriedPipeInfo<Type>(channelName, dataType, Object, request);
				writer.Write(JsonConvert.SerializeObject(pipeInfo));
				writer.Flush();
			}

			ThreadPool.QueueUserWorkItem(CleanUpMessages, new FileInfo(filePath).Directory);
		}

		internal static string GetChannelDirectory(string channelName)
		{
			string folder = null;
			try
			{
				var channelKey = GetChannelKey(channelName);
				folder = Path.Combine(TemporaryFolder, channelKey);
				if (!Directory.Exists(folder))
				{
					Directory.CreateDirectory(folder);
					try
					{
						var directorySecurity = Directory.GetAccessControl(folder);
						var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
						directorySecurity.AddAccessRule(new FileSystemAccessRule(everyone,
							FileSystemRights.Modify | FileSystemRights.Read | FileSystemRights.Write |
							FileSystemRights.Delete |
							FileSystemRights.Synchronize,
							InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None,
							AccessControlType.Allow));
						Directory.SetAccessControl(folder, directorySecurity);
					}
					catch (UnauthorizedAccessException)
					{
					}
				}
				return folder;
			}
			catch (PathTooLongException e)
			{
				throw new ArgumentException(
					$"Unable to bind to channel as the name '{channelName}' is too long." +
					" Try a shorter channel name.", e);
			}
			catch (UnauthorizedAccessException ue)
			{
				throw new UnauthorizedAccessException(
					$"Unable to bind to channel '{channelName}' as access is denied." +
					$" Ensure the process has read/write access to the directory '{folder}'.", ue);
			}
			catch (IOException ie)
			{
				throw new IOException(
					$"There was an unexpected IO error binding to channel '{channelName}'." +
					$" Ensure the process is unable to read/write to directory '{folder}'.", ie);
			}
		}

		internal static string GetChannelKey(string channelName)
		{
			foreach (var c in InvalidChannelChars)
			{
				if (channelName.Contains(c.ToString()))
				{
					channelName = channelName.Replace(c, '_');
				}
			}
			return channelName;
		}

		#endregion
		#endregion
		#region Receiver
		private readonly object disposeLock = new object();
		private readonly object lockObj = new object();
		private bool disposed;
		private Dictionary<string, FileSystemWatcher> watcherList;

		private void RegisterChannel(string channelName)
		{
			if (string.IsNullOrWhiteSpace(channelName))
				return;

			if (disposed)
			{
				return;
			}

			lock (disposeLock)
			{
				if (disposed)
				{
					return;
				}

				var watcher = EnsureWatcher(channelName);
				watcher.EnableRaisingEvents = true;
			}
		}
		private void UnRegisterChannel(string channelName)
		{
			if (string.IsNullOrWhiteSpace(channelName))
				return;

			if (disposed)
			{
				throw new ObjectDisposedException("FriedPipeReceiver", "This instance has been disposed.");
			}

			lock (disposeLock)
			{
				if (disposed)
				{
					throw new ObjectDisposedException("FriedPipeReceiver", "This instance has been disposed.");
				}

				var watcher = EnsureWatcher(channelName);
				watcher.EnableRaisingEvents = false;
			}
		}

		private FileSystemWatcher EnsureWatcher(string channelName)
		{
			FileSystemWatcher watcher;
			if (watcherList.TryGetValue(channelName, out watcher))
			{
				return watcher;
			}

			lock (lockObj)
			{
				if (watcherList.TryGetValue(channelName, out watcher))
				{
					return watcher;
				}

				var folder = GetChannelDirectory(channelName);
				watcher = new FileSystemWatcher(folder, "*.fp")
				{
					NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite
				};

				watcher.Changed += OnChanged;
				watcherList.Add(channelName, watcher);
			}

			return watcher;
		}

		private void OnChanged(object sender, FileSystemEventArgs e)
		{
			if (e.ChangeType != WatcherChangeTypes.Changed)
			{
				return;
			}

			Action<string> action = ProcessMessage;
			action.BeginInvoke(e.FullPath, action.EndInvoke, null);
		}
		private void ProcessMessage(string fullPath)
		{
			try
			{
				if (!File.Exists(fullPath))
				{
					return;
				}

				string rawmessage;
				using (var stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					using (var reader = new StreamReader(stream))
					{
						rawmessage = reader.ReadToEnd();
					}
				}
				FriedPipeInfo<Type> pipeInfo = JsonConvert.DeserializeObject<FriedPipeInfo<Type>>(rawmessage);
				if (pipeInfo != null && pipeInfo.IsValid)
				{
					intercept(pipeInfo);
				}
			}
			catch (FileNotFoundException)
			{
			}
			catch (UnauthorizedAccessException ue)
			{
				throw new UnauthorizedAccessException(
					"Unable to bind to channel as access is denied." +
					$" Ensure the process has read/write access to the directory '{fullPath}'.",
					ue);
			}
			catch (IOException ie)
			{
				throw new IOException(
					"There was an unexpected IO error binding to a channel." +
					$" Ensure the process is unable to read/write to directory '{fullPath}'.", ie);
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		private void Dispose(bool disposeManaged)
		{
			if (disposed)
			{
				return;
			}

			lock (disposeLock)
			{
				if (disposed)
				{
					return;
				}

				disposed = true;
				if (!disposeManaged)
				{
					return;
				}

				if (OnChange != null)
				{
					var del = OnChange.GetInvocationList();
					foreach (var item in del)
					{
						var msg = (FriedPipeHandler<Type>)item;
						OnChange -= msg;
					}
				}
				if (watcherList == null)
				{
					return;
				}

				foreach (var watcher in watcherList.Values)
				{
					watcher.EnableRaisingEvents = false;
					watcher.Changed -= OnChanged;
					watcher.Dispose();
				}

				watcherList.Clear();
				watcherList = null;
			}
		}
		~PipeBase()
		{
			Dispose(false);
		}
		#endregion
		#endregion
	}
}
