namespace AssemblyToProcessDotNet4
{
    //[ImplementWeakEvents] // Attribute can't be applied to interface
    internal interface ITestInterface
    {
        // Not a class, shouldn't be touched by the weaver
    }
}