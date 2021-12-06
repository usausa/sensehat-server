namespace SenseHatServer.Services;

using System.Diagnostics.CodeAnalysis;

using SenseHatServer.Devices;
using SenseHatServer.Infrastructure;

public sealed class SenseHatServiceOptions
{
    [AllowNull]
    public string Device { get; set; }

    public byte Width { get; set; }

    public byte Height { get; set; }
}

public sealed class SenseHatService : IDisposable
{
    private readonly object sync = new();

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

    public void Clear() =>
        RunOnWorker(async cancel => await clearImage.ShowAsync(device, cancel).ConfigureAwait(false));

    public void ShowAsync(SenseHatImage image) =>
        RunOnWorker(async cancel => await image.ShowAsync(device, cancel).ConfigureAwait(false));

    public void Play(SenseHatMovie movie) =>
        RunOnWorker(async cancel => await movie.PlayAsync(device, cancel).ConfigureAwait(false));

    [SuppressMessage("Microsoft.Reliability", "CA2008:Do not create tasks without passing a TaskScheduler", Justification = "Ignore")]
    private void RunOnWorker(Func<CancellationToken, ValueTask> func)
    {
        lock (sync)
        {
            CancelInternal();

            var cts = new CancellationTokenSource();
            lastTokenSource = cts;

            // ReSharper disable once MethodSupportsCancellation
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
        }
    }
}
