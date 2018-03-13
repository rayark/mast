using System.Collections;

namespace Rayark.Mast
{
    /// <summary>
    /// Provides extensions methods to fertilize the usage of <see cref="Executor"/>.
    /// </summary>
    public static class ExecutorExtensions
    {
        /// <summary>
        /// Adds an interator block to a executor as a coroutine.
        /// </summary>
        /// <param name="executor">The executor.</param>
        /// <param name="block"> The iterator block of the coroutine </param>
        /// <returns>The created coroutine which executes <paramref name="block"/> when resumed by <paramref name="executor"/>.</returns>
        public static Coroutine Add(this IExecutor executor, IEnumerator block)
        {
            Coroutine c = new Coroutine(block);
            executor.Add(c);
            return c;
        }
    }

    /// <summary>
    /// Represents a condition which returns a bool value to determine whether a condition is true.
    /// </summary>
    /// <returns></returns>
    public delegate bool Condition();

    public static class ResumableExtensions
    {
        /// <summary>
        /// Resume a resumable until it is finished.
        /// </summary>
        /// <param name="resumable"></param>
        /// <returns></returns>
        /// <example>
        /// Resume two resumable sequentially.
        /// <code>
        /// IEnumerator ResumeAll( IResumable a, IResumable b ){
        ///     yield return a.Join();
        ///     yield return b.Join();
        /// }
        /// </code>
        /// Resume multiple iterator blocks concurrently.
        /// <code>
        /// IEnumerator RunAll(){
        ///     var executor = new Executor();
        ///     executor.Add(A());
        ///     executor.Add(B());
        ///     yield return executor.Join();
        /// }
        /// </code>
        /// </example>
        public static IEnumerator Join(this IResumable resumable)
        {
            if(!resumable.Finished)
                resumable.Resume(Coroutine.Delta);

            while (!resumable.Finished)
            {
                yield return null;
                resumable.Resume(Coroutine.Delta);
            }
        }

        /// <summary>
        /// Resume a resumable until the condition is met.
        /// </summary>
        /// <param name="resumable">The resumable.</param>
        /// <param name="condition">Condition delegate. It will be evaluated every before resuming.</param>
        /// <returns></returns>
        public static IEnumerator JoinWhile(this IResumable resumable, Condition condition)
        {
            if (!condition())
                resumable.Resume(Coroutine.Delta);

            while (condition())
            {
                yield return null;
                resumable.Resume(Coroutine.Delta);
            }
        }

        /// <summary>
        /// Resume a resumable until it is finished or time is up
        /// </summary>
        /// <param name="resumable">The resumable.</param>
        /// <param name="wait_time">Time to wait for resuming the resumable.</param>
        /// <returns></returns>
        public static IEnumerator TimedJoin(this IResumable resumable, float waitTime)
        {
            if (waitTime > 0 && !resumable.Finished)
                resumable.Resume(Coroutine.Delta);

            while (waitTime > 0 && !resumable.Finished)
            {
                yield return null;
                waitTime -= Coroutine.Delta;
                resumable.Resume(Coroutine.Delta);
            }
        }
    }
}
