using System;

namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Marks property or method as an option that can be set from command line.
    /// Property must have a setter.
    /// Method must have only 1 parameter.
    /// </summary>
    [ AttributeUsage( AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false ) ]
    public class OptionAttribute : Attribute
    {
        /// <summary>
        /// Name that will be used in command line without the leading -
        /// </summary>
        public string OptionName { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="optionName">Name that will be used in command line without the leading -</param>
        public OptionAttribute( string optionName )
        {
            OptionName = optionName;
        }
    }
}
