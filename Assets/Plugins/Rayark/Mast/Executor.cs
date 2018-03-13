using System;
using System.Collections;
using System.Collections.Generic;

namespace Rayark.Mast
{
    /// <summary>
    /// Represents a asynchronous operation that can be resumed until it is finished.
    /// </summary>
    public interface IResumable
    {
        /// <summary>
        /// Determine whether the current <see cref="IResumable"/> is finished.
        /// </summary>
        bool Finished { get; }

        /// <summary>
        /// Resume the current <see cref="IResumable"/> to advance a small progress of the asynchronous operation.
        /// </summary>
        /// <param name="delta"></param>
        void Resume(float delta);
    }

    /// <summary>
    /// Represents a executor which can resume multiple added <see cref="IResumable"/>s concurrently.
    /// </summary>
    /// <remarks>
    /// Note that the current <see cref="IExecutor"/> is resumed, the order of resuming added <see cref="IResumable"/>s will not be guaranteed.
    /// </remarks>
    public interface IExecutor : IResumable
    {
        /// <summary>
        /// Add an <see cref="IResumable"/> to the current <see cref="IExecutor"/> so that the added <see cref="IResumable"/> will be resumed when the current <see cref="IExecutor"/> is resumed.
        /// </summary>
        /// <param name="resumable">The added <see cref="IResumable"/>.</param>
        void Add(IResumable resumable);
    }

    /// <summary>
    /// Represents a executor which can resume multiple added <see cref="IResumable"/>s concurrently. Implments <see cref="IExecutor"/>.
    /// </summary>
    /// <remarks>
    /// Note that the current <see cref="Executor"/> is resumed, the order of resuming added <see cref="IResumable"/>s will not be guaranteed.
    /// </remarks>
    public class Executor : IExecutor, ICollection<IResumable>
    {
        private List<IResumable> _resumables = new List<IResumable>();

        /// <summary>
        /// Gets a boolean value indicate whether the current <see cref="IExecutor"/> is finished.
        /// </summary>
        public bool Finished { get { return _resumables.Count == 0; } }


        /// <summary>
        /// Gets a boolean value indicate whether the current <see cref="IExecutor"/> is empty.
        /// </summary>
        public bool Empty { get { return _resumables.Count == 0; } }

        /// <summary>
        /// Gets the number of <see cref="IResumable"/>s are resumed.
        /// </summary>
        public int Count
        {
            get { return _resumables.Count; }
        }

        /// <summary>
        /// Whether this <see cref="IExecutor"/> is a readonly collection of <see cref="IResumable"/>.
        /// </summary>
        bool ICollection<IResumable>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Initialized a new instance of <see cref="Executor"/>.
        /// </summary>
        public Executor()
        {
            // empty
        }

        /// <summary>
        /// Resumes the current <see cref="Executor"/> so that all the added <see cref="IResumable"/>s will be resumed.
        /// </summary>
        /// <param name="delta">The elapsed time from last resume.</param>
        public void Resume(float delta)
        {
            for(int i = _resumables.Count-1; i >= 0; --i){
                _resumables[i].Resume(delta);
            }

            _resumables.RemoveAll(r => r.Finished);
        }

        /// <summary>
        /// Adds an <see cref="IResumable"/> to the current <see cref="Executor"/> so that the added <see cref="IResumable"/> will be resumed when the current <see cref="IExecutor"/> is resumed.
        /// </summary>
        /// <param name="resumable">The added <see cref="IResumable"/>.</param>
        public void Add(IResumable resumable)
        {
            _resumables.Add(resumable);
        }

        /// <summary>
        /// Removes an <see cref="IResumable"/> from the current <see cref="Executor"/> so that the removed <see cref="IResumable"/> will no longer be resumed. 
        /// </summary>
        /// <param name="resumable">The removed <see cref="IResumable"/></param>
        public bool Remove(IResumable resumable)
        {
            return _resumables.Remove(resumable);
        }

        /// <summary>
        /// Removes all <see cref="IResumable"/>s from the current <see cref="Executor"/> so that no <see cref="IResumable"/> will be resumed. 
        /// </summary>
        public void Clear()
        {
            _resumables.Clear();
        }

        /// <summary>
        /// Determines whether an <see cref="IResumable"/> is in the <see cref="Executor"/>.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(IResumable item)
        {
            return _resumables.Contains(item);
        }

        /// <summary>
        /// Implements <see cref="ICollection{T}.CopyTo(T[], int)"/>.
        /// </summary>
        /// <seealso cref="ICollection{T}"/>
        /// <param name="array">See <see cref="ICollection{T}"/>.</param>
        /// <param name="arrayIndex">See <see cref="ICollection{T}"/>.</param>
        void ICollection<IResumable>.CopyTo(IResumable[] array, int arrayIndex)
        {
            _resumables.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Implements <see cref="IEnumerable{T}.GetEnumerator"/>.
        /// </summary>
        /// <seealso cref="IEnumerable{T}"/>
        /// <param name="array">See <see cref="IEnumerable{T}"/>.</param>
        /// <param name="arrayIndex">See <see cref="IEnumerable{T}"/>.</param>
        IEnumerator<IResumable> IEnumerable<IResumable>.GetEnumerator()
        {
            return _resumables.GetEnumerator();
        }

        /// <summary>
        /// Implements <see cref="IEnumerable.GetEnumerator"/>.
        /// </summary>
        /// <seealso cref="IEnumerable"/>
        /// <param name="array">See <see cref="IEnumerable{T}"/>.</param>
        /// <param name="arrayIndex">See <see cref="IEnumerable{T}"/>.</param>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _resumables.GetEnumerator();
        }


        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="Executor"/>
        /// </summary>
        /// <returns></returns>
        public List<IResumable>.Enumerator GetEnumerator()
        {
            return _resumables.GetEnumerator();
        }
    }
}

