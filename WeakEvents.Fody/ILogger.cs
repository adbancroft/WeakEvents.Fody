using System;

namespace WeakEvents.Fody
{
    internal interface ILogger
    {
        // Will log an MessageImportance.Normal message to MSBuild.
        Action<string> LogDebug { get; }

        // Will log an MessageImportance.High message to MSBuild.
        Action<string> LogInfo { get; }

        // Will log an warning message to MSBuild.
        Action<string> LogWarning { get; }

        // Will log an error message to MSBuild.
        Action<string> LogError { get; }
    }
}