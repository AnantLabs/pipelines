/*
 * Created by SharpDevelop.
 * User: User
 * Date: 22.04.2008
 * Time: 16:56
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Threading;
using System.Collections;


namespace Pipeline1
{
	#region Exceptions
	public class PortNotConnected : Exception {}
	public class CannotConnectToPort : Exception {}
	public class PortAlreadyConnected : Exception {}
	public class CannotFormatPipe : Exception {}
	#endregion
	
	/// <summary>
	/// Description of Component.
	/// </summary>
	public abstract class Component : Element
	{
		#region const
		public const int DYNAMIC_PORT_COUNT = -1;
		public const int ERROR_PORT = -1;
		#endregion
		
		#region fields
		private Thread _processorThread;
		private Hashtable _inputs = new Hashtable();
		private Hashtable _outputs = new Hashtable();				

		private int _INPUT_PORTS_DEFINED = 0;
		private int _OUTPUT_PORTS_DEFINED = 0;
		
		#endregion
		
		#region properties
		public int INPUT_PORTS_DEFINED
		{
			get {return _INPUT_PORTS_DEFINED;}
			protected set {_INPUT_PORTS_DEFINED = value;}
		}

		public int OUTPUT_PORTS_DEFINED
		{
			get {return _OUTPUT_PORTS_DEFINED;}
			protected set {_OUTPUT_PORTS_DEFINED = value;}
		}
		
		public int InputCount
		{
			get {return _inputs.Count;}
		}

		public int OutputCount
		{
			get {return _outputs.Count;}
		}
		
		protected bool Active
		{
			get {return Status == Status.Running;}
		}
		#endregion
		
		#region methods
		public void ConnectInput(int index,Pipe pipe)
		{
			if (_inputs.ContainsKey(index))
				throw new PortAlreadyConnected();
			if (index < 0 || (index >= INPUT_PORTS_DEFINED))
				throw new CannotConnectToPort();
			pipe.Consumer = this;
			_inputs[index] = pipe;
		}
		public void ConnectInput(Pipe pipe)
		{
			int index = 0;
			if (INPUT_PORTS_DEFINED == DYNAMIC_PORT_COUNT)
				index = InputCount;
			ConnectInput(index,pipe);
		}
		
		public void ConnectOutput(int index,Pipe pipe)
		{
			if (_outputs.ContainsKey(index))
				throw new PortAlreadyConnected();
			if (index < 0 || (index >= OUTPUT_PORTS_DEFINED))
				throw new CannotConnectToPort();
			pipe.Producer = this;
			_outputs[index] = pipe;
		}

		public void ConnectOutput(Pipe pipe)
		{
			int index = 0;
			if (OUTPUT_PORTS_DEFINED == DYNAMIC_PORT_COUNT)
				index = OutputCount;
			ConnectOutput(index,pipe);
		}
		
		public Pipe GetInput(int index)
		{
			if (!_inputs.ContainsKey(index))
				throw new PortNotConnected();
			return (Pipe)_inputs[index];
		}

		public Pipe GetInput()
		{
			return GetInput(0);
		}
		
		public Pipe GetOutput(int index)
		{
			if (!_outputs.ContainsKey(index))
				throw new PortNotConnected();
			return (Pipe)_outputs[index];
			
		}
		
		public Pipe GetOutput()
		{
			return GetOutput(0);
			
		}
		
		public virtual bool GetsDataFrom(Component component)
		{
			for (int i=0; i<InputCount;i++)
			{
				if (GetInput(i).Producer == component)
					return true;
			}
			return false;
		}
				
		public override void Init()
		{
			if (InputCount != DYNAMIC_PORT_COUNT)
			{
				for (int i=0;i<InputCount;i++)
				{
					if (!_inputs.ContainsKey(i))
						throw new PortNotConnected();
				}
			}

			if (OutputCount != DYNAMIC_PORT_COUNT)
			{
				for (int i=0;i<OutputCount;i++)
				{
					if (!_outputs.ContainsKey(i))
						throw new PortNotConnected();
				}
			}
			
			FormatOutputPipes();
			Status = Status.Ready;
		}
		
		protected virtual void FormatOutputPipes()
		{
			for (int i=0;i<OutputCount;i++)
			{
				Pipe pipe = this.GetOutput(i);
				if (!pipe.HasColumns)
				{
					if (this.GetInput().HasColumns)
						pipe.Columns.CopyColumnsFrom(this.GetInput().Columns);
					else
						throw new CannotFormatPipe();
				}
			}
		}
		
		protected virtual void CloseOutputPipes()
		{
			for (int i=0;i<OutputCount;i++)
				this.GetOutput(i).Close();
		}
		
		public override void Start()
		{
			if (Status == Status.Ready)
			{
				Status = Status.Running;
				_processorThread = new Thread(ThreadExecute);
				_processorThread.Start();
			}
		}
		
		public override void Stop()
		{
			if (Status == Status.Running)
				Status = Status.Cancelling;
		}
		
		public override void Terminate()
		{
			if (Status == Status.Running && _processorThread != null && _processorThread.IsAlive)
			{
				_processorThread.Abort();
				_processorThread = null;
				Status = Status.Cancelled;
			}
		}
		
		protected virtual void ThreadExecute()
		{
			try
			{
				Run();
				ThreadFinished();
			}
			catch (Exception e)
			{
				ThreadError(e);
			}
		}
		
		protected abstract void Run();
		
		protected void ThreadFinished()
		{
			CloseOutputPipes();
			
			switch (Status)
			{
				case Status.Cancelling:
					Status = Status.Cancelled;
					break;
				case Status.Running:
					Status = Status.Success;
					break;
			}
			_processorThread = null;
		}
		
		protected void ThreadError(Exception e)
		{
			CloseOutputPipes();
			
			LastError = e;
			Status = Status.Error;
			_processorThread = null;
		}
		#endregion
	}
}
