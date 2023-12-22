# FriedPipeV2
a library for inter proccess communication (IPC)

this is (imo) a pretty cool library
i made this a while ago but just found out i never put it on github

keep in mind that this library isnt verry speedy or reliable

the basic is that you create a pipe and give it a name
then you can send data between them

the way that it works is with systemfilewatcher
basicly a pipe will create a directory with the message encoded in json into a json file

other programs with the pipe will monitor this and once something changes you can get the object
see FriedPipeConsole's Program.cs for an basic example

however there i use a Pipeline (i just tought it was a cool name)

to have multiple pipes and send on multiple or listen on multiple

this is mostly a passion project as i just tought it was cool

i definitly took some code from someone but dont remember from where if i ever find out ill definitly credit them or add a licence

Pipeline isnt required but it definitely is usefull when working with more than one pipe
we can either
create a pipeline and a pipe and connect them
```
Pipeline<int> intPipeline = new Pipeline<int>("MessagingApp");

Pipe<int> intPipe = new Pipe<int>(Name:"Int",Channel: "MessagingApp");
intPipe.OnRequest += IntPipe_OnRequest;

intPipeline.Connect(intPipe);
```

or we can do it in alot less lines like this
```
Pipeline<int> intPipeline = new Pipeline<int>("MessagingApp","Int");
intPipeline.OnAnyRequest += IntPipe_OnRequest;
```

but if you're not planning on using any pipeline features you can just do
```
Pipe<int> intPipe = new Pipe<int>("Int", "MessagingApp");
intPipe.OnRequest += IntPipe_OnRequest;
```
but then you can never call IntPipe_OnRequest by doing `int result = await intPipe.Request(5);`
not that you'd ever really have to call its own method from the same object.

you could do this
```
            Pipe<int> intPipe = new Pipe<int>("Int", "MessagingApp");
            intPipe.OnRequest += IntPipe_OnRequest;

            Pipe<int> intPipe2 = new Pipe<int>("Int", "MessagingApp");
            intPipe2.OnRequest += IntPipe_OnRequest;

            int result = await intPipe.Request(5);
```
```
request lookin something like this
        private static int IntPipe_OnRequest(PipeBase<int> callerPipe, FriedPipeEventArgs<int> e)
        {
            int num = e.PipeInfo.PipeObject;
            return num * 2;
        }
```

thats about it i guess
i recently added comments/summary so that should hopefully help any other questions


