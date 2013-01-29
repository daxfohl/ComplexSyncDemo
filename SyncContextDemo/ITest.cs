using System.Threading;

namespace SyncContextDemo
{
    interface ITest<out T>
    {
        T Run(CancellationToken token);
    }
}