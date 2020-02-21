using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Handles Web GL specific options
    /// </summary>
    public class WebGLOptionsStep : IRunPreBuild
    {
        /// <summary>
        /// уровень поддержки исключений в Web GL
        /// </summary>
        [Option( "webGlExceptionSupport" )]
        public WebGLExceptionSupport WebGLExceptionSupport { get; private set; } = PlayerSettings.WebGL.exceptionSupport;

        public virtual void OnPreBuild( IStepLocator locator )
        {
            if( locator.Get<BuildPipelineStep>().BuildTarget != BuildTarget.WebGL )
                return;

            // Выставляем уровень логгирования для WebGL
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport;
        }
    }
}
