namespace SenseHatServer.Services;

using SenseHatServer.Devices;
using SenseHatServer.Infrastructure;

public sealed class SenseHatServiceOptions
{
    public string Device { get; set; } = default!;

    public byte Width { get; set; }

    public byte Height { get; set; }
}

public sealed class SenseHatService : IDisposable
{
    private readonly Lock sync = new();

    private readonly TaskFactory workerFactory = new(new LimitedConcurrencyTaskScheduler(1));

    private readonly Stream device;

    private readonly SenseHatImage clearImage;

    private CancellationTokenSource? lastTokenSource;

    private Task? lastRequest;

    public SenseHatService(SenseHatServiceOptions options)
    {
        device = File.OpenWrite(options.Device);
        clearImage = new SenseHatImage(options.Width, options.Height);
    }

    public void Dispose()
    {
        lock (sync)
        {
            CancelInternal();
        }

        lastRequest?.Wait();

        clearImage.Dispose();
        device.Dispose();
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
                catch (TaskCanceledException)
                {
                }
            }, cts.Token);
#pragma warning restore CA2008
        }
    }
}
