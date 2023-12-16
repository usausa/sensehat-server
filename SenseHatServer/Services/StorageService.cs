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
        root = Path.GetFullPath(options.Root);
    }

    private string NormalizePath(string path)
    {
        if (path.EndsWith('/'))
        {
            path = path[..^1];
        }

        var fullPath = Path.Combine(root, path);
        if (fullPath.Length < root.Length)
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
