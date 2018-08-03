using System;
using System.Collections;

namespace Rayark.Mast
{
    /// <summary>
    /// Implements a tail-recursive loop. 
    /// </summary>
    /// <typeparam name="T">The type of the return value</typeparam>
    public class LoopMonad<T> : IMonad<T>
    {
        T _initialState;
        Func<T, IMonad<Loop<T>>> _reducer;

        /// <summary>
        /// Creates a new monad implementing a tail-recursive loop. 
        /// </summary>
        /// <remarks>
        /// The reducer delegate is immediately called with initialState and should return a value that can be converted to a monad. On successful completion, this monad should output a <see cref="Loop{T}"/> to indicate the status of the loop.
        /// <see cref="Loop.Break{T}(T)"/> halts the loop and completes the monad with output T.
        /// <see cref="Loop.Continue{T}(T)"/> reinvokes the loop function with state T.The returned future will be subsequently polled for a new <see cref="Loop{T}"/> value.
        /// </remarks>
        /// <param name="reducer">The reducer delegate</param>
        public LoopMonad(Func<T, IMonad<Loop<T>>> reducer, T initialState = default(T))
        {
            _initialState = initialState;
            _reducer = reducer;
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

            var loop = Loop.Continue(_initialState);

            do
            {
                var effect = _reducer(loop.State);
                if( effect == null )
                {
                    Error = new System.NullReferenceException("The reducer returns null in LoopMonad");
                    yield break;
                }

                yield return effect.Do();

                if (effect.Error != null)
                {
                    Error = effect.Error;
                    yield break;
                }

                loop = effect.Result;

            } while (!loop.IsBreak);

            Result = loop.State;
        }
    }

    public static class Loop
    {
        public static Loop<T> Break<T>(T state)
        {
            return new Loop<T>(state, true);
        }

        public static Loop<T> Continue<T>(T state)
        {
            return new Loop<T>(state, false);
        }
    }

    public struct Loop<T>
    {
        internal Loop(T state, bool isBreak){
            State = state;
            IsBreak = isBreak;
        }
        public readonly bool IsBreak;
        public readonly T State;
    }
}
