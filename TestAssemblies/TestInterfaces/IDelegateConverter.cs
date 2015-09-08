using System;

namespace TestInterfaces
{
    public interface IDelegateConverter
    {
        EventHandler<AssemblyLoadEventArgs> Convert(AssemblyLoadEventHandler handler);
    }
}