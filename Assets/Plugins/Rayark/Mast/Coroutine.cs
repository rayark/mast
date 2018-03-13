using System;
using System.Collections;
using System.Collections.Generic;

namespace Rayark.Mast
{

    /// <summary>
    /// Represents a coroutine with an emulated stack.
    /// </summary>
    /// <remarks>
    /// This class is used to provides a execution stack for a running iterator block so that
    /// we can <b>yield</b> a child iterator block, wait for it to be finished, and continue to
    /// executor the current iterator block.
    /// </remarks>
    /// <example>
    /// This is a short example demonstrating the execution stack provided by <see cref="Coroutine"/>.
    /// <code>
    /// IEnumerator GoOutside(){
    ///     yield return Walk( DOOR_POS );
    ///     yield return OpenDoor();
    ///     yield return Walk( DOOR_POS + forward*3.0f);
    /// }
    /// IEnumerator WalkTo(Vector3 pos){
    ///     // Do something
    /// }
    /// 
    /// IEnumerator OpenDoor(){
    ///     ///     // Do something
    /// }
    /// 
    /// Coroutine co = new Coroutine(GoOutside());
    /// 
    /// void FrameUpdate(){
    ///     if( !co.Finished )
    ///         co.Resume(Time.deltaTime);
    /// }
    /// </code>
    /// </example>
    public class Coroutine : IResumable, IDisposable
    {
        /// <summary>
        /// Represents a coroutine operation, which can be `yield return`ed within a coroutine and 
        /// <see cref="Coroutine"/> is reponsible for execute it.
        /// </summary>
        public interface IOperation
        {
            void Execute(Coroutine coroutine);
        }

        /// <summary>
        /// Determine whether the current <see cref="Coroutine"/> is finished.
        /// </summary>
        public bool Finished {
            get
            {
                return _block == null && _next == null;
            }
        }

        Stack<IEnumerator> _stack = null;
        IEnumerator _block = null;
        Coroutine _next = null;

        /// <summary>
        /// Represents an empty coroutine. It will be finished once it is get executed.
        /// </summary>
        public static readonly IEnumerator Empty = new EmptyBlock();

        [ThreadStatic]
        static float _delta;

        /// <summary>
        /// Represents the elapsed time passed to the parent coroutine.
        /// </summary>
        /// <remarks>
        /// This value is used to evaluate <see cref="Coroutine.Sleep(float)"/> is finished.
        /// You may need to pass this value down to the child <see cref="IResumable"/> if the current 
        /// <see cref="Coroutine"/> is not top <see cref="Coroutine"/>.
        /// <code>
        /// IEnumerator ResumeInTenTimesSlower(IResumable resumable){
        ///     while( !resumeable.Finished ){
        ///         resumable.Resume( Coroutine.Delta * 0.1f );
        ///         yield return null;
        ///     }
        /// }
        /// </code>
        /// </remarks>
        public static float Delta {
            get
            {
                return _delta;
            }
            private set
            {
                _delta = value;
            }
        }

        /// <summary>
        /// Initialize a new instance of <see cref="Coroutine"/> with an <a href="http://csharpindepth.com/Articles/Chapter6/IteratorBlockImplementation.aspx">iterator block</a>.
        /// </summary>
        /// <param name="block"></param>
        public Coroutine(IEnumerator block)
        {
            _block = block;
        }

        /// <summary>
        ///  Resumes the current <see cref="Coroutine"/>.
        /// </summary>
        /// <param name="delta"></param>
        public void Resume(float delta)
        {
            float originalDelta = Delta;
            Delta = delta;

            if (_block != null)
                _Resume();

            _ResumeSiblings();

            Delta = originalDelta;
        }

        private void _Resume()
        {
            while(true)
            {
                if (!_block.MoveNext())
                {
                    _DisposeBlock(_block);
                    if (_stack != null &&_stack.Count > 0)
                    {
                        _block = _stack.Pop();
                        continue;
                    }
                    else
                    {
                        _block = null;
                        break;
                    }
                }

                var c = _block.Current;
                if (c == null)
                    break;

                var subBlock = c as IEnumerator;
                if (subBlock != null){
                    if (_stack == null)
                        _stack = new Stack<IEnumerator>();
                    _stack.Push(_block);
                    _block = subBlock;
                    continue;
                }

                var operation = c as IOperation;
                if( operation != null )
                {
                    operation.Execute(this);
                    continue;
                }
                throw new Exception("Return type is not ether an iterator or an operation");
            }
        }

        private void _ResumeSiblings()
        {
            for (Coroutine curr = _next, prev = this; curr != null;)
            {
                curr._Resume();
                if (curr._block == null)
                    prev._next = curr._next;
                else
                    prev = curr;
                curr = curr._next;
            }
        }

        private void _Dispose()
        {
            if (_block != null)
            {
                _DisposeBlock(_block);
                _block = null;
            }

            if( _stack != null )
            {
                foreach (var block in _stack)
                {
                    _DisposeBlock(block);
                }
                _stack.Clear();
            }
        }

        /// <summary>
        /// Dispose the current <see cref="Coroutine"/>.
        /// </summary>
        /// <remarks>
        /// Standard C# languages allows <c>using{}</c> clause to manage resouces within the life time of an iterator block.
        /// By dispose the current <see cref="Coroutine"/>, all the associated iterator blocks will be disposed, so that
        /// any resources allocated with <c>using{}</c> clauses will be released.
        /// </remarks>
        public void Dispose()
        {
            for( var curr = this; curr != null; curr = curr._next)
            {
                curr._Dispose();
            }
        }

        static void _DisposeBlock( IEnumerator block )
        {
            IDisposable disposable = block as IDisposable;
            if (disposable != null)
                disposable.Dispose();
        }


        /// <summary>
        /// A helper interator block that will makes the current <see cref="Coroutine"/> do nothing for a period time caculated via <see cref="Coroutine.Delta"/>.
        /// </summary>
        /// <param name="remain">The sleeping period.</param>
        /// <returns>A sleeping interator block.</returns>
        public static IEnumerator Sleep(float remain)
        {
            while(remain > 0){
                yield return null;
                remain -= Coroutine.Delta;
            }
        }

        /// <summary>
        /// Makes the current iterator block in the stack to be replaced by another iterator block.
        /// </summary>
        /// <param name="block">The next iterator block</param>
        /// <returns>An operation to <c>yield return</c>.</returns>
        /// <remarks>
        /// This method is meant to optmize <b>Tail-Call</b> like behavior of <see cref="Coroutine"/>s.
        /// With this method, we are able to avoid very deep coroutine stacks for recursive style coroutine structures.
        /// </remarks>
        /// <example>
        /// The following example is a recursive style finite state machince implementation with coroutine.
        /// <code>
        /// IEnumerator A(){
        ///     // Do something
        ///     yield return Coroutine.Become(B());
        /// }
        /// 
        /// IEnumerator B(){
        ///     // Do something
        ///     yield return Coroutine.Become(A());
        /// }
        /// </code>
        /// </example>
        public static Coroutine.IOperation Become(IEnumerator block)
        {
            return new BecomeOperation(block);
        }

        private class BecomeOperation : Coroutine.IOperation
        {

            public BecomeOperation(IEnumerator block)
            {
                _target = block;
            }

            IEnumerator _target;

            public void Execute(Coroutine coroutine)
            {
                _DisposeBlock(coroutine._block);
                coroutine._block = _target;
            }
        }
    }

}

