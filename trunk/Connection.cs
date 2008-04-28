/*
 * Created by SharpDevelop.
 * User: User
 * Date: 27.04.2008
 * Time: 16:23
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;

namespace Pipeline1
{
	/// <summary>
	/// Description of Connection.
	/// ConnectionManager, Singleton, contails list of connections
	/// 
	/// AcuireConnection(connectionId,componentId)
	/// if available > 0 and queue is empty, available--, return
	/// else add to queue & block until some thread calls release. then check if first in queue. if 
	/// true, dequeue, available--
	/// ReleaseConnection(connectionId) 
	/// available++, monitor.pulse
	/// 
	/// </summary>
	
	public class Connection
	{
			
	}
	
	public class ConnectionManager
	{
		private static volatile ConnectionManager instance;
		private static object syncRoot = new Object();
		
		private ConnectionManager() {}
		
		public static ConnectionManager Instance
		{
		  get 
		  {
		     if (instance == null) 
		     {
		        lock (syncRoot) 
		        {
		           if (instance == null) 
		              instance = new ConnectionManager();
		        }
		     }
		
		     return instance;
		  }
		}	
	}
}
