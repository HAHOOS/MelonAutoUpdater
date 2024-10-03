using MelonAutoUpdater.Search;
using System;

namespace MelonAutoUpdater.Utils
{
    /// <summary>
    /// Class providing utilities for ensuring that the plugin does not crash while running extension methods
    /// </summary>
    public static class Safe
    {
        /// <summary>
        /// Run an <see cref="Action"/> safely
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to run</param>
        public static void SafeAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MelonAutoUpdater.logger.Error(ex);
            }
        }

        /// <summary>
        /// Run an <see cref="Action"/> safely, in case of exception unload <see cref="MAUExtension"/>
        /// </summary>
        /// <param name="extension">The <see cref="MAUExtension"/> that should be unloaded if an exception is thrown</param>
        /// <param name="action">The <see cref="Action"/> to run</param>
        public static void SafeAction(this MAUExtension extension, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MelonAutoUpdater.logger.Error(ex);
                extension.InternalUnload(ex);
            }
        }

        /// <summary>
        /// Run a <see cref="Func{TResult}"/> safely
        /// </summary>
        /// <typeparam name="T">Type that will be used with <see cref="Func{TResult}"/> and will be returned</typeparam>
        /// <param name="function">The <see cref="Func{TResult}"/> to run safely</param>
        /// <returns>Value of provided type that was returned by <see cref="Func{TResult}"/></returns>
        public static T SafeFunction<T>(Func<T> function)
        {
            try
            {
                return function.Invoke();
            }
            catch (Exception ex)
            {
                MelonAutoUpdater.logger.Error(ex);
                return default;
            }
        }

        /// <summary>
        /// Run a <see cref="Func{TResult}"/> safely, in case of exception unload <see cref="MAUExtension"/>
        /// </summary>
        /// <typeparam name="T">Type that will be used with <see cref="Func{TResult}"/> and will be returned</typeparam>
        /// <param name="extension">The <see cref="MAUExtension"/> that should be unloaded if an exception is thrown</param>
        /// <param name="function">The <see cref="Func{TResult}"/> to run safely</param>
        /// <returns>Value of provided type that was returned by <see cref="Func{TResult}"/></returns>
        public static T SafeFunction<T>(this MAUExtension extension, Func<T> function)
        {
            try
            {
                return function.Invoke();
            }
            catch (Exception ex)
            {
                MelonAutoUpdater.logger.Error(ex);
                extension.InternalUnload(ex);
                return default;
            }
        }
    }
}