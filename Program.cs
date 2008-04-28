/*
 * Created by SharpDevelop.
 * User: egon
 * Date: 5.04.2008
 * Time: 21:27
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;

namespace Pipeline1
{
			
	public class Producer : Component
	{
		private string _filePath = "FilePath";
				
		public Producer(string id)
		{
			Id = id;
			OUTPUT_PORTS_DEFINED = 1;
			Parameters.Add(_filePath);
		}
		
		protected override void Run()
		{
			Console.WriteLine("{0} started",Id);
			
			PipeWriter writer = GetOutput().Writer;
			string path = Parameters.AsString(_filePath);
			Console.WriteLine("Value={0}",path);
			StreamReader file = new StreamReader(path);
			int count = 0;
			
			while (!file.EndOfStream && DoRun)
			{
				writer[0] = file.ReadLine();
				writer.Flush();
				count++;
			}
			file.Close();			
			//writer.Close();
			Console.WriteLine("[{1}] {2} : Count = {0}",count,DateTime.Now,Id);
		}
	}
	
	public class Filter : Component
	{
		public Filter(string id)
		{
			Id = id;
			INPUT_PORTS_DEFINED = 1;
			OUTPUT_PORTS_DEFINED = 1;
		}
				
		protected override void Run()
		{
			Console.WriteLine("{0} started",Id);
			
			int count = 0;
			PipeReader reader = GetInput().Reader;
			PipeWriter writer = GetOutput().Writer;
			
			while (DoRun && reader.Next())
			{
				string[] items = ((string)reader[0]).Split(",".ToCharArray());
		
				writer[0] = items[0];
				writer[1] = items[1];
				writer[2] = Convert.ToInt64(items[0])+Convert.ToInt64(items[1]);
				
				writer.Flush();
				count++;
			}
			//writer.Close();
			Console.WriteLine("[{1}] {2} : Count = {0}",count,DateTime.Now,Id);
		}
	}
		
	public class Consumer : Component
	{
		private string _filePath = "FilePath";
				
		public Consumer(string id)
		{
			Id = id;
			INPUT_PORTS_DEFINED = 1;
			Parameters.Add(_filePath);
		}
		
		protected override void Run()
		{
			Console.WriteLine("{0} started",Id);
			
			PipeReader reader = GetInput().Reader;
			StreamWriter file = new StreamWriter(Parameters.AsString(_filePath));
			
			int count = 0;

			while (DoRun && reader.Next())
			{
				string line = string.Empty;
				for (int i=0;i<reader.Columns.Count;i++)
				{
					line += reader.AsString(i);
				}
				file.WriteLine(line);
				count++;
				//Console.WriteLine("result: {0}",item.ToString());
			}				
								
			file.Close();
			Console.WriteLine("[{1}] {2} : Count = {0}",count,DateTime.Now, Id);
		}
	}
	class Program
	{
		public static void Main(string[] args)
		{
						
	        Console.WriteLine("Configuring worker threads...");
	        
	        Task task = new Task();
	        task.Parameters.Add("Path",@"c:\etl_beta\data");
	        task.Parameters.Add("InputFileName",@"skype_prefix.txt");
	        task.Parameters.Add("OutputFileName",@"skype_prefix_converted.txt");
	        task.Parameters.Add("InputPath").Expression = @"$(Path)\$(InputFileName)";
	        task.Parameters.Add("OutputPath");
	        task.Parameters.SetExpression("OutputPath", @"$(Path)\$(OutputFileName)");
	        
	        Component comp = new Producer("p1");
	        comp.Parameters.SetVariable("FilePath","InputPath");
	        task.AddComponent(comp);
	        
	        comp = new Filter("f1");
	       	task.AddComponent(comp);
			
	       	comp = new Consumer("c1");
	       	comp.Parameters.SetVariable("FilePath","OutputPath");
	        task.AddComponent(comp);
	        	        
	        Pipe p = task.Connect("p1","f1");
	        p.Columns.Add("line");

	        p = task.Connect("f1","c1");
	        p.Columns.Add("one",Types.BigInt,false,-1);
	        p.Columns.Add("two",Types.BigInt,@"+{0}");
	        p.Columns.Add("sum",Types.BigInt,@"={0}");	        
		        	
	        Console.WriteLine("[{0}] Launching producer and consumer threads...",DateTime.Now);        
	        task.Start();
	        	        	     
	       	Console.ReadKey();

		}
	}
}
