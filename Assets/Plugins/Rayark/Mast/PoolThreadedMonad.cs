using System;
using System.Collections;
using System.Threading;

namespace Rayark.Mast
{
    /// <summary>
    /// Evaluates a function with <see cref="ThreadPool.QueueUserWorkItem"/>.
    /// </summary>
    /// <remarks>
    /// PoolThreadedMonad will be more efficient than ThreadedMonad. However, it doesn't support cancellation.
    /// In other words, the specified function won't stop running even if the PoolThreadMonad is interrupted.
    /// On the other hand, PoolThreadedMonad isn't suitable for long-run functions, since there are only limited number
    /// of threads inside the thread pool.
    /// </remarks>
    /// <typeparam name="T">The return type of background thread.</typeparam>
    public class PoolThreadedMonad<T> : IMonad<T>
    {
        /// <summary>
        /// Gets the returned value of the current monad. See <see cref="IMonad{T}.Result"/>.
        /// </summary>
        public T  Result
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

        Func<T> _func;

        /// <summary>
        /// Initialize a new instance of the <see cref="ThreadedMonad{T}"/>.
        /// </summary>
        /// <param name="func">A delegates returns a value. It will be invoked in a newly spawned thread.</param>
        public PoolThreadedMonad( Func<T> func )
        {
            _func = func;
        }

        /// <summary>
        /// Return a iterator block that executes the monad. See <see cref="IMonad{T}.Do"/>.
        /// </summary>
        public IEnumerator Do()
        {
            using (var defer = new Defer())
            {
                bool done = false;
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        Result = _func();
                    }
                    catch (Exception e)
                    {
                        Error = e;
                    }
                    finally
                    {
                        done = true;
                    }
                });
                
                while (!done)
                    yield return null;
            } 
        }
    }
}