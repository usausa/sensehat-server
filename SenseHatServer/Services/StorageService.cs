namespace SenseHatServer.Services;

public sealed class StorageException : Exception
{
    public StorageException()
    {
    }

    public StorageException(string message)
        : base(message)
    {
    }

    public StorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class StorageServiceOptions
{
    public string Root { get; set; } = default!;
}

public sealed class StorageService
{
    private readonly string root;

    public StorageService(StorageServiceOptions options)
    {
        var fullRoot = Path.GetFullPath(options.Root);
        root = fullRoot.EndsWith(Path.DirectorySeparatorChar)
            ? fullRoot
            : fullRoot + Path.DirectorySeparatorChar;
    }

    private string NormalizePath(string path)
    {
        var fullPath = Path.GetFullPath(Path.Combine(root, path));
        if (!fullPath.StartsWith(root, StringComparison.Ordinal))
        {
            throw new StorageException("Invalid path.");
        }

        return fullPath;
    }

    public ValueTask<Stream?> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        path = NormalizePath(path);

        if (!File.Exists(path))
        {
            return ValueTask.FromResult((Stream?)null);
        }

#pragma warning disable CA2000
        return ValueTask.FromResult((Stream?)File.OpenRead(path));
#pragma warning restore CA2000
    }
}
