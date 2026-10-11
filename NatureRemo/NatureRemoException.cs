namespace NatureRemo;

/// <summary>Base class for Nature Remo errors.</summary>
public class NatureRemoException : Exception
{
    /// <summary>Creates a new exception with the given message.</summary>
    /// <param name="message">Error message.</param>
    public NatureRemoException(string message) : base(message) { }

    /// <summary>Creates a new exception with the given message and inner exception.</summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public NatureRemoException(string message, Exception innerException)
        : base(message, innerException) { }
}