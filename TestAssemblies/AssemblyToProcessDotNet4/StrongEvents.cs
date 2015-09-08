using System;

namespace AssemblyToProcessDotNet4
{
#pragma warning disable 0067

    // Test class that should not be touched by the weaver as it's missing the ImplementWeakEvents attribute
    public class StrongEvents
    {
        public event EventHandler WillBeStrong;
    }

#pragma warning restore 0067
}