using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Code.Helpers.UniTaskHelpers
{
    public interface IAsyncEnumerableWithEvent<T> : IUniTaskAsyncEnumerable<T>
    {
        void AddListener(Action<T> callback);
        void RemoveListener(Action<T> callback);
        UniTask<T> Task(CancellationToken ct);
    }

    public class AsyncEnumerableWithEvent<T> : IAsyncEnumerableWithEvent<T>, IDisposable
    {
        private readonly AsyncReactiveProperty<T> activeProperty = new(default);
        private Action<T> callback;

        public void AddListener(Action<T> callback)
        {
            this.callback += callback;
        }

        public void RemoveListener(Action<T> callback)
        {
            this.callback -= callback;
        }

        public UniTask<T> Task(CancellationToken ct = default) => activeProperty.WaitAsync(ct);

        public void Write(T item)
        {
            callback?.Invoke(item);
            activeProperty.Value = item;
        }

        public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new()) =>
            activeProperty.WithoutCurrent().GetAsyncEnumerator(cancellationToken);

        public void Dispose()
        {
            activeProperty?.Dispose();
            callback = null;
        }
    }
}