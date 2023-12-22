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
            Pipeline<ExampleObject> objPipeline = new Pipeline<ExampleObject>(ObjPipeline_OnAnyChange, "main", "cmds", "logs");
            objPipeline.SendToAll(obj);





            Pipeline pipeline = new Pipeline("MessagingApp");
            pipeline.OnAnyChange += Pipeline_OnAnyChange;

            pipeline.Add("Chat");
            pipeline.Add("Control");
            var chatPipe = pipeline.GetPipe("Chat");

            chatPipe.Send("Hello world"); //will send hello world (which we ALSO pick up)
            chatPipe.Set("Hello world"); //will send hello world (which WE DONT pick up)


            //Pipeline<int> intPipeline = new Pipeline<int>("MessagingApp");

            //Pipe<int> intPipe = new Pipe<int>(Name:"Int",Channel: "MessagingApp");
            //intPipe.OnRequest += IntPipe_OnRequest;

            //intPipeline.Connect(intPipe);

            ////var doubleOf = await intPipe.Request(5);
            ////this doest work it doest trigger its own request so we must simulate one
            ////thats why we use pipeline because it has usefull features like this
            //const int input = 5;
            //var doubleOf = await intPipeline.RequestSpecific(input,"Int");
            //Console.WriteLine($"The double of {input} is {doubleOf}");
            Pipeline<int> intPipeline = new Pipeline<int>("MessagingApp","Int");
            intPipeline.OnAnyRequest += IntPipe_OnRequest;
            const int input = 5;
            var doubleOf = await intPipeline.RequestSpecific(input, "Int");
            Console.WriteLine($"The double of {input} is {doubleOf}");
        }

        private static int IntPipe_OnRequest(PipeBase<int> callerPipe, FriedPipeEventArgs<int> e)
        {
            int num = e.PipeInfo.PipeObject;
            return num * 2;
        }

        private static void Pipeline_OnAnyChange(PipeBase<string> callerPipe, FriedPipeEventArgs<string> e)
        {
            string message = e.PipeInfo.PipeObject;
            if (callerPipe.Name == "Chat")
            {
                Console.WriteLine("[Chat] Got message:" + message);
            }
            else if (callerPipe.Name == "Control")
            { 
                Console.WriteLine("[Control] Got control:" + message);
            }
        }

        private static void ObjPipeline_OnAnyChange(PipeBase<ExampleObject> callerPipe, FriedPipeEventArgs<ExampleObject> e)
		{
            ExampleObject obj = e.PipeInfo.PipeObject;
            Console.WriteLine($"pipe {callerPipe.Name} recived: height:{obj.height} radius:{obj.radius}");
		}
	}
}
