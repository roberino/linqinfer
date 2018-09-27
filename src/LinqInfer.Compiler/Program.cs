using System;
using System.IO;

namespace LinqInfer.Compiler
{
    class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var comp = new Compilation(new DirectoryInfo(Environment.CurrentDirectory));

                var func = comp.Compile(args);

                Console.Write(func().Result);
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }

            Console.Read();
        }
    }
}
