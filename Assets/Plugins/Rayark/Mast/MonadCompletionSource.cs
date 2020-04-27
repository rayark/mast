using System;
using System.Collections;

namespace Rayark.Mast
{
    /// <summary>
    /// Represents the producer side of a <see cref="IMonad{T}"/> unbound to a coroutine,
    /// providing access to the consumer side through the Monad property. 
    /// </summary>
    /// <typeparam name="T">The type of the result value associated with this <see cref="MonadCompletionSource{T}"/></typeparam>
    public class MonadCompletionSource<T> : IReturn<T>
    {

        #region private types
        private class InternalMonad : IMonad<T>, IEnumerator
        {
            public bool IsDone = false;

            public T Result
            {
                get;
                set;
            }

            public Exception Error
            {
                get;
                set;
            }
            public IEnumerator Do()
            {
                return this;
            }

            public bool MoveNext()
            {
                return !IsDone;
            }

            public void Reset()
            {
            }

            public object Current
            {
                get { return null; }
            }
        }
        #endregion

        
        readonly InternalMonad _monad = new InternalMonad();

        public IMonad<T> Monad
        {
            get { return _monad; }
        }
        
        public void Accept(T result)
        {
            if (_monad.IsDone)
                throw new System.Exception("Already done");
            _monad.Result = result;
            _monad.IsDone = true;
        }

        public void Fail(Exception error)
        {
            if (_monad.IsDone)
                throw new System.Exception("Already done");
            _monad.Error = error;
            _monad.IsDone = true;
        }
    }
}
