namespace SenseHatServer.Services;

using System.Diagnostics.CodeAnalysis;

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
    [AllowNull]
    public string Root { get; set; }
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Factory")]
    public ValueTask<Stream?> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        path = NormalizePath(path);

        if (!File.Exists(path))
        {
            return ValueTask.FromResult((Stream?)null);
        }

        return ValueTask.FromResult((Stream?)File.OpenRead(path));
    }
}
