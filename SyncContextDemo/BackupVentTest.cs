using System;
using System.Reactive.Subjects;
using System.Threading;

namespace SyncContextDemo
{
    sealed class BackupVentTest : ITest<BackupVentTestResult>
    {
        static readonly Random RandGen = new Random();
        readonly Subject<BackupVentTestProgress> _progress = new Subject<BackupVentTestProgress>();

        public IObservable<BackupVentTestProgress> Progress
        {
            get { return _progress; }
        }

        public BackupVentTestResult Run(CancellationToken token)
        {
            // Pretend we're doing something
            for (var i = 0; i < 10; ++i)
            {
                Thread.Sleep(100);
                // Check cancelation status every so often, and throw if canceled
                token.ThrowIfCancellationRequested();
                // Otherwise report progress
                var pct = i * 10d;
                _progress.OnNext(new BackupVentTestProgress(pct));
            }
            return new BackupVentTestResult(RandGen.NextDouble());
            // See this just runs synchronouosly; we don't have to
            // worry about switching thread contexts from the
            // test class itself.  Ahhh....nice.
        }
    }

    struct BackupVentTestResult
    {
        public readonly double SomeValue;

        public BackupVentTestResult(double d)
        {
            SomeValue = d;
        }
    }

    struct BackupVentTestProgress
    {
        public readonly double Percent;

        public BackupVentTestProgress(double pct)
        {
            Percent = pct;
        }
    }
}