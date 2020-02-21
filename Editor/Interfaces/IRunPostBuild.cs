namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Step that will be executed after building a player
    /// </summary>
    public interface IRunPostBuild : IBuildStep
    {
        /// <summary>
        /// This method will be executed after building a player
        /// </summary>
        /// <param name="locator">Locator used to get access to other build steps</param>
        void OnPostBuild( IStepLocator locator );
    }
}
