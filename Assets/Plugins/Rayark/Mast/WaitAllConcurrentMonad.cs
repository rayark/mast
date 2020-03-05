using System;
using System.Collections;
using System.Collections.Generic;

namespace Rayark.Mast
{
    public class CompletionStatus<T>
    {
        public readonly T Result;
        public readonly Exception Error;
        public CompletionStatus(T r, Exception e)
        {
            Result = r;
            Error = e;
        }

        public CompletionStatus(IMonad<T> m)
        {
            Result = m.Result;
            Error = m.Error;
        }
    }
    
    
    /// <summary>
    /// Executes monads with the same type of return value concurrently.
    /// It will run until all the executed monads are completed whether they are failed.
    /// </summary>
    /// <remarks>
    /// When any monad finished with error, the <see cref="ConcurrentMonad{T}"/> will stop immediately.
    /// This class is rarely used directly. Use the extension method <see cref="Monad.WaitAll{T}(IMonad{T}[])"/>.
    /// </remarks>
    /// <typeparam name="T">The type of the return value</typeparam>
    public class WaitAllConcurrentMonad<T> : IMonad<CompletionStatus<T>[]>
    {
        private readonly IMonad<T>[] _ms;

        /// <summary>
        /// Initialize a new instance of <see cref="ConcurrentMonad{T}"/>.
        /// </summary>
        /// <param name="ms">The monads that will be executed concurrently by the current monad.</param>
        public WaitAllConcurrentMonad(IMonad<T>[] ms)
        {
            _ms = ms;
        }

        /// <summary>
        /// Gets the returned value of the current monad. See <see cref="IMonad{T}.Result"/>.
        /// </summary>
        public CompletionStatus<T>[] Result
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the error of the current monad. See <see cref="IMonad{T}.Error"/>.
        /// </summary>
        public Exception Error
        {
            get { return null; }
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
                    executor.Add(_ms[i].Do());
                }

                executor.Resume(Coroutine.Delta);
                while (!executor.Finished)
                {
                    yield return null;
                    executor.Resume(Coroutine.Delta);
                }
                
                Result = System.Array.ConvertAll(_ms, m => new CompletionStatus<T>(m)) ;
            }
        }
    }

    /// <summary>
    /// Executes two monads with different or the same type of return value concurrently.
    /// It will run until all the executed monads are completed whether they are failed.
    /// </summary>
    /// <typeparam name="T1">The type of the first monad.</typeparam>
    /// <typeparam name="T2">The type of the second monad.</typeparam>
    /// <remarks>
    /// This class is rarely used directly. Use the extension method <see cref="Monad.WaitAll{T1, T2}(IMonad{T1}, IMonad{T2})"/> or LINQ syntax instead if it is possible.
    /// </remarks>
    public class WaitAllConcurrentMonad<T1, T2> : IMonad<Tuple<CompletionStatus<T1>, CompletionStatus<T2>>>
    {
        private readonly IMonad<T1> _m1;
        private readonly IMonad<T2> _m2;

        public WaitAllConcurrentMonad(IMonad<T1> m1, IMonad<T2> m2)
        {
            _m1 = m1;
            _m2 = m2;
        }

        /// <summary>
        /// Gets the returned value of the current monad. See <see cref="IMonad{T}.Result"/>.
        /// </summary>
        /// <remarks>
        /// The <c>Item1</c> and <c>Item2</c> field of the result will be the return value of the first and second monad respectively.
        /// </remarks>
        public Tuple<CompletionStatus<T1>, CompletionStatus<T2>> Result
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the error of the current monad. See <see cref="IMonad{T}.Error"/>.
        /// </summary>
        public Exception Error
        {
            get { return null; }
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

                executor.Add(_m1.Do());
                executor.Add(_m2.Do());

                executor.Resume(Coroutine.Delta);
                while (!executor.Finished)
                {

                    yield return null;
                    executor.Resume(Coroutine.Delta);
                }

                Result = new Tuple<CompletionStatus<T1>, CompletionStatus<T2>>(
                    new CompletionStatus<T1>(_m1), 
                    new CompletionStatus<T2>(_m2));
            }
        }
    }

    /// <summary>
    /// Executes three monads with different or the same type of return value concurrently.
    /// The results of the monads will be presented in the result field.
    /// </summary>
    /// <typeparam name="T1">The type of the first monad.</typeparam>
    /// <typeparam name="T2">The type of the second monad.</typeparam>
    /// <typeparam name="T3">The type of the third monad.</typeparam>
    /// <remarks>
    /// This class is rarely used directly. Use the extension method <see cref="Monad.WaitAll{T1, T2, T3}(IMonad{T1}, IMonad{T2}, IMonad{T3})"/> or LINQ syntax instead if it is possible.
    /// </remarks>
    public class WaitAllConcurrentMonad<T1, T2, T3> : IMonad<Tuple<CompletionStatus<T1>, CompletionStatus<T2>, CompletionStatus<T3>>>
    {
        private readonly IMonad<T1> _m1;
        private readonly IMonad<T2> _m2;
        private readonly IMonad<T3> _m3;

        public WaitAllConcurrentMonad(IMonad<T1> m1, IMonad<T2> m2, IMonad<T3> m3)
        {
            _m1 = m1;
            _m2 = m2;
            _m3 = m3;
        }

        /// <summary>
        /// Gets the returned value of the current monad. See <see cref="IMonad{T}.Result"/>.
        /// </summary>
        /// <remarks>
        /// The <c>Item1</c>, <c>Item2</c>, and <c>Item3</c> fields of the result will be the return value of the first, second and third monads respectively.
        /// </remarks>
        public Tuple<CompletionStatus<T1>, CompletionStatus<T2>, CompletionStatus<T3>> Result
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the error of the current monad. See <see cref="IMonad{T}.Error"/>.
        /// </summary>
        public Exception Error
        {
            get { return null; }
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

                executor.Add(_m1.Do());
                executor.Add(_m2.Do());
                executor.Add(_m3.Do());

                executor.Resume(Coroutine.Delta);
                while (!executor.Finished)
                {
                    if (Error != null)
                    {
                        yield break;
                    }
                    yield return null;
                    executor.Resume(Coroutine.Delta);
                }

                Result = new Tuple<CompletionStatus<T1>, CompletionStatus<T2>, CompletionStatus<T3>>(
                    new CompletionStatus<T1>(_m1), 
                    new CompletionStatus<T2>(_m2), 
                    new CompletionStatus<T3>(_m3));
            }
        }
    }
}
