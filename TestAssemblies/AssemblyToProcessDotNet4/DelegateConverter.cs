using System;

namespace AssemblyToProcessDotNet4
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class DelegateConverter : TestInterfaces.IDelegateConverter
    {
        public EventHandler<AssemblyLoadEventArgs> Convert(AssemblyLoadEventHandler handler)
        {
            // The unit test will weave valid code in here.
            throw new NotImplementedException();
        }
    }
}