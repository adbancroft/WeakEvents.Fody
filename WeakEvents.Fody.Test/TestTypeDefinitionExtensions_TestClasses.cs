using System;

namespace WeakEvents.Fody.Test
{
#pragma warning disable 0067

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal class NoEvents
    {
    }

    internal interface IInterface
    {
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal class EventButNoAttribute
    {
        public event EventHandler e;
    }

    [ImplementWeakEvents]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal class EventWithAttribute
    {
        public event EventHandler e;
    }

#pragma warning restore 0067
}