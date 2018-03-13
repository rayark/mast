using System;

namespace Rayark.Mast
{
    /// <summary>
    /// Provides interface for an iterator block to return a value or report an error.
    /// </summary>
    /// <remarks>
    /// Typically <see cref="IReturn{T}"/> is used as an argument of an iterator block, which is meant to be executed through <see cref="BlockMonad{T}"/>.
    /// </remarks>
    /// <example>
    /// Sleep 1 second and return a value of the <c>float</c> input.
    /// <code>
    /// IEnumerator Main(){
    ///     var m = new BlockMonad&lt;float&gt;( r => DelayFloat(3.0f, r));
    ///     yield return m.Do();
    ///     Assert.That( m.Result == 3.0f );
    /// }
    /// 
    /// static IEnumerator DelayFloat(float input, IReturn&lt;float&gt; ret){
    ///     yield return Coroutine.Sleep(1.0f);
    ///     ret.Accept(input);
    /// }
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of the returned value.</typeparam>
    public interface IReturn<T>
    {
        /// <summary>
        /// Accepts a value as the result.
        /// </summary>
        /// <param name="result">The value.</param>
        void Accept(T result);

        /// <summary>
        /// Reports an error.
        /// </summary>
        /// <param name="error">The error.</param>
        void Fail(Exception error);
    }
}

