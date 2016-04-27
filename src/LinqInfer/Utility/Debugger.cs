using System;

namespace LinqInfer.Utility
{
    internal static class Debugger
    {
        public static void Log(object obj)
        {
#if DEBUG
            Console.WriteLine(obj);
#else            
            Debug.WriteLine(obj);
#endif
        }

        public static void Log(string msgFormat, params object[] args)
        {
            Log((object)string.Format(msgFormat, args));
        }
    }
}