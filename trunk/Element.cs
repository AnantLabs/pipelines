/*
 * Created by SharpDevelop.
 * User: User
 * Date: 23.04.2008
 * Time: 15:27
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;

namespace Pipeline1
{
	#region Enums
	public enum Status
	{
		Pending, 	//waiting for validation
		Ready, 		//validated, ready to run
		Running,	//running
		Success,	//runned to end, no critical error occured
		Error, 		//critical error occured
		Cancelling, //stopped, waiting to finish
		Cancelled 	//stoped and finished or terminated
	}
	#endregion
	
	#region Events
	public delegate void StatusChangeEventHandler(object source, StatusChangeEventArgs e);
	
	public class StatusChangeEventArgs : EventArgs 
	{
		public Status status;
		public StatusChangeEventArgs(Status status) 
		{
    		this.status = status;
  		}
	}
	#endregion
	
	#region Exceptions
	public class CanNotChangeIdValue : Exception {}
	#endregion
		
	#region Element
	/// <summary>
	/// Description of Element.
	/// </summary>
	public abstract class Element
	{
		#region fields
		private Status _status = Status.Pending;
		private Exception _lastError;
		private string _id = null;
		private Precedence _precedence;
		private ParameterCollection _parameters = new ParameterCollection();
		#endregion
		
		#region properties
		public string Id
		{
			get {return _id;}
			set {
					if (_id != null)
						throw new CanNotChangeIdValue();
					else
						_id = value;
				}
		}
		
		public Status Status 
		{ 
			get {return _status;}
			protected set 
			{
				_status = value; 
				FireStatusChange(_status);
			}
		}
		
		public Exception LastError
		{
			get {return _lastError;}
			protected set {_lastError = value;}
		}
		
		public Precedence Precedence
		{
			get {return _precedence;}
		}
		
		public ParameterCollection Parameters
		{
			get {return _parameters;}
		}
		#endregion
		
		#region events
		public event StatusChangeEventHandler StatusChange;
		#endregion
		
		#region constructors
		public Element()
		{
			_precedence = new Precedence(this);
		}
		#endregion
				
		#region methods
		public abstract void Init();

		public abstract void Start();
		
		public abstract void Stop();
		
		public abstract void Terminate();
		
		protected virtual void FireStatusChange(Status status)
		{
			if (StatusChange != null) // if invocation list not empty
			{
          		StatusChangeEventArgs args = new StatusChangeEventArgs(status);
          		StatusChange(this, args); // fire event
			}
		}		
		#endregion
	}
	#endregion
}
