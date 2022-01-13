using System;
using System.Collections.Generic;

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
        
        public IEnumerable< string > ExtraOptionNames { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="optionName">Name that will be used in command line without the leading -</param>
        /// <param name="extraOptionNames">Extra parameters</param>
        public OptionAttribute( string optionName, params string[] extraOptionNames )
        {
            OptionName = optionName;
            ExtraOptionNames = extraOptionNames;
        }
    }
}