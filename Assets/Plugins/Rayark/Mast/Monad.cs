using System;
using System.Collections;

namespace Rayark.Mast
{
    /// <summary>
    /// Represents a monad of a task, which returns a value of type <c>T</c> or an error after the monad is finished.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMonad<T>
    {
        /// <summary>
        /// Gets the returned value of the current monad.
        /// </summary>
        /// <remarks>
        /// The returned value will be undefined if the current monad hasn't been finished.
        /// </remarks>
        T Result { get; }

        /// <summary>
        /// Gets the error of the current monad.
        /// </summary>
        /// <remarks>
        /// The error will be undefined if the current monad hasn't been finished.
        /// </remarks>
        Exception Error { get; }

        /// <summary>
        /// Return a iterator block that executes the monad.
        /// </summary>
        /// <remarks>
        /// It is recommended to <c>yield return</c> the result of this method inside an iterator block ran inside a <see cref="Coroutine"/> or create an instance of <see cref="Coroutine"/> with it as the parameter.
        /// </remarks>
        IEnumerator Do();
    }

    /// <summary>
    /// Adapts <a href="http://csharpindepth.com/Articles/Chapter6/IteratorBlockImplementation.aspx">iterator block</a> to <see cref="IMonad{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">ResultType</typeparam>
    public class BlockMonad<T> : IMonad<T>, IReturn<T> {
        public T Result {get; private set;}
        public Exception Error {get; private set;}

        public BlockMonad(Func<IReturn<T>, IEnumerator> impl)
        {
            _doImpl = impl;
        }

        public void Accept(T result)
        {
            Error = null;
            Result = result;
        }

        public void Fail(Exception error)
        {
            Error = error;
        }

        Func<IReturn<T>, IEnumerator> _doImpl;

        public IEnumerator Do()
        {
            return _doImpl(this);
        }
    }

    /// <summary>
    /// Provides extension and static methods that helps the usage of <see cref="IMonad{T}"/>.
    /// </summary>
    public static class Monad
    {
        /// <summary>
        /// Represents a monad which returns no value.
        /// </summary>
        public static readonly SimpleMonad<None> NoneMonad = new SimpleMonad<None>(default(None));

        /// <summary>
        /// Creates a monad returns the given value of type T
        /// </summary>
        public static IMonad<T> With<T>( T value ){
            return new SimpleMonad<T>( value );
        }

        /// <summary>
        /// Creates a monad returns the given error
        /// </summary>
        public static IMonad<T> WithError<T>( Exception e ){
            return new SimpleMonad<T>(e);
        }

        /// <summary>
        /// Adapts <a href="http://csharpindepth.com/Articles/Chapter6/IteratorBlockImplementation.aspx">iterator block</a> to <see cref="IMonad{T}"/> interface.
        /// </summary>
        /// <typeparam name="T">ResultType</typeparam>
        public static IMonad<T> Wrap<T>( Func<IReturn<T>, IEnumerator> f ){
            return new BlockMonad<T>(f);
        }

        /// <summary>
        /// Creates a monad returns the given error. This overloaded method accept a redundant parameter of type T for type inference.
        /// </summary>
        public static IMonad<T> WithError<T>( Exception e, T _){
            return new SimpleMonad<T>(e);
        }

        /// <summary>
        /// Chains two <see cref="IMonad{T}"/> sequentially into a new monad. It is a helper function constructing <see cref="BindMonad{T, U, V}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the return value of the first <see cref="IMonad{T}"/>.</typeparam>
        /// <typeparam name="U">The type of the return value of the second <see cref="IMonad{T}"/>.</typeparam>
        /// <param name="monad">The first monad</param>
        /// <param name="binder">A delegate takes the result of first monad and returns an instance of <see cref="IMonad{T}"/> as MonoTouchAOTHelper second monad.</param>
        /// <remarks>When the chained monad is executed, if an error is returned from the first monad, then the second won't get run. The design is similar to Maybe Monad in certain functional language, which provides a less verbose way of error handling.</remarks>
        /// <example>
        /// <code>            
        /// var m1 = new SimpleMonad&lt;string&gt;("12345");
        /// var m2 = m1.Then( v => new ParseStringMonad(v));
        /// yield return m2.Do();
        /// </code>
        /// </example>
        public static IMonad<U> Then<T, U>(this IMonad<T> monad, Func<T, IMonad<U>> binder)
        {
            return new BindMonad<T, U, U>(monad, binder, (t, u) => u);
        }

