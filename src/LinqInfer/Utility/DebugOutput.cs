using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LinqInfer.Utility
{
    internal static class DebugOutput
    {
#if DEBUG
        private static readonly Stopwatch _timer;

        static DebugOutput()
        {
            _timer = new Stopwatch();
            _timer.Start();
        }
#endif
        public static void Log<T>(IEnumerable<T> objs)
        {
            if (objs == null) return;

#if DEBUG && !NET_STD
            var callingMethod = new StackFrame(1).GetMethod();
            Log("{0}.{1}() => {2} items:", callingMethod.DeclaringType, callingMethod.Name, objs.Count());
#endif
            foreach (var o in objs)
            {
                Log("\t-" + o);
            }
        }

        public static void Log(object obj)
        {
#if DEBUG
            Console.WriteLine("{0}: {1}", _timer.Elapsed, obj);
            Console.Out.Flush();
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