namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Step that will be executed before building a player
    /// </summary>
    public interface IRunPreBuild : IBuildStep
    {
        /// <summary>
        /// This method will be executed before building a player
        /// </summary>
        /// <param name="locator">Locator used to get access to other build steps</param>
        void OnPreBuild( IStepLocator locator );
    }
}
