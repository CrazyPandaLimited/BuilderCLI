using System;

namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Tells builder that this step must be executed before given step types
    /// </summary>
    [ AttributeUsage( AttributeTargets.Class ) ]
    public class RunBeforeAttribute : Attribute
    {
        /// <summary>
        /// Step will be executed before all of these steps
        /// </summary>
        public Type[] Types { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="types">This step will be executed before all of these steps</param>
        public RunBeforeAttribute( params Type[] types )
        {
            Types = types;
        }
    }
}
