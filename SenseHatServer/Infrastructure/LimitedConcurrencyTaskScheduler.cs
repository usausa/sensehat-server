namespace SenseHatServer.Infrastructure;

public class LimitedConcurrencyTaskScheduler : TaskScheduler
{
    [ThreadStatic]
    private static bool threadIsProcessing;

    private readonly LinkedList<Task> tasks = new();

    private int currentRunningThreads;

    public LimitedConcurrencyTaskScheduler(int maxConcurrency)
    {
        if (maxConcurrency < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
        }

        MaximumConcurrencyLevel = maxConcurrency;
    }

    protected sealed override void QueueTask(Task task)
    {
        lock (tasks)
        {
            tasks.AddLast(task);
            if (currentRunningThreads < MaximumConcurrencyLevel)
            {
                currentRunningThreads++;
                RunNewTask();
            }
        }
    }

    private void RunNewTask()
    {
        ThreadPool.UnsafeQueueUserWorkItem(_ =>
        {
            threadIsProcessing = true;
            try
            {
                while (true)
                {
                    Task item;
                    lock (tasks)
                    {
                        if (tasks.Count == 0)
                        {
                            currentRunningThreads--;
                            break;
                        }

                        item = tasks.First!.Value;
                        tasks.RemoveFirst();
                    }

                    TryExecuteTask(item);
                }
            }
            finally
            {
                threadIsProcessing = false;
            }
        }, null);
    }

    protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        if (!threadIsProcessing)
        {
            return false;
        }

        if (taskWasPreviouslyQueued)
        {
            return TryDequeue(task) && TryExecuteTask(task);
        }

        return TryExecuteTask(task);
    }

    protected sealed override bool TryDequeue(Task task)
    {
        lock (tasks)
        {
            return tasks.Remove(task);
        }
    }

    public sealed override int MaximumConcurrencyLevel { get; }

    protected sealed override IEnumerable<Task> GetScheduledTasks()
    {
        var lockTaken = false;
        try
        {
            Monitor.TryEnter(tasks, ref lockTaken);
            if (lockTaken)
            {
                return tasks;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(tasks);
            }
        }
    }
}
