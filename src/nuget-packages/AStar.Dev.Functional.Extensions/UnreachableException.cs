namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     An implementation of an exception that indicates that the code path should be unreachable.
///     This class exists as .Net Standard 2.1 does not have System.Diagnostics.UnreachableException.
/// </summary>
public class UnreachableException : Exception
{
}