        /// <summary>
        /// Chains an action to a monad thus providing a way of producing side effects after the monad is finished.
        /// </summary>
        /// <typeparam name="T">The type of the return value of the monad</typeparam>
        /// <param name="monad">The monad</param>
        /// <param name="binder">An <see cref="Action{T}"/> that producing side effects</param>
        /// <returns></returns>
        public static IMonad<None> Then<T>(this IMonad<T> monad, Action<T> binder)
        {
            return new BindMonad<T, None, None>(monad,
                r => NoneMonad, 
                (t, n) => { binder(t); return n; });
        }

        /// <summary>
        /// Catches the error of a monad.
        /// </summary>
        /// <typeparam name="T">The type of the return value of the monad</typeparam>
        /// <param name="monad">The monad</param>
        /// <param name="handler">A delegate takes the error of the monad and returns a new monad that recovers error.</param>
        /// <returns></returns>
        public static IMonad<T> Catch<T>(this IMonad<T> monad, Func<Exception, IMonad<T>> handler)
        {
            return new CatchMonad<T>(monad, handler);
        }

        /// <summary>
        /// Maps the return value of a monad to another value of any type
        /// </summary>
        /// <typeparam name="T">The type of return value</typeparam>
        /// <typeparam name="U">The type of mapped value</typeparam>
        /// <param name="monad">The monad</param>
        /// <param name="binder">A delegate taks the return value of the monad and returns a new value of type <c>U</c></param>
        public static IMonad<U> Map<T, U>(this IMonad<T> monad, Func<T, U> binder)
        {
            return new BindMonad<T, None, U>(monad, result => NoneMonad, (t, n) => binder(t));
        }

        /// <summary>
        /// Create a <see cref="IMonad{T}"/> that concurrently executes multiple <see cref="IMonad{T}"/>s with the same type of return value.
        /// </summary>
        /// <typeparam name="T">The type of return value</typeparam>
        /// <param name="ms">The <see cref="IMonad{T}"/>s that will be executed concurrently</param>
        /// <returns></returns>
        public static IMonad<T[]> WhenAll<T>(params IMonad<T>[] ms)
        {
            return new ConcurrentMonad<T>(ms);
        }

        /// <summary>
        /// Create a monad that concurrently executes two monad with different or the same type of return value.
        /// </summary>
        /// <typeparam name="T1">The type of return value of first monad</typeparam>
        /// <typeparam name="T2">The type of return value of second monad</typeparam>
        /// <param name="m1">The first monad</param>
        /// <param name="m2">The second monad</param>
        /// <returns></returns>
        public static IMonad<Tuple<T1, T2>> WhenAll<T1, T2>(IMonad<T1> m1, IMonad<T2> m2)
        {
            return new ConcurrentMonad<T1, T2>(m1, m2);
        }


        /// <summary>
        /// Create a monad that concurrently executes three monad with different or the same type of return value.
        /// </summary>
        /// <typeparam name="T1">The type of return value of first monad</typeparam>
        /// <typeparam name="T2">The type of return value of second monad</typeparam>
        /// <typeparam name="T3">The type of return value of third monad</typeparam>
        /// <param name="m1">The first monad</param>
        /// <param name="m2">The second monad</param>
        /// <param name="m3">The third monad</param>
        /// <returns></returns>
        public static IMonad<Tuple<T1, T2, T3>> WhenAll<T1, T2, T3>(IMonad<T1> m1, IMonad<T2> m2, IMonad<T3> m3)
        {
            return new ConcurrentMonad<T1, T2, T3>(m1, m2, m3);
        }

        /// <summary>
        /// Create a <see cref="IMonad{T}"/> that concurrently executes multiple <see cref="IMonad{T}"/>s with the same type of return value. 
        /// All monads will stop executing when any monad ran into the end and is completed.
        /// </summary>
        /// <typeparam name="T">The type of return value</typeparam>
        /// <param name="ms">The <see cref="IMonad{T}"/>s that will be executed concurrently</param>
        /// <returns></returns>
        public static IMonad<T> WhenAnyCompleted<T>(params IMonad<T>[] ms)
        {
            return new FirstConcurrentMonad<T>(ms, true);
        }

