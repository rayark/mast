using System.Collections;
using System;
namespace Rayark.Mast
{
    /// <summary>
    /// Executes monads with the same type of return value concurrently.
    /// All monads will stop executing when any monad is ended and meets the criterion.
    /// </summary>
    /// <remarks>
    /// This class is rarely used directly. Use the extension method <see cref="Monad.FirstCompleted{T}(IMonad{T}[])"/> or <see cref="Monad.FirstCompletedOrFaulted{T}(IMonad{T}[])"/>.
    /// </remarks>
    /// <typeparam name="T">The type of the return value</typeparam>
    public class FirstConcurrentMonad<T> : IMonad<T>
    {
        private readonly IMonad<T>[] _ms;
        private IMonad<T> _any;
        private bool _onlyCompleted;

        /// <summary>
        /// Initialize a new instance of <see cref="ConcurrentMonad{T}"/>.
        /// </summary>
        /// <remarks>
        /// When any monad ran into the end with error and onlyCompleted is false, the <see cref="FirstConcurrentMonad{T}"/> will stop immediately.
        /// </remarks>
        /// <param name="ms">The monads that will be executed concurrenly by the current monad.</param>
        /// <param name="onlyCompleted">When onlyCompleted is true, the <see cref="FirstConcurrentMonad{T}"/> will stop only if any monad is comleted or all monads are failed. </param>
        public FirstConcurrentMonad(IMonad<T>[] ms, bool onlyCompleted = false)
        {
            _ms = ms;
            _onlyCompleted = onlyCompleted;
        }

        /// <summary>
        /// Gets the returned value of the current monad. See <see cref="IMonad{T}.Result"/>.
        /// </summary>
        public T Result
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the error of the current monad. See <see cref="IMonad{T}.Error"/>.
        /// </summary>
        public Exception Error
        {
            get;
            private set;
        }

        /// <summary>
        /// Return a iterator block that executes the monad. See <see cref="IMonad{T}.Do"/>.
        /// </summary>
        public IEnumerator Do()
        {
            Executor executor = new Executor();
            using (var defer = new Defer())
            {
                defer.Add(() =>
                {
                    foreach (Coroutine c in executor)
                    {
                        c.Dispose();
                    }
                    executor.Clear();
                });

                for (int i = 0; i < _ms.Length; ++i)
                {
                    executor.Add(_Do(_ms[i]));
                }

                executor.Resume(Coroutine.Delta);
                while (!executor.Finished)
                {
                    if (_any != null)
                    {
                        break;
                    }
                    yield return null;
                    executor.Resume(Coroutine.Delta);
                }

                if (_any == null)
                {
                    Error = new AggregateException(Array.ConvertAll(_ms, m => m.Error));
                    yield break;
                }

                if (_any.Error != null)
                {
                    Error = _any.Error;
                    yield break;
                }
                Result = _any.Result;
            }
        }

        private IEnumerator _Do(IMonad<T> m)
        {
            yield return m.Do();
            if (m.Error != null && _onlyCompleted)
                yield break;
            _any = m;
        }
    }
}