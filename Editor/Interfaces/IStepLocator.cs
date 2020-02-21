namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Locator used to get access to other build steps
    /// </summary>
    public interface IStepLocator
    {
        /// <summary>
        /// Returns build step of requested type
        /// </summary>
        /// <typeparam name="T">Type of build step</typeparam>
        /// <returns>Build step or <see langword="null"/> if not found</returns>
        T Get< T >() where T : class, IBuildStep;
    }
}
