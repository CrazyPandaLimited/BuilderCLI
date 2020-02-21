using System;

namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Tells builder that this step must be executed after given step types
    /// </summary>
    [ AttributeUsage( AttributeTargets.Class ) ]
    public class RunAfterAttribute : Attribute
    {
        /// <summary>
        /// Step will be executed after all of these steps
        /// </summary>
        public Type[] Types { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="types">This step will be executed after all of these steps</param>
        public RunAfterAttribute( params Type[] types )
        {
            Types = types;
        }
    }
}
