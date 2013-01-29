using System;
using System.Threading;

namespace SyncContextDemo
{
    // This static class wraps all thread context switching so nobody else has to worry about it, 
    // neither the test nor the GUI.  It's reusable across GUI forms and tests, so you won't have
    // to copy and paste variants of this.  It's generic and all it does is sync threads, so this one
    // class should be able to service the whole project.
    static class TestSynchronizer
    {
        // This is the sole method you'll ever require.  It takes a testRunner, a finishedCallback, and a
        // progressCallback, it runs the test on a new thread, and handles all the thread coordination
        // so that neither the test nor the GUI have to be concerned about them.
        // This should be called from the GUI thread.  
        public static CancellationTokenSource RunTestAsync<TReturn>(this ITest<TReturn> test,
            Action<TestCompletionState, TReturn, Exception> finishedCallback)
        {
            // Get the current context; presumably the GUI that you want to post back to
            var guiContext = SynchronizationContext.Current;

            // Create a cancellation token.
            var cancelSource = new CancellationTokenSource();

            // Run the test in a new thread so the UI doesn't get blocked.
            new Thread(() =>
            {
                var result = default(TReturn);
                Exception exception = null;
                var completionState = TestCompletionState.Normal;
                try
                {
                    result = test.Run(cancelSource.Token);
                }
                catch (Exception ex)
                {
                    exception = ex;
                    completionState = ex is OperationCanceledException
                                          ? TestCompletionState.Cancelled
                                          : TestCompletionState.Exception;
                }
                // Post a message to the GUI thread to run the callback function with the result
                guiContext.Post(_ => finishedCallback(completionState, result, exception), null);
            }) {IsBackground = true}.Start();

            return cancelSource;
        }
    }

    enum TestCompletionState
    {
        Normal,
        Exception,
        Cancelled
    }
}