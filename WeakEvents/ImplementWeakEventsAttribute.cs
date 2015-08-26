using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeakEvents
{
    /// <summary>
    /// Specifies that all class events will be weak.
    /// <para>
    /// WeakEvents.Fody will weave a class so that all events are weak. I.e. they do not retain a strong reference to the event subscriber.
    /// Requires a support DLL (WeakEvents.Runtime) be shipped.
    /// </para>
    /// <para>
    /// see https://github.com/Fody/PropertyChanged <see href="https://github.com/Fody/PropertyChanged">(link)</see> for more information.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ImplementWeakEventsAttribute : Attribute
    {
    }
}
