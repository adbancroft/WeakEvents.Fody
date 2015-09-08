using System;
using System.Diagnostics;
using WeakEvents;

namespace WeakEventPerformanceTester
{
    internal class EventTarget
    {
        public void OnEventHandler(object sender, EventArgs args)
        {
            // No-op
        }
    }

    internal interface IEventSource
    {
        event EventHandler<EventArgs> GenericEvent;

        void InvokeEvent(EventArgs args);
    }

    internal class StrongEventSource : IEventSource
    {
        public event EventHandler<EventArgs> GenericEvent;

        public void InvokeEvent(EventArgs args)
        {
            GenericEvent(this, args);
        }
    }

    [ImplementWeakEvents]
    internal class WeakEventSource : IEventSource
    {
        public event EventHandler<EventArgs> GenericEvent;

        public void InvokeEvent(EventArgs args)
        {
            GenericEvent(this, args);
        }
    }

    internal class Program
    {
        private const int numEvents = 2000;
        private const int numFiring = 5000;

        private long AddHandler(IEventSource source, EventTarget target)
        {
            var stopwatch = Stopwatch.StartNew();
            for (int n = 0; n < numEvents; ++n)
            {
                source.GenericEvent += target.OnEventHandler;
            }
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private long InvokeHandler(IEventSource source)
        {
            var stopwatch = Stopwatch.StartNew();
            EventArgs args = new EventArgs();
            for (int n = 0; n < numFiring; ++n)
            {
                source.InvokeEvent(args);
            }
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private long RemoveHandler(IEventSource source, EventTarget target)
        {
            var stopwatch = Stopwatch.StartNew();
            for (int n = 0; n < numEvents; ++n)
            {
                source.GenericEvent -= target.OnEventHandler;
            }
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private void TestHandler(IEventSource source)
        {
            Console.WriteLine("Test result");
            var target = new EventTarget();

            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("\tAdd: " + AddHandler(source, target) + "ms");
            Console.WriteLine("\tInvoke: " + InvokeHandler(source) + "ms");
            Console.WriteLine("\tRemove: " + RemoveHandler(source, target) + "ms");

            stopwatch.Stop();
            Console.WriteLine("\tOverall: " + stopwatch.ElapsedMilliseconds + "ms");
        }

        public void TestStrongEventHandlerT()
        {
            TestHandler(new StrongEventSource());
        }

        public void TestWeakEventHandlerT()
        {
            TestHandler(new WeakEventSource());
        }

        private static void Main(string[] args)
        {
            var p = new Program();

            Console.WriteLine("TestStrongEventHandlerT");
            p.TestStrongEventHandlerT();
            Console.WriteLine("TestWeakEventHandlerT");
            p.TestWeakEventHandlerT();
        }
    }
}