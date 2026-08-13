using System.Threading;
using Microsoft.UI.Dispatching;

namespace rainspace.MyThread
{
    public class DispatcherBuild
    {
        // WinUI 3 migration: replaced WPF Dispatcher with DispatcherQueue.
        // DispatcherQueue is the WinUI 3 equivalent for dispatching work to the UI thread.
        // For background thread dispatching, we use DispatcherQueueController.

        /// <summary>
        /// PERF: Creates a dedicated DispatcherQueue for background thread operations.
        /// WinUI 3 uses DispatcherQueue instead of WPF Dispatcher.
        /// </summary>
        public static DispatcherQueue Build()
        {
            // In WinUI 3, we can create a DispatcherQueue on a dedicated thread
            // using DispatcherQueueController.CreateOnDedicatedThread
            var controller = DispatcherQueueController.CreateOnDedicatedThread();
            return controller.DispatcherQueue;
        }
    }
}
