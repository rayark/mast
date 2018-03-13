using System.Collections;
using System.Collections.Generic;
using System;

namespace Rayark.Mast
{
    /// <summary>
    /// Provides a way to automatically perform actions after leaving a scope.
    /// </summary>
    /// <example>
    /// The following example demonstrates how to use <see cref="Defer"/> object to automatically release unmanaged resource.
    /// <code>
    /// void InvokeUnmanaged(byte[] data){
    ///     using (var defer = new Defer()){
    ///         IntPtr dataPointer = Marshal.AllocHGlobal(data.Length);
    ///         // register clearing action to the defer object
    ///         defer.Add(() => Marshal.FreeHGlobal(dataPointer));
    ///         
    ///         Marshal.Copy(data, 0, dataPointer, data.Length);
    ///         
    ///         // do something with dataPointer
    ///         
    ///     }
    ///     // The memeory pointed by dataPointer is free after leaving using scope.
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// <see cref="Defer"/> is usually used with <c>using{}</c> clause. It can be used within a coroutine to
    /// provides a safer way to release resource when an interator block may stopped being executed unintentionally.
    /// </remarks>
    public class Defer : IDisposable
    {
        Action _actions = null;

        /// <summary>
        /// Register an deferred action that will be invoked when <see cref="Dispose"/> gets called.
        /// </summary>
        /// <param name="action"></param>
        public void Add( System.Action action)
        {
            _actions = (_actions == null) ? action : action + _actions;
        }

        /// <summary>
        /// Dispose the current <see cref="Defer"/> object. All registered actions will be invoked.
        /// </summary>
        public void Dispose()
        {
            if (_actions != null)
                _actions();
        }
    }
}
