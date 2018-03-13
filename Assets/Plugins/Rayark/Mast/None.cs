namespace Rayark.Mast
{
    /// <summary>
    /// Represents void value.
    /// </summary>
    /// <remarks>
    /// A coroutine might have parameter of type <see cref="IReturn{T}"/> with <c>T</c> as <see cref="None"/> to indicate that no value but only error may be returned.
    /// In the same manner, <see cref="IMonad{None}"/> with <c>T</c> as <see cref="None"/> represents the fact that no value will be returned as well.
    /// </remarks>
    public struct None
    {
    }
}