using System;
using System.Diagnostics;
using System.Reflection;

namespace IORedirectedChild
{
    // A parent process read input.
    //
    // It pass along input to a child, line by line, and await
    // the child to write a string before continuing to read
    // the next line.
    //
    // If the child have terminated, it checks the exit code
    // of the child. If it's 0, it restart the child. If its
    // something else, it terminates itself.

    class Child
    {
        public int Execute(int id)
        {
            while (true)
            {
                try
                {
                    var next = Console.ReadLine();

                    if (next == "sql")
                    {
                        Console.WriteLine($"{id}: {next.Length}");
                    }
                    else if (next == "ddl")
                    {
                        Console.WriteLine();
                        return 0;
                    }
                    else
                    {
                        throw new Exception("Simulate error in child");
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    return 1;
                }
            }
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            var id = Process.GetCurrentProcess().Id;

            if (args.Length > 0)
            {
                return new Child().Execute(id);
            }

            Process child = null;

            while (true)
            {
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    return 0;
                }

                if (child == null || child.HasExited)
                {
                    var self = Assembly.GetExecutingAssembly().Location;

                    var ps = new ProcessStartInfo()
                    {
                        FileName = "dotnet",
                        Arguments = $"{self} 1",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    child = Process.Start(ps);
                }

                child.StandardInput.WriteLine(input);
                
                var ack = child.StandardOutput.ReadLine();
                if (ack == null)
                {
                    var error = child.StandardError.ReadToEnd();
                    Console.WriteLine(error);
                    child.WaitForExit();
                    return child.ExitCode;
                }
                else if (ack == string.Empty)
                {
                    child.WaitForExit();
                    if (child.ExitCode != 0)
                    {
                        throw new Exception("Not according to protocol");
                    }
                }
                else
                {
                    Console.WriteLine(ack);
                    Console.WriteLine();
                }
            }
        }
    }
}
