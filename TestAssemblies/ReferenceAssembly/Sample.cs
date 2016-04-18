using System;
using System.ComponentModel;
using WeakEvents.Runtime;

namespace ReferenceAssembly
{
#pragma warning disable 0067

    // Used to confirm the IL that we need to weave.
    public class Sample
    {
        public event EventHandler AutoEH;

        private void WillBeWeak1_Weak_Unsubscribe(EventHandler<EventArgs> weh)
        {
            var realHandler = (EventHandler)DelegateConvert.ChangeType(weh, typeof(EventHandler));
            _WillBeWeak1 = (EventHandler)Delegate.Remove(_WillBeWeak1, realHandler);
        }

        private EventHandler _WillBeWeak1;

        public event EventHandler WillBeWeak1
        {
            add
            {
                EventHandler<EventArgs> generic = (EventHandler<EventArgs>)DelegateConvert.ChangeType(value, typeof(EventHandler<EventArgs>));
                Action<EventHandler<EventArgs>> unsubscribe = new Action<EventHandler<EventArgs>>(WillBeWeak1_Weak_Unsubscribe);
                EventHandler<EventArgs> weakEhT = generic.MakeWeak(unsubscribe);
                EventHandler weakEh = (EventHandler)DelegateConvert.ChangeType(weakEhT, typeof(EventHandler));

                EventHandler handler2;
                EventHandler handler = this._WillBeWeak1;
                do
                {
                    handler2 = handler;
                    EventHandler handler3 = (EventHandler)Delegate.Combine(handler2, weakEh);
                    handler = System.Threading.Interlocked.CompareExchange<EventHandler>(ref this._WillBeWeak1, handler3, handler2);
                }
                while (handler != handler2);
            }
            remove
            {
                EventHandler<EventArgs> generic = (EventHandler<EventArgs>)DelegateConvert.ChangeType(value, typeof(EventHandler<EventArgs>));
                EventHandler<EventArgs> weakEhT = this._WillBeWeak1.FindWeak(generic);
                EventHandler weakEh = (EventHandler)DelegateConvert.ChangeType(weakEhT, typeof(EventHandler));

                EventHandler<AssemblyLoadEventArgs> handler2;
                EventHandler<AssemblyLoadEventArgs> handler = this._eht;
                do
                {
                    handler2 = handler;
                    EventHandler<AssemblyLoadEventArgs> handler3 = (EventHandler<AssemblyLoadEventArgs>)Delegate.Remove(handler2, weakEh);
                    handler = System.Threading.Interlocked.CompareExchange<EventHandler<AssemblyLoadEventArgs>>(ref this._eht, handler3, handler2);
                }
                while (handler != handler2);
            }
        }

        public void FireWillBeWeak1()
        {
            _WillBeWeak1(this, new EventArgs());
        }

        public event PropertyChangedEventHandler AutoPCEH;

        public event EventHandler<AssemblyLoadEventArgs> AutoEHT;

        private void EHT_Weak_Unsubscribe(EventHandler<AssemblyLoadEventArgs> weh)
        {
            _eht -= weh;
        }

        private EventHandler<AssemblyLoadEventArgs> _eht;

        public event EventHandler<AssemblyLoadEventArgs> EHT
        {
            add
            {
                Action<EventHandler<AssemblyLoadEventArgs>> unsubscribe = EHT_Weak_Unsubscribe;
                EventHandler<AssemblyLoadEventArgs> weakEh = value.MakeWeak(unsubscribe);
                _eht += weakEh;
            }
            remove
            {
                EventHandler<AssemblyLoadEventArgs> handler2;
                EventHandler<AssemblyLoadEventArgs> handler = this._eht;
                EventHandler<AssemblyLoadEventArgs> weakEh = this._eht.FindWeak(value);
                do
                {
                    handler2 = handler;
                    EventHandler<AssemblyLoadEventArgs> handler3 = (EventHandler<AssemblyLoadEventArgs>)Delegate.Remove(handler2, weakEh);
                    handler = System.Threading.Interlocked.CompareExchange<EventHandler<AssemblyLoadEventArgs>>(ref this._eht, handler3, handler2);
                }
                while (handler != handler2);
            }
        }

        private static void StaticEhT_Weak_Unsubscribe(EventHandler<AssemblyLoadEventArgs> weh)
        {
            _staticEht -= weh;
        }

        private static EventHandler<AssemblyLoadEventArgs> _staticEht;

        public static event EventHandler<AssemblyLoadEventArgs> StaticEhT
        {
            add
            {
                Action<EventHandler<AssemblyLoadEventArgs>> unsubscribe = StaticEhT_Weak_Unsubscribe;
                EventHandler<AssemblyLoadEventArgs> weakEh = value.MakeWeak(unsubscribe);
                _staticEht += weakEh;
            }
            remove
            {
                EventHandler<AssemblyLoadEventArgs> handler2;
                EventHandler<AssemblyLoadEventArgs> handler = _staticEht;
                EventHandler<AssemblyLoadEventArgs> weakEh = _staticEht.FindWeak(value);
                do
                {
                    handler2 = handler;
                    EventHandler<AssemblyLoadEventArgs> handler3 = (EventHandler<AssemblyLoadEventArgs>)Delegate.Remove(handler2, weakEh);
                    handler = System.Threading.Interlocked.CompareExchange<EventHandler<AssemblyLoadEventArgs>>(ref _staticEht, handler3, handler2);
                }
                while (handler != handler2);
            }
        }
    }

#pragma warning restore 0067
}