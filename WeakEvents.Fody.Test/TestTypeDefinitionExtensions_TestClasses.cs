using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WeakEvents;

namespace WeakEvents.Fody.Test
{
#pragma warning disable 0067

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    class NoEvents
    {
    }

    interface IInterface
    {
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    class EventButNoAttribute
    {
        public event EventHandler e;
    }

    [ImplementWeakEvents]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    class EventWithAttribute
    {
        public event EventHandler e;
    }

#pragma warning restore 0067
}
