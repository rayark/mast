using System.Collections;
using System;

namespace Rayark.Mast
{
    /// <summary>
    /// Represents a empty iterator block, which yields no value.
    /// </summary>
    /// <remarks>
    /// <see cref="Coroutine.Empty"/> is a ready-to-hand instance to be used.
    /// </remarks>
    public class EmptyBlock : IEnumerator
    {
        /// <summary>
        /// Implements <see cref="IEnumerator.Current"/>.
        /// </summary>
        /// <seealso cref="IEnumerator"/>
        public object Current
        {
            get
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Implements <see cref="IEnumerator.MoveNext"/>.
        /// </summary>
        /// <seealso cref="IEnumerator"/>
        public bool MoveNext()
        {
            return false;
        }

        /// <summary>
        /// Implements <see cref="IEnumerator.Reset"/>.
        /// </summary>
        /// <seealso cref="IEnumerator"/>
        public void Reset()
        {
        }
    }
}