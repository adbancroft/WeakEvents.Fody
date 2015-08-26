using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeakEvents.Runtime;

namespace WeakEventPerformanceTester
{
    class Program
    {
        public event EventHandler<EventArgs> StrongEventT;

        private EventHandler<EventArgs> _weakEventT;
        public event EventHandler<EventArgs> WeakEventT
        {
            add { _weakEventT += value.MakeWeak(eventHandler => _weakEventT -= eventHandler); }
            remove { _weakEventT -= _weakEventT.FindWeak(value); }
        }

        private void OnEventHandler(object sender, EventArgs args)
        {
            // No-op
        }

        private const int numEvents = 2000;
        private const int numFiring = 5000;

        private long AddHandler(Action addHandler)
        {
            var stopwatch = Stopwatch.StartNew();
            for (int n = 0; n < numEvents; ++n)
            {
                addHandler();
            }
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private long InvokeHandler<TEventArgs>(Action<object, TEventArgs> invokeHandler)
            where TEventArgs : EventArgs, new()
        {
            var stopwatch = Stopwatch.StartNew();
            TEventArgs args = new TEventArgs();
            for (int n = 0; n < numFiring; ++n)
            {
                invokeHandler(this, args);
            }
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private long RemoveHandler(Action removeHandler)
        {
            var stopwatch = Stopwatch.StartNew();
            for (int n = 0; n < numEvents; ++n)
            {
                removeHandler();
            }
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private void TestHandler<TEventArgs>(Action addHandler, Action<object, TEventArgs> invokeHandler, Action removeHandler)
            where TEventArgs : EventArgs, new()
        {
            Console.WriteLine("Test result");

            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("\tAdd: " + AddHandler(addHandler) + "ms");
            Console.WriteLine("\tInvoke: " + InvokeHandler(invokeHandler) + "ms");
            Console.WriteLine("\tRemove: " + RemoveHandler(removeHandler) + "ms");

            stopwatch.Stop();
            Console.WriteLine("\tOverall: " + stopwatch.ElapsedMilliseconds + "ms");
        }

        public void TestStrongEventHandlerT()
        {
            TestHandler<EventArgs>(() => StrongEventT += OnEventHandler, (sender, args) => StrongEventT(sender, args), () => StrongEventT -= OnEventHandler);
        }

        public void TestWeakEventHandlerT()
        {
            TestHandler<EventArgs>(
                () => WeakEventT += OnEventHandler,
                (sender, args) => _weakEventT(sender, args),
                () => WeakEventT -= OnEventHandler);
        }

        static void Main(string[] args)
        {
            var p = new Program();

            Console.WriteLine("TestStrongEventHandlerT");
            p.TestStrongEventHandlerT();
            Console.WriteLine("TestWeakEventHandlerT");
            p.TestWeakEventHandlerT();
        }
    }
}
