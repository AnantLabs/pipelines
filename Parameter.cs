/*
 * Created by SharpDevelop.
 * User: User
 * Date: 24.04.2008
 * Time: 19:31
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Pipeline1
{
	#region Exceptions
	public class ContextNotSet : Exception {}
	public class NoExpressionDefined : Exception {}
	public class VariableNotFound : Exception {}
	#endregion
	
	#region Parameter
	public class Parameter
	{
		#region fields
		private object _value;
		private Type _type;
		private string _expression = null;
		private string _variableName = null;
		private ParameterCollection _collection = null;
		private Regex _expressionParamRegex = new Regex(@"\$\((?<param>.*?)\)");
		private ExpressionEvaluator _evaluator = new ExpressionEvaluator();
		#endregion
		
		#region properties
		public string Expression
		{
			get {return _expression;}
			set {_expression = value;}
		}
		
		public string Variable
		{
			get {return _variableName;}
			set {_variableName = value;}
		}
		
		public object Value
		{
			get {return GetValue();}
			set {SetValue(value);}
		}		
		
		#endregion
		
		#region constructors
		public Parameter(ParameterCollection collection, Type type, object value)
		{
			this._collection = collection;
			this._type = type;
			this._value = value; 
		}
		
		public Parameter(ParameterCollection collection, object value) : this(collection,typeof(string),value) {}
		
		public Parameter(ParameterCollection collection, Type type) : this(collection,type,null) {}
		
		public Parameter(ParameterCollection collection) : this(collection,typeof(string), null){}
		#endregion

		#region subclass
		internal class ExpressionEvaluator
		{
			private ParameterCollection _context;
			private int _counter;
			public List<object> _params = new List<object>();
				
			public object[] Values
			{
				get {return _params.ToArray();}
			}
			
			public void Init(ParameterCollection context)
			{
				_context = context;
				_counter = 0;
				_params.Clear();
			}
				
			public string ExpressionMatchEvaluator(Match m)
	      	{
				string result = "{"+_counter.ToString()+"}";
				string param = m.Groups["param"].Value;
				if (!_context.Contains(param))
					throw new VariableNotFound();
				_params.Add(_context[param]);				
	         	_counter++;
	         	return result;        
	      	}
		}
		#endregion
		
		#region methods				
		private object EvaluateExpression()
		{
			CheckContext();

			if (_collection.Context == null)
				throw new ContextNotSet();
			if (_expression == null)
				throw new NoExpressionDefined();
			
			_evaluator.Init(_collection.Context);
			string exp = _expressionParamRegex.Replace(_expression, new MatchEvaluator(_evaluator.ExpressionMatchEvaluator));
						
			return ConvertType(String.Format(exp, _evaluator.Values));
		}		
		
		private object ConvertType(object value)
		{
			return Convert.ChangeType(value,_type);
		}
		
		private void CheckContext()
		{
			if (_collection.Context == null)
				throw new ContextNotSet();
		}
		
		private void SetVariableValue(string variable, object value)
		{
			CheckContext();
			_collection.Context[_variableName] = ConvertType(value);			
		}
		
		private object GetVariableValue(string variable)
		{
			CheckContext();
			return ConvertType(_collection.Context[_variableName]);		
		}
		
		private void SetValue(object value)
		{
			if (_variableName != null)
				SetVariableValue(_variableName,value);
			else
				_value = ConvertType(value);
		}
		
		private object GetValue()
		{
			if (_expression != null)
				return EvaluateExpression();
			if (_variableName != null)
				return GetVariableValue(_variableName);
			return _value;
		}
		#endregion
	}
	#endregion

	#region ParameterCollection
	public class ParameterCollection
	{
		#region fields
		private Dictionary<string, Parameter> _parameters = new Dictionary<string, Parameter>();
		private ParameterCollection _context = null;
		#endregion
		
		#region properties
		public object this[string name]
		{
			get {return _parameters[name].Value;}
			set {_parameters[name].Value = value;}
		}		
		
		public ParameterCollection Context
		{
			get {return _context;}
			set {_context = value;}
		}
		#endregion
		
		#region methods		
		public bool Contains(string name)
		{
			return _parameters.ContainsKey(name);
		}				
		
		public void SetVariable(string param, string variable)
		{
			_parameters[param].Variable = variable;
		}
		
		public void SetExpression(string param, string expression)
		{
			_parameters[param].Expression = expression;
		}
		
		public Parameter Add(string name)
		{
			Parameter param = new Parameter(this);
			_parameters.Add(name,param);
			return param;
		}

		public Parameter Add(string name, Type type)
		{
			Parameter param = new Parameter(this,type);
			_parameters.Add(name,param);
			return param;
		}
		
		public Parameter Add(string name, Type type, object value)
		{
			Parameter param = new Parameter(this,type,value);
			_parameters.Add(name,param);
			return param;
		}
		
		public Parameter Add(string name, object value)
		{
			Parameter param = new Parameter(this,value);
			_parameters.Add(name,param);
			return param;
		}

		public Parameter Get(string name)
		{
			return _parameters[name];
		}
		
		public string AsString(string name)
		{
			return Convert.ToString(_parameters[name].Value);
		}
		#endregion		
	}
	#endregion
}
