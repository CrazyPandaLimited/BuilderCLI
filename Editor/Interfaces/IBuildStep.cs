namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Marker interface to denote a class used in build process.
    /// DO NOT use this interface directly. Use either <see cref="IRunPreBuild"/> or <see cref="IRunPostBuild"/>
    /// </summary>
    public interface IBuildStep
    {
    }
}