        /// <summary>
        /// Create a <see cref="IMonad{T}"/> that concurrently executes multiple <see cref="IMonad{T}"/>s with the same type of return value. 
        /// All monads will stop executing when any monad is copmleted or faulted.
        /// </summary>
        /// <typeparam name="T">The type of return value</typeparam>
        /// <param name="ms">The <see cref="IMonad{T}"/>s that will be executed concurrently</param>
        /// <returns></returns>
        public static IMonad<T> WhenAnyCompletedOrFaulted<T>(params IMonad<T>[] ms)
        {
            return new FirstConcurrentMonad<T>(ms, false);
        }

        /// <summary>
        /// Creates a new monad implementing a tail-recursive loop. 
        /// </summary>
        /// <remarks>
        /// The reducer delegate is immediately called with initialState and should return a Monad. On successful completion, this monad should output a <see cref="Loop{T}"/> to indicate the status of the loop.
        /// <see cref="Loop.Break{T}(T)"/> halts the loop and completes the monad with output T.
        /// <see cref="Loop.Continue{T}(T)"/> reinvokes the loop function with state T.The returned future will be subsequently polled for a new <see cref="Loop{T}"/> value.
        /// </remarks>
        /// <typeparam name="T">The type of the return value</typeparam>
        /// <param name="reducer">The reducer delegate</param>
        public static IMonad<T> Loop<T>(Func<T, IMonad<Loop<T>>> reducer, T initialState = default(T))
        {
            return new LoopMonad<T>(reducer, initialState);
        }


        /// <summary>
        /// Creates a new monad skiping frames until the criterion is met. 
        /// </summary>
        /// <remarks>
        /// The predicator delegate is immediately called should return  a bool to indicate whether to skip current frame.
        /// </remarks>
        /// <typeparam name="T">The type of the return value</typeparam>
        /// <param name="predicator">The predicator delegate</param>
        public static IMonad<None> Wait( Func<bool> predicator)
        {
            return new WaitMonad<None>(
                _ => predicator() 
                    ? Mast.Loop.Continue(default(None)) 
                    : Mast.Loop.Break(default(None)),
                default(None));
        }

        /// <summary>
        /// Creates a new monad skiping frames until the criterion is met. 
        /// </summary>
        /// <remarks>
        /// The reducer delegate is immediately called with initialState and should return  a <see cref="Loop{T}"/> to indicate whether to skip current frame.
        /// <see cref="Loop.Break{T}(T)"/> halts the loop and completes the monad with output T.
        /// <see cref="Loop.Continue{T}(T)"/> reinvokes the loop function with state T.The returned future will be subsequently polled for a new <see cref="Loop{T}"/> value.
        /// </remarks>
        /// <typeparam name="T">The type of the return value</typeparam>
        /// <param name="reducer">The reducer delegate</param>
        public static IMonad<T> Wait<T>( Func<T, Loop<T>> reducer, T initialState = default(T))
        {
            return new WaitMonad<T>(reducer, initialState);
        }
    }

    /// <summary>
    /// Provides LINQ specific extension methods which enables LINQ style query syntax for <see cref="IMonad{T}"/>.
    /// </summary>
    public static class MonadLinq
    {
        /// <summary>
        /// Wraps the creation of <see cref="BindMonad{T, U, V}"/> so that the LINQ expression <c>from ... select ...</c> is supported.
        /// </summary>
        /// <typeparam name="T">The type of the return value of the first monad</typeparam>
        /// <typeparam name="U">The type of the return value of the second monad</typeparam>
        /// <typeparam name="V">The type of mapped value in accordance with the return values of the first and the second monad</typeparam>
        /// <param name="monad">The first monad</param>
        /// <param name="binder">A delegate takes the result of first monad and returns an instance of <see cref="IMonad{T}"/> as the second monad.</param>
        /// <param name="selector">A delegate takes the result of the first and the second monad and returns a value</param>
        /// <remarks>This extension methods is intented to be used via LINQ syntax.</remarks>
        /// <example>
        /// <code>
        /// var m = from v1 in new SimpleMonad&lt;int&gt;(3)
        ///         from v2 in new SimpleMonad&lt;int&gt;(v1*2)
        ///         select v1 + v2;
        /// yield return m.Do();
        /// Assert.That( m.Result == 9 );
        /// </code>
        /// </example>
        public static IMonad<V> SelectMany<T, U, V>(this IMonad<T> monad, Func<T, IMonad<U>> binder, Func<T, U, V> selector)
        {
            return new BindMonad<T, U, V>(monad, binder, selector);
        }
    }

