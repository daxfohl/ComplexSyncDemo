using System;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Forms;

namespace SyncContextDemo
{
    // Note because of the TestSynchronizer there doesn't need to be any explicit thread handling here.
    // Just a regular Form class with some callback functions.
    sealed partial class Form1 : Form
    {
        // This needs to be a member so it can be accessed by the Cancel button.
        // It is set in RunTest, and reset to null in onTestFinished.
        CancellationTokenSource _testCancelSource;

        public Form1()
        {
            InitializeComponent();
        }

        void _btnStart_Click(object sender, EventArgs e)
        {
            var backupVentTest = new BackupVentTest();

            // Subscribe to the Rx event on the current synchronization context.
            backupVentTest.Progress.ObserveOn(SynchronizationContext.Current).Subscribe(OnTestProgress);
            // Note you don't have to unsubscribe, as the "subscription" is simply a list of callbacks
            // maintained on the test class (inside the _progress Subject).  Thus it simply gets garbage
            // collected when there are no more references; in this case, as soon as onTestFinished gets called.

            RunTest(backupVentTest, OnBackupVentTestResult);
        }

        void _btnCancel_Click(object sender, EventArgs e)
        {
            if (_testCancelSource != null)
            {
                _testCancelSource.Cancel();
            }
        }

        void OnBackupVentTestResult(BackupVentTestResult result)
        {
            Text = "Value = " + result.SomeValue;
        }

        void OnTestProgress(BackupVentTestProgress progress)
        {
            Text = progress.Percent + "% complete";
        }

        // Helper member function that wraps exception handling and ensures one test at a time
        void RunTest<TReturn>(ITest<TReturn> test, Action<TReturn> resultCallback)
        {
            // Only allow one test at a time.
            if (_testCancelSource != null)
            {
                return;
            }

            // Wrap the callback with an exception handler and ensure _currentTest is deleted at the end.
            // Any cleanup code should be added here too.  TestSynchronizer guarantees that this will
            // be called exactly once per test run.
            Action<TestCompletionState, TReturn, Exception> onTestFinished =
                (state, result, exception) =>
                {
                    switch (state)
                    {
                        case TestCompletionState.Normal:
                            resultCallback(result);
                            break;
                        case TestCompletionState.Cancelled:
                            Text = "Canceled";
                            break;
                        default:
                            Text = exception.ToString();
                            break;
                    }
                    // clean up
                    _testCancelSource = null;
                };

            // Run the test within the thread synchronizer to make sure your callback
            // gets called on the appropriate thread.
            _testCancelSource = test.RunTestAsync(onTestFinished);
        }
    }
}