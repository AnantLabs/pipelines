/*
 * Created by SharpDevelop.
 * User: User
 * Date: 24.04.2008
 * Time: 14:19
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Pipeline1
{
	#region Enums
	public enum PrecedenceEvent
	{
		Start,		//Status -> Running
		Success,	//Status -> Success
		Error,		//Status -> Error
		Completion	//Status -> Success,Error,Cancelled
	}
	
	public enum PrecedenceOperator
	{
		And,	//start precedence owner when all element events are fired
		Or		//start precedence owner when first element events is fired
	}
	#endregion
	
	#region Exceptions
	public class CannotChangePropertyValue : Exception {}
	#endregion
	
	/// <summary>
	/// Description of Precedence.
	/// </summary>
	public class Precedence
	{
		#region fields
		private List<Element> _monitorElements = new List<Element>();
		private Hashtable _precedenceEvent = new Hashtable();
		private Element _owner;
		private BitArray _firedElements = null;
		private bool _fired = false;
		private PrecedenceOperator _operator = PrecedenceOperator.And;
		#endregion
		
		#region properties
		public int Count
		{
			get {return _monitorElements.Count;}
		}
		
		public Element this[int index]
		{
			get {return _monitorElements[index];}
		}				
		
		public PrecedenceOperator Operator
		{
			get {return _operator;}
			set {
					if (_firedElements != null)
						throw new CannotChangePropertyValue();
					else
						_operator = value;
				}
		}
		#endregion
		
		#region constructors
		public Precedence(Element owner)
		{
			_owner = owner;
		}
		#endregion
		
		#region methods
		public PrecedenceEvent GetEvent(Element element)
		{
			return (PrecedenceEvent)_precedenceEvent[element];
		}
		
		public PrecedenceEvent GetEvent(int index)
		{
			return GetEvent(this[index]);
		}
		
		public void SetEvent(Element element, PrecedenceEvent pe)
		{
			_precedenceEvent[element] = pe;
		}

		public void SetEvent(int index, PrecedenceEvent pe)
		{
			SetEvent(this[index],pe);
		}
		
		public bool Has(Element element)
		{
			return _monitorElements.Contains(element);
		}
		
		public int IndexOf(Element element)
		{
			return _monitorElements.IndexOf(element);
		}
		
		public void Add(Element element, PrecedenceEvent pe)
		{
			_monitorElements.Add(element);
			_precedenceEvent.Add(element,pe);
			element.StatusChange += new StatusChangeEventHandler(ElementStatusChange);
		}
		
		public void Remove(Element element)
		{
			element.StatusChange -= new StatusChangeEventHandler(ElementStatusChange);
			_precedenceEvent.Remove(element);
			_monitorElements.Remove(element);
		}
			
		public void Remove(int index)
		{
			Remove(this[index]);
		}

		protected bool StatusMatchesEvent(Status status, PrecedenceEvent pe)
		{
			switch (pe)
			{
				case PrecedenceEvent.Start:
					return status == Status.Running;
				case PrecedenceEvent.Error:
					return status == Status.Error;
				case PrecedenceEvent.Success:
					return status == Status.Success;
				case PrecedenceEvent.Completion:
					return (status == Status.Cancelled || status == Status.Error || status == Status.Success);
				default:
					return false;
			}
		}
			
		protected void ElementStatusChange(object source, StatusChangeEventArgs args)
		{
			if (_fired)
				return;
			
			Element element = (source as Element);
			if (_monitorElements.Contains(element) && StatusMatchesEvent(args.status,(PrecedenceEvent)_precedenceEvent[element]))
			{
				if (_operator == PrecedenceOperator.Or)
				{
					_owner.Start();
					_fired = true;
				}
				else
				{
					if (_firedElements == null)
						_firedElements = new BitArray(_monitorElements.Count,false);				
					_firedElements[_monitorElements.IndexOf(element)] = true;
					foreach (bool fired in _firedElements)
						if (!fired)
							return;
					_owner.Start();
					_fired = true;
				}				
			}
		}		
		#endregion
		
	}
}
