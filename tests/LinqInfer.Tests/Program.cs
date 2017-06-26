using NUnitLite;

namespace LinqInfer.Tests
{
    public class Program 
    {
        public static int Main(string[] args)
        {
            return new AutoRun(TestFixtureBase.GetAssembly()).Execute(args);
        }
    }
}