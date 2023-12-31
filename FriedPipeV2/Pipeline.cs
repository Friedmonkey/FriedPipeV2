﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FriedPipeV2
{
	/// <summary>
	/// Default string Pipeline great to use when using multiple pipes
	/// </summary>
	public class Pipeline : PipelineBase<string>
	{
		public Pipeline(string channel = null, params string[] pipeNames) : base(channel, pipeNames) { }
		public Pipeline(FriedPipelineHandler<string> OnAnyChange, string channel = null, params string[] pipeNames) : base(channel, pipeNames)
		{
			this.OnAnyChange += OnAnyChange;
		}
	}
    /// <summary>
    /// Custom class Pipeline great to use when using multiple pipes
    /// </summary>
    public class Pipeline<Type> : PipelineBase<Type>
	{
		public Pipeline(string channel = null, params string[] pipeNames) : base(channel, pipeNames) { }
		public Pipeline(FriedPipelineHandler<Type> OnAnyChange, string channel = null, params string[] pipeNames) : base(channel, pipeNames)
		{
			this.OnAnyChange += OnAnyChange;
		}
	}
}