    /// <summary>
    /// Adapts simple value to <see cref="IMonad{T}>"/> interface
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    public class SimpleMonad<T> : IMonad<T>
    {
        public T Result { get; private set; }
        public Exception Error { get; private set; }
        
        public IEnumerator Do()
        {
            return Coroutine.Empty;
        }

        public SimpleMonad(T result)
        {
            Result = result;
        }

        public SimpleMonad(Exception error)
        {
            Error = error;
        }
    }

    /// <summary>
    /// Chains two monads sequentially into single monad. The chained monads executed sequentially, and the <c>Result</c> of first monad will be passed to binder to create second monad.
    /// </summary>
    /// <typeparam name="T">The type of the return value of the first monad</typeparam>
    /// <typeparam name="U">The type of the return value of the second monad</typeparam>
    /// <typeparam name="V">The type of mapped value in accordance with the return values of the first and the second monad</typeparam>
    /// <remarks>This class is rarely used directly. Use the extension method <see cref="Monad.Then{T, U}(IMonad{T}, Func{T, IMonad{U}})"/> or LINQ syntax instead if it is possible.</remarks>
    public class BindMonad<T, U, V> : IMonad<V>
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="BindMonad{T, U, V}"/>.
        /// </summary>
        /// <param name="first">The first monad</param>
        /// <param name="binder">A delegate takes the result of first monad and returns an instance of <see cref="IMonad{T}"/> as the second monad.</param>
        /// <param name="selector">A delegate takes the result of the first and the second monad and returns a value.</param>
        public BindMonad(IMonad<T> first, Func<T, IMonad<U>> binder, Func<T, U, V> selector)
        {
            _first = first;
            _binder = binder;
            _selector = selector;
        }

        /// <summary>
        /// Gets the returned value of the current monad. See <see cref="IMonad{T}.Result"/>.
        /// </summary>
        public V Result
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
            yield return _first.Do();
            if (_first.Error != null)
            {
                Error = _first.Error;
                yield break;
            }

            try
            {
                _second = _binder(_first.Result);
            }
            catch(System.Exception e)
            {
                Error = e;
                yield break;
            }

            yield return _second.Do();
            Error = _second.Error;
            if (Error != null)
                yield break;

