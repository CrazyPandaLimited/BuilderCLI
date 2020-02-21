using CrazyPanda.UnityCore.DefinesEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Handles setting and unsetting defines
    /// </summary>
    public class DefinesStep : IRunPreBuild
    {
        protected List<string> _addDefines = new List<string>();
        protected List<string> _removeDefines = new List<string>();

        /// <summary>
        /// Установить дефайн
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        [Option( "define" )]
        public void AddDefine( string define )
        {
            if( string.IsNullOrEmpty( define ) )
                throw new ArgumentNullException( $"{nameof( define )} can not be null or empty!" );

            define = define.ToUpper();
            if( !_addDefines.Contains( define ) )
            {
                _addDefines.Add( define );
            }
        }

        /// <summary>
        /// Удалить дефайн
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        [Option( "undefine" )]
        public void RemoveDefine( string define )
        {
            if( string.IsNullOrEmpty( define ) )
                throw new ArgumentNullException( $"{nameof( define )} can not be null or empty!" );

            define = define.ToUpper();
            if( !_removeDefines.Contains( define ) )
            {
                _removeDefines.Add( define );
            }
        }

        public virtual void OnPreBuild( IStepLocator locator )
        {
            var pipelineStep = locator.Get<BuildPipelineStep>();
            var buildTarget = pipelineStep.BuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup( buildTarget );

            var defines = CompilerDefinesUtils.ReadDefines()
                .GetAllDefinesByPlatform( buildTargetGroup )
                .Union( _addDefines )
                .Except( _removeDefines )
                .ToList();

            CompilerDefinesUtils.Write( defines, buildTargetGroup );
        }
    }
}
