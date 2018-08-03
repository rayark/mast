using System;
using System.Collections;

namespace Rayark.Mast
{

    /// <summary>
    /// Skips frames until the criterion is met.
    /// </summary>
    /// <typeparam name="T">The type of the return value</typeparam>
    public class WaitMonad<T> : IMonad<T>
    {
        readonly System.Func<T, Loop<T>> _reducer;
        readonly T _initialState;

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
        /// Creates a new monad skiping frames until the criterion is met. 
        /// </summary>
        /// <remarks>
        /// The reducer delegate is immediately called with initialState and should return  a <see cref="Loop{T}"/> to indicate whether to skip current frame.
        /// <see cref="Loop.Break{T}(T)"/> halts the loop and completes the monad with output T.
        /// <see cref="Loop.Continue{T}(T)"/> reinvokes the loop function with state T.The returned future will be subsequently polled for a new <see cref="Loop{T}"/> value.
        /// </remarks>
        /// <param name="reducer">The reducer delegate</param>
        public WaitMonad(System.Func<T, Loop<T>> reducer, T initialState)
        {
            _reducer = reducer;
            _initialState = initialState;
        }

        /// <summary>
        /// Return a iterator block that executes the monad. See <see cref="IMonad{T}.Do"/>.
        /// </summary>
        public IEnumerator Do()
        {
            Loop<T> loop = _reducer(_initialState);

            while (!loop.IsBreak)
            {
                yield return null;
                try
                {
                    loop = _reducer(loop.State);
                }
                catch (Exception e)
                {
                    Error = e;
                    break;
                }
            }

            Result = loop.State;
        }
    }


}
