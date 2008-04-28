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
	#region Exceptions
	public class ComponentMustHaveId : Exception {}
	public class ComponentIdMustBeUnique : Exception {}
	public class ComponentNotFound : Exception {}
	public class CannotFindStartingNode : Exception {}
	#endregion
	
	/// <summary>
	/// Description of Task.
	/// </summary>
	public class Task : Element
	{
		#region fields
		private Dictionary<string, Component> _components = new Dictionary<string, Component>();
		#endregion
		
		#region constructors
		public Task()
		{
			this.Parameters.Context = this.Parameters;
		}
			
		#endregion
		
		#region methods
		public void AddComponent(Component component)
		{
			string id = component.Id;
			
			if (id == null)
				throw new ComponentMustHaveId();
			if (_components.ContainsKey(id))
				throw new ComponentIdMustBeUnique();
			_components[id] = component;
			component.StatusChange += new StatusChangeEventHandler(ComponentStatusChange);
			component.Parameters.Context = this.Parameters;
		}
		
		public Component GetComponent(string id)
		{
			if (!_components.ContainsKey(id))
				throw new ComponentNotFound();
			else
				return _components[id];
		}
		
		public Pipe Connect(string sourceId, int outPort, string targetId, int inPort)
		{
			Pipe pipe = new Pipe();
			GetComponent(sourceId).ConnectOutput(outPort,pipe);
			GetComponent(targetId).ConnectInput(inPort,pipe);
			return pipe;
		}
		
		public Pipe Connect(string sourceId, string targetId)
		{
			return this.Connect(sourceId,0,targetId,0);
		}

		protected void ConnectRootComponents()
		{
			//first disconnect connected rootnodes
			foreach (Component component in _components.Values)
				if (component.Precedence.Has(this))
					component.Precedence.Remove(this);
			
			int rootNodeCount = 0;
			
			foreach (Component component in _components.Values)
			{
				if (component.InputCount == 0 && component.Precedence.Count == 0)//no inputs and no precedences
				{
					component.Precedence.Add(this, PrecedenceEvent.Start);
					rootNodeCount++;
				}
			}

			if (rootNodeCount == 0)
				throw new CannotFindStartingNode();
		}
			
		protected void TreeInit(Element root)
		{
			foreach (Component component in _components.Values)
			{
				if (component.Precedence.Has(root) || (root is Component && component.GetsDataFrom(root as Component)))
				{
					component.Init();
					TreeInit(component);
				}
			}			
		}
		
		public override void Init()
		{
			ConnectRootComponents();
			TreeInit(this);
			
			Status = Status.Ready;
		}
					
		public override void Start()
		{
			if (Status != Status.Ready)
				Init();

			Status = Status.Running;
		}
		
		public override void Stop()
		{
			foreach (Component component in _components.Values)
			{
				if (component.Status == Status.Running)
					component.Stop();
			}		
			
		}
		
		public override void Terminate()
		{
			foreach (Component component in _components.Values)
			{
				if (component.Status == Status.Running)
					component.Stop();
			}					
		}
		
		protected void ComponentStatusChange(object source, StatusChangeEventArgs args)
		{
			switch (args.status)
			{
				case Status.Error:
					ComponentError(source as Component);
					break;
				case Status.Cancelled:
					ComponentFinished(source as Component);
					break;
				case Status.Success:
					ComponentFinished(source as Component);
					break;
			}
		}
		
		protected void ComponentFinished(Component component)
		{
			//TODO:if last component finished then whole task finished
		}
		
		protected void ComponentError(Component component)
		{
			LastError = component.LastError;
			Status = Status.Error;
			Terminate();
		}
				
		#endregion
	}
}
