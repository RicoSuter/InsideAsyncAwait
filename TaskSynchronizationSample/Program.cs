using System;
using System.Threading.Tasks;

namespace TaskSynchronizationSample
{
    class Program
    {
        private static readonly TaskSynchronizationScope _lock = new TaskSynchronizationScope();

        static void Main(string[] args)
        {
            Console.WriteLine("Without locks (tasks are executed in parallel):");
            for (int i = 0; i < 10; i++)
                NotSynchronizedRunAsync(i);
            Console.ReadKey();

            Console.WriteLine("With locks (tasks are executed after each other):");
            for (int i = 0; i < 10; i++)
                RunAsync(i);

            Console.ReadKey();
        }

        private static async Task NotSynchronizedRunAsync(int i)
        {
            Console.WriteLine("Before: " + i);
            await Task.Delay(new Random().Next(300, 500));
            Console.WriteLine("After: " + i);
        }

        private static Task RunAsync(int i)
        {
            return _lock.RunAsync(async () =>
            {
                Console.WriteLine("Before: " + i);
                await Task.Delay(new Random().Next(300, 500));
                Console.WriteLine("After: " + i);
            });
        }
    }

    // The TaskSynchronizationScope class can also be found here: 
    // https://github.com/MyToolkit/MyToolkit/blob/master/src/MyToolkit/Utilities/TaskSynchronizationScope.cs

    /// <summary>Synchronizes tasks so that they are executed after each other.</summary>
    /// <typeparam name="T">The return type of the task.</typeparam>
    public class TaskSynchronizationScope
    {
        private Task _currentTask;
        private readonly object _lock = new object();

        /// <summary>Executes the given task when the previous task has been completed.</summary>
        /// <param name="task">The task function.</param>
        /// <returns>The task.</returns>
        public Task RunAsync(Func<Task> task)
        {
            return RunAsync<object>(async () =>
            {
                await task();
                return null;
            });
        }

        /// <summary>Executes the given task when the previous task has been completed.</summary>
        /// <param name="task">The task function.</param>
        /// <returns>The task.</returns>
        public Task<T> RunAsync<T>(Func<Task<T>> task)
        {
            lock (_lock)
            {
                if (_currentTask == null)
                {
                    var currentTask = task();
                    _currentTask = currentTask;
                    return currentTask;
                }
                else
                {
                    var source = new TaskCompletionSource<T>();
                    _currentTask.ContinueWith(t =>
                    {
                        var nextTask = task();
                        nextTask.ContinueWith(nt =>
                        {
                            if (nt.IsCompleted)
                                source.SetResult(nt.Result);
                            else if (nt.IsFaulted)
                                source.SetException(nt.Exception);
                            else
                                source.SetCanceled();

                            lock (_lock)
                            {
                                if (_currentTask.Status == TaskStatus.RanToCompletion)
                                    _currentTask = null;
                            }
                        });
                    });
                    _currentTask = source.Task;
                    return source.Task;
                }
            }
        }
    }
}
