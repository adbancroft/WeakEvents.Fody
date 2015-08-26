using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestInterfaces
{
    public interface IDelegateConverter
    {
        EventHandler<AssemblyLoadEventArgs> Convert(AssemblyLoadEventHandler handler);
    }
}
