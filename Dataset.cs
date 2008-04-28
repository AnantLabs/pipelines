/*
 * Created by SharpDevelop.
 * User: User
 * Date: 21.04.2008
 * Time: 21:31
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Pipeline1	
{
	#region Exceptions
	public class NoNullValuesAllowed : Exception
	{		
	}
	
	public class PropertyChangeNotAllowed : Exception
	{		
	}
	#endregion
		
	#region Events
	public delegate void PropertyChangeEventHandler(object source, PropertyChangeEventArgs e);
	
	public class PropertyChangeEventArgs : EventArgs 
	{
		public object newValue;
		public object oldValue;
		public string propertyName;
		public bool cancel = false;
		public PropertyChangeEventArgs(string propertyName,object newValue,object oldValue) 
		{
    		this.propertyName = propertyName;
    		this.oldValue = oldValue;
    		this.newValue = newValue;
  		}
	}
	#endregion
	
	#region Types
	public sealed class Types
	{
		public static readonly Type String = typeof(string);
		public static readonly Type BigInt = typeof(System.Int64);
		public static readonly Type Int = typeof(int);
		
		#region constructor
		private Types(){}
		#endregion
			
	}
	#endregion
	
	#region DataColumn
	public class DataColumn
	{
		#region fields
		private string _name;
		private Type _dataType;
		private bool _nullable;
		private string _format;
		private object _defaultValue;
		#endregion
		
		#region properties
		public string Name
		{
			get {return _name;}
			set {
					if (OnPropertyChanging("Name",value,_name))
						_name = value;
					else
						throw new PropertyChangeNotAllowed();
				}
		}
		public Type DataType
		{
			get {return _dataType;}
			set {
					if (OnPropertyChanging("DataType",value,_dataType))
						_dataType = value;
					else
						throw new PropertyChangeNotAllowed();
				}
		}
		public bool Nullable
		{
			get {return _nullable;}
			set {
					if (OnPropertyChanging("Nullable",value,_nullable))
						_nullable = value;
					else
						throw new PropertyChangeNotAllowed();
				}
		}
		public string Format
		{
			get {return _format;}
			set {
					if (OnPropertyChanging("Format",value,_format))
						_format = value;
					else
						throw new PropertyChangeNotAllowed();
				}
			
		}
		public bool HasFormat
		{
			get {return _format != string.Empty;}
		}
		public object DefaultValue
		{
			get {return _defaultValue;}
			set {_defaultValue = value;}
		}
		#endregion

		#region events
		public event PropertyChangeEventHandler PropertyChange;
		#endregion
		
		#region constructors
		public DataColumn() : this(string.Empty,typeof(string),false,string.Empty){}

		public DataColumn(string name) : this(name,typeof(string),true,null,string.Empty){}

		public DataColumn(string name,Type dataType) : this(name,dataType,true,null,string.Empty){}

		public DataColumn(string name,Type dataType, string format) : this(name,dataType,true,null,format){}
		
		public DataColumn(string name,Type dataType, bool nullable, object defaultValue) : this(name,dataType,nullable,defaultValue,string.Empty){}

		public DataColumn(string name,Type dataType, bool nullable, object defaultValue, string format)
		{
			_name = name;
			_dataType = dataType;
			_nullable = nullable;
			_defaultValue = defaultValue;
			_format = format;
		}		
		#endregion
		
		public object ConvertValueType(object value)
		{
			if (value == null)
				return value;
			else
				return Convert.ChangeType(value,DataType);
		}
		public virtual DataColumn Clone()
		{
			DataColumn dc = new DataColumn();
			dc._name = _name;
			dc._dataType = _dataType;
			dc._defaultValue = _defaultValue;
			dc._format = _format;
			dc._nullable = _nullable;
			
			return dc;
		}
		
		public virtual bool Equals(DataColumn column)
		{
			return (_name == column._name &&
			       	_dataType == column._dataType &&
			        _defaultValue == column._defaultValue &&
			      	_format == column._format &&
			        _nullable == column._nullable);
		}
		protected virtual bool OnPropertyChanging(string propertyName,object newValue,object oldValue)
		{
			bool result = true;
			if (PropertyChange != null) // if invocation list not empty
			{
          		PropertyChangeEventArgs args = new PropertyChangeEventArgs(propertyName,newValue,oldValue);
          		PropertyChange(this, args); // fire event
          		result = !args.cancel;          			
			}
			return result;
		}
			
	}
	#endregion
	
	#region DataColumnCollection
	public class DataColumnCollection		
	{
		#region fields
		private List<DataColumn> _columns = new List<DataColumn>();
		private Dictionary<string, int> _columnNames = new Dictionary<string, int>();
		#endregion
		
		#region properties
		public int Count
		{
			get {return _columns.Count;}
		}		
		public bool HasColumns
		{
			get {return Count == 0;}
		}
		#endregion
		
		public DataColumn this[int index]
		{
			get {return _columns[index];}
		}
		public DataColumn this[string index]
		{
			get {return _columns[_columnNames[index]];}
		}
		public void Add(DataColumn column)
		{
			if (column.Name == string.Empty)
				column.Name = String.Format("Column{0}",_columns.Count);
			_columns.Add(column);
			UpdateColumnNames();
			column.PropertyChange += new PropertyChangeEventHandler(ColumnPropertyChange);
		}
		public DataColumn Add()
		{
			DataColumn column = new DataColumn();
			this.Add(column);
			return column;
		}
		public DataColumn Add(string name)
		{
			DataColumn column = new DataColumn(name);
			this.Add(column);
			return column;
		}
		public DataColumn Add(string name,Type dataType)
		{
			DataColumn column = new DataColumn(name,dataType);
			this.Add(column);
			return column;
		}
		public DataColumn Add(string name,Type dataType,string format)
		{
			DataColumn column = new DataColumn(name,dataType,format);
			this.Add(column);
			return column;
		}
		public DataColumn Add(string name,Type dataType,bool nullable,object defaultValue)
		{
			DataColumn column = new DataColumn(name,dataType,nullable,defaultValue);
			this.Add(column);
			return column;
		}
		public DataColumn Add(string name,Type dataType,bool nullable,object defaultValue, string format)
		{
			DataColumn column = new DataColumn(name,dataType,nullable,defaultValue,format);
			this.Add(column);
			return column;
		}
		public int IndexOf(string name)
		{
			return _columnNames[name];
		}
		
		public string ColumnName(int index)
		{
			foreach (KeyValuePair<string, int> entry in _columnNames)
			{
				if (entry.Value == index)
					return entry.Key;
			}
			return string.Empty;
		}
		
		private void UpdateColumnNames()
		{
			_columnNames.Clear();
			for (int i=0;i<_columns.Count;i++)				
			{
				if (_columns[i].Name != string.Empty)
				{
					_columnNames.Add(_columns[i].Name,i);
				}
			}
		}
		protected void ColumnPropertyChange(object source,PropertyChangeEventArgs args)
		{
			if (args.propertyName == "Name")
			{
				UpdateColumnNames();
			}
		}
		
		public void CopyColumnsFrom(DataColumnCollection columns)
		{
			foreach (DataColumn column in columns._columns)
			{
				this.Add(column.Clone());
			}
		}
		
		
	}
	#endregion
	
	#region DataRow	
	public abstract class DataRow
	{
		private DataColumnCollection _columns;
		
		public DataColumnCollection Columns
		{
			get {return _columns;}
		}
		
		public DataRow(DataColumnCollection columns)
		{
			this._columns = columns;
			this.InitData();
			
		}
		public object this[string name]
		{
			get {return this[this._columns.IndexOf(name)];}
			set {this[this._columns.IndexOf(name)] = value;}
		}
		public object this[int index]
		{
			get {return this.GetValue(index);}
			set {
				if (value == null && !_columns[index].Nullable)
					throw new NoNullValuesAllowed();
				else
					this.SetValue(index,_columns[index].ConvertValueType(value));}
		}
		public string AsString(int index)
		{
			if (IsNull(index))
				return string.Empty;
			
			if (_columns[index].HasFormat)
				return String.Format(_columns[index].Format,this.GetValue(index));
			else
				return Convert.ToString(this.GetValue(index));				
		}
		public string AsString(string name)
		{
			return AsString(_columns.IndexOf(name));
		}
		public bool IsNull(int index)
		{
			return (GetValue(index) == null);
		}
		public bool IsNull(string name)
		{
			return IsNull(_columns.IndexOf(name));
		}
			
		protected abstract void InitData();
		protected abstract void SetValue(int index, object value);
		protected abstract object GetValue(int index);
		
	}
	#endregion
	
}