            try
            {
                Result = _selector(_first.Result, _second.Result);
            }
            catch (System.Exception e)
            {
                Error = e;
            }
        }

        Func<T, U, V> _selector;
        IMonad<T> _first;
        IMonad<U> _second;
        Func<T, IMonad<U>> _binder;
    }

    /// <summary>
    /// Chains a monad and an error handler, which create a recovery monad according to the error. If the original monad failed with an exception, that exception will be passed to the handler and the returned value becomes the final result.
    /// </summary>
    /// <typeparam name="T">The type of the return value of the recovery monad.</typeparam>
    /// <remarks>This class is rarely used directly. Use the extension method <see cref="Monad.Catch{T}(IMonad{T}, Func{Exception, IMonad{T}})"/> or LINQ syntax instead if it is possible.</remarks>
    public class CatchMonad<T> : IMonad<T>
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="CatchMonad{T}"/>.
        /// </summary>
        /// <param name="first">The first monad</param>
        /// <param name="handler">A error handler which takes an <see cref="System.Exception"/> and returns a new monad which returns a value of type <typeparamref name="T"/> after executed.</param>
        public CatchMonad(IMonad<T> first, Func<Exception, IMonad<T>> handler)
        {
            _first = first;
            _handler = handler;
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
            yield return _first.Do();
            if(_first.Error == null){
                Result = _first.Result;
                Error = null;
                yield break;
            }

            var second = _handler(_first.Error);
            yield return second.Do();
            Result = second.Result;
            Error = second.Error;
        }

        IMonad<T> _first;
        Func<Exception, IMonad<T>> _handler;
    }

    /// <summary>
    /// Executes monads with the same type of return value concurrently.
    /// </summary>
    /// <remarks>
    /// When any monad finished with error, the <see cref="ConcurrentMonad{T}"/> will stop immediately.
    /// This class is rarely used directly. Use the extension method <see cref="Monad.WhenAll{T}(IMonad{T}[])"/> or LINQ syntax instead if it is possible.
    /// </remarks>
    /// <typeparam name="T">The type of the return value</typeparam>
    public class ConcurrentMonad<T> : IMonad<T[]>
    {
        private readonly IMonad<T>[] _ms;

        /// <summary>
        /// Initialize a new instance of <see cref="ConcurrentMonad{T}"/>.
        /// </summary>
        /// <param name="ms">The monads that will be executed concurrenly by the current monad.</param>
        public ConcurrentMonad(IMonad<T>[] ms)
        {
            _ms = ms;
        }

        /// <summary>
        /// Gets the returned value of the current monad. See <see cref="IMonad{T}.Result"/>.
        /// </summary>
        public T[] Result
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
                    if (Error != null)
                    {
                        yield break;
                    }
                    yield return null;
                    executor.Resume(Coroutine.Delta);
                }

                if (Error != null)
                    yield break;
                Result = System.Array.ConvertAll(_ms, m => m.Result);
            }
        }

        private IEnumerator _Do( IMonad<T> m)
        {
            yield return m.Do();
            if (m.Error != null)
                Error = m.Error;
        }
    }

    /// <summary>
    /// Executes two monads with different or the same type of return value concurrently. When any monad finished with error, the CorrurentMonad will stop immediately.
    /// </summary>
    /// <typeparam name="T1">The type of the first monad.</typeparam>
    /// <typeparam name="T2">The type of the second monad.</typeparam>
    /// <remarks>
    /// When any monad finished with error, the <see cref="ConcurrentMonad{T1, T2}"/> will stop immediately.
    /// This class is rarely used directly. Use the extension method <see cref="Monad.WhenAll{T1, T2}(IMonad{T1}, IMonad{T2})"/> or LINQ syntax instead if it is possible.
    /// </remarks>
    public class ConcurrentMonad<T1, T2> : IMonad<Tuple<T1, T2>>
    {
        private readonly IMonad<T1> _m1;
        private readonly IMonad<T2> _m2;

        public ConcurrentMonad(IMonad<T1> m1, IMonad<T2> m2)
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
        public Tuple<T1, T2> Result
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

                executor.Add(_Do(_m1));
                executor.Add(_Do(_m2));

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
                if (Error != null)
                    yield break;
                Result = new Tuple<T1, T2>(_m1.Result, _m2.Result);
            }
        }

        private IEnumerator _Do<U>(IMonad<U> m)
        {
            yield return m.Do();
            if (m.Error != null)
                Error = m.Error;
        }
    }

    /// <summary>
    /// Executes three monads with different or the same type of return value concurrently. When any monad finished with error, the CorrurentMonad will stop immediately.
    /// </summary>
    /// <typeparam name="T1">The type of the first monad.</typeparam>
    /// <typeparam name="T2">The type of the second monad.</typeparam>
    /// <typeparam name="T3">The type of the third monad.</typeparam>
    /// <remarks>
    /// When any monad finished with error, the <see cref="ConcurrentMonad{T1, T2, T3}"/> will stop immediately.
    /// This class is rarely used directly. Use the extension method <see cref="Monad.WhenAll{T1, T2, T3}(IMonad{T1}, IMonad{T2}, IMonad{T3})"/> or LINQ syntax instead if it is possible.
    /// </remarks>
    public class ConcurrentMonad<T1, T2, T3> : IMonad<Tuple<T1, T2, T3>>
    {
        private readonly IMonad<T1> _m1;
        private readonly IMonad<T2> _m2;
        private readonly IMonad<T3> _m3;

        public ConcurrentMonad(IMonad<T1> m1, IMonad<T2> m2, IMonad<T3> m3)
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
        public Tuple<T1, T2, T3> Result
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

                executor.Add(_Do(_m1));
                executor.Add(_Do(_m2));
                executor.Add(_Do(_m3));

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
                if (Error != null)
                    yield break;
                Result = new Tuple<T1, T2, T3>(_m1.Result, _m2.Result, _m3.Result);
            }
        }

        private IEnumerator _Do<U>(IMonad<U> m)
        {
            yield return m.Do();
            if (m.Error != null)
                Error = m.Error;
        }
    }

    /// <summary>
    /// Evaluates a function in a background thread and wait until it returns a value.
    /// </summary>
    /// <typeparam name="T">The return type of background thread.</typeparam>
    public class ThreadedMonad<T> : IMonad<T>
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
        public ThreadedMonad( Func<T> func )
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
                var thread = new System.Threading.Thread(() =>
                {
                    try
                    {
                        Result = _func();
                    }
                    catch (Exception e)
                    {
                        Error = e;
                    }
                });

                thread.Start();
                defer.Add(() =>
                {
                    if (thread.IsAlive)
                        thread.Abort();
                });

                while (thread.IsAlive)
                    yield return null;
            } 
        }
    }
}