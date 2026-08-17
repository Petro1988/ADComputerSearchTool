namespace ADComputerSearchTool.Exceptions;

public sealed class ActiveDirectoryQueryException : Exception
{
    public ActiveDirectoryQueryException(
        string message)
        : base(message)
    {
    }

    public ActiveDirectoryQueryException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}