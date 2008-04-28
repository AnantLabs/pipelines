/*
 * Created by SharpDevelop.
 * User: User
 * Date: 22.04.2008
 * Time: 11:32
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Threading;

namespace Pipeline1
{
	#region Exceptions
	public class PipeAlreadyConnected : Exception {}
	public class CannotWriteToClosedPipe : Exception {}
	#endregion
	
	#region PipeColumns
	public class PipeColumns : DataColumnCollection
	{
		private Pipe _pipe;
		
		public Pipe Pipe
		{
			get {return _pipe;}
		}
		
		public PipeColumns(Pipe pipe)
		{
			_pipe = pipe;
		}
		
		public object[] DefaultValues()
		{
			object[] data = new object[this.Count];
			for (int i=0;i<this.Count;i++)
			{
				data[i] = this[i].DefaultValue;
			}
			return data;
		}
	}
	#endregion

	#region PipeDataRow
	/// <summary>
	/// Note. Use Buffer
	/// </summary>
	public abstract class PipeDataRow : DataRow
	{
		protected object[] _data;
		private Pipe _pipe;
		
		protected Pipe Pipe
		{
			get {return _pipe;}
		}
		
		public PipeDataRow(PipeColumns columns) : base(columns)
		{
			_pipe = columns.Pipe;
		}
		protected override void InitData()
		{
			_data = (Columns as PipeColumns).DefaultValues();
		}
		protected override void SetValue(int index, object value)
		{
			_data[index] = value;
		}
		protected override object GetValue(int index)
		{
			return _data[index];
		}
		
		public void CopyData(PipeDataRow source)
		{
			for (int i=0;i<Columns.Count;i++)
			{
				this[i] = source[i];
			}
		}
		
		public void CopyDataRef(PipeDataRow source)
		{
			_data = source._data;
		}
		
	}
	#endregion
	
	#region PipeReader
	public class PipeReader : PipeDataRow
	{
		public PipeReader(PipeColumns columns) : base(columns) {}
		
		public bool Next()
		{
			return this.Pipe.Read(out this._data);			
		}
	}
	#endregion
	
	#region PipeWriter
	public class PipeWriter : PipeDataRow
	{
		public PipeWriter(PipeColumns columns) : base(columns) {}
		
		public void Flush()
		{
			this.Pipe.Write(this._data);
			InitData();
		}
		
		public void Close()
		{
			this.Pipe.Close();			
		}
	}
	#endregion
	
	#region Pipe
	/// <summary>
	/// Description of Pipe.
	/// </summary>
	public class Pipe
	{
		#region fields
		private object _syncRoot;
		private Queue<object[]> _queue;
		private bool _closed;
		private int _capacity;
		private PipeColumns _columns;
		private Component _producer = null;
		private Component _consumer = null;
		private PipeReader _reader = null;
		private PipeWriter _writer = null;
		private System.Int64 _count = 0;
		#endregion
		
		#region properties		
		public bool EndOfData
		{
			get {	lock (_syncRoot)
					{
						return (_closed && _queue.Count == 0);
					}
				}
		}
		
		public PipeColumns Columns
		{
			get {return _columns;}
		}
		
		public bool HasColumns
		{
			get {return this._columns.Count != 0;}
		}
		
		public Component Producer
		{
			get {return _producer;}
			set {
				if (_producer != null && value != null) //cant connect to already connected pipe
					throw new PipeAlreadyConnected();
				else
					_producer = value;
				}	
		}

		public Component Consumer
		{
			get {return _consumer;}
			set {
				if (_consumer != null && value != null) //cant connect to already connected pipe
					throw new PipeAlreadyConnected();
				else
					_consumer = value;
				}	
		}
		
		public PipeReader Reader
		{
			get { if (_reader == null)
					_reader = new PipeReader(_columns);
				return _reader;
				}
		}
		
		public PipeWriter Writer
		{
			get { if (_writer == null)
					_writer = new PipeWriter(_columns);
				return _writer;
				}
		}
		
		#endregion
		
		#region constructors
		public Pipe(int size)
		{
			_syncRoot = new object();
			_capacity = size;
			_queue = new Queue<object[]>(size);
			_closed = false;
			_columns = new PipeColumns(this);
		}
		
		public Pipe() : this(100) {}
		#endregion
		
		#region methods
		public bool Read(out object[] data)
		{
			lock (_syncRoot)
			{
				while (_queue.Count == 0)
				{
					if (_closed)
					{
						data = null;
						return false;
					}
					Monitor.Wait(_syncRoot);
				}
				
				data = _queue.Dequeue();
				
				if (_queue.Count == _capacity - 1)
					Monitor.Pulse(_syncRoot);
			}
			
			return true;
		}
		
		public void Write(object[] data)
		{
			lock(_syncRoot)
			{					
				while (_queue.Count == _capacity)
					Monitor.Wait(_syncRoot);
				
				if (_closed)
					throw new CannotWriteToClosedPipe();
				
				_queue.Enqueue(data);
				
				if (_count == 0 && _consumer.Status != Status.Running)
					_consumer.Start();
				
				_count++;
				
				if (_queue.Count == 1)
					Monitor.Pulse(_syncRoot);				
			}
			
		}
		
		public void Close()
		{
			lock(_syncRoot)
			{
				_closed = true;
				Monitor.Pulse(_syncRoot);
			}			
		}
				
		#endregion
	}
	#endregion
}
