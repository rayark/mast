using System.Collections;
using System;

namespace Rayark.Mast
{
    /// <summary>
    /// Invoke the providing delegate of the type <see cref="System.Func{T}"/> while the monad is being executed.
    /// </summary>
    /// <typeparam name="T">The type of the return value</typeparam>
    /// <remarks>Although invoking  <see cref="System.Func{T}"/> is a trivial operation, wrapping  <see cref="System.Func{T}"/> with <see cref="Rayark.Mast.FuncMonad{T}"/>
    /// makes it possible to add some extra operation right before a complex monad.</remarks>
    public class FuncMonad<T> : IMonad<T>, IEnumerator
    {
        private readonly Func<T> _func;

        public FuncMonad(Func<T> func)
        {
            _func = func;
        }


        public IEnumerator Do()
        {
            return this;
        }

        public T Result { get; private set; }
        public Exception Error { get; private set; }
        bool IEnumerator.MoveNext()
        {
            try
            {
                Result = _func();
            }
            catch (Exception e)
            {
                Error = e;
            }
            return false;
        }

        void IEnumerator.Reset()
        {
        }

        object IEnumerator.Current
        {
            get { return null; }
        }
    }
}