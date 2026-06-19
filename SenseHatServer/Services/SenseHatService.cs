namespace SenseHatServer.Services;

using SenseHatServer.Devices;
using SenseHatServer.Infrastructure;

public sealed class SenseHatServiceOptions
{
    public string Device { get; set; } = default!;

    public byte Width { get; set; }

    public byte Height { get; set; }
}

public sealed partial class SenseHatService : IDisposable, IAsyncDisposable
{
    private readonly Lock sync = new();

    private readonly TaskFactory workerFactory = new(new LimitedConcurrencyTaskScheduler(1));

    private readonly ILogger<SenseHatService> logger;

    private readonly Stream device;

    private readonly SenseHatImage clearImage;

    private CancellationTokenSource? lastTokenSource;

    private Task? lastRequest;

    private bool disposed;

    public byte Width => clearImage.Width;

    public byte Height => clearImage.Height;

    public SenseHatService(SenseHatServiceOptions options, ILogger<SenseHatService> logger)
    {
        this.logger = logger;
        device = File.OpenWrite(options.Device);
        clearImage = new SenseHatImage(options.Width, options.Height);
    }

    public void Dispose()
    {
        Task? request;
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            CancelInternal();
            request = lastRequest;
            lastRequest = null;
        }

        request?.Wait();

        clearImage.Dispose();
        device.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        Task? request;
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            CancelInternal();
            request = lastRequest;
            lastRequest = null;
        }

        if (request is not null)
        {
            await request.ConfigureAwait(false);
        }

        clearImage.Dispose();
        await device.DisposeAsync().ConfigureAwait(false);
    }

    public void Cancel()
    {
        lock (sync)
        {
            CancelInternal();
        }
    }

    private void CancelInternal()
    {
        if (lastTokenSource is not null)
        {
            lastTokenSource.Cancel();
            lastTokenSource.Dispose();
            lastTokenSource = null;
        }
    }

    public void Clear() => RunOnWorker(cancel => clearImage.ShowAsync(device, cancel));

    public void Show(SenseHatImage image) => RunOnWorker(cancel => image.ShowAsync(device, cancel));

    public void Play(SenseHatMovie movie) => RunOnWorker(cancel => movie.PlayAsync(device, cancel));

    private void RunOnWorker(Func<CancellationToken, ValueTask> func)
    {
        lock (sync)
        {
            CancelInternal();

            var cts = new CancellationTokenSource();
            lastTokenSource = cts;

            // ReSharper disable once MethodSupportsCancellation
#pragma warning disable CA2008
            lastRequest = workerFactory.StartNew(async state =>
            {
                try
                {
                    await func((CancellationToken)state!).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
#pragma warning disable CA1031
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    ErrorWorkerFailed(ex);
                }
            }, cts.Token).Unwrap();
#pragma warning restore CA2008
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Sense HAT worker failed.")]
    private partial void ErrorWorkerFailed(Exception exception);
}
