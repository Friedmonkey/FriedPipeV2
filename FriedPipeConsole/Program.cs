using FriedPipeV2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace FriedPipeConsole
{
    internal class Program
    {
        public class ExampleObject
        {
            public int radius;
            public int height;
        }
        static void Main(string[] args)
        {
            Async().Wait();
            Console.ReadLine();
        }
        public static async Task Async() 
        {
            ExampleObject obj = new ExampleObject();
            obj.height = 5;
            obj.radius = 10;
            await Task.Delay(1);
            Pipeline<ExampleObject> pipeline = new Pipeline<ExampleObject>(Pipeline_OnAnyChange, "main","cmds","logs");
            pipeline.SendToAll(obj);
        }

		private static void Pipeline_OnAnyChange(PipeBase<ExampleObject> callerPipe, FriedPipeEventArgs<ExampleObject> e)
		{
            ExampleObject obj = e.PipeInfo.PipeObject;
            Console.WriteLine($"pipe {callerPipe.Name} recived: height:{obj.height} radius:{obj.radius}");
		}
	}
}
