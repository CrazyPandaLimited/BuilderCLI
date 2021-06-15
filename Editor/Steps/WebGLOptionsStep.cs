using System;
using UnityEditor;

namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Handles Web GL specific options
    /// </summary>
    public class WebGLOptionsStep : IRunPreBuild
    {
        /// <summary>
        /// Уровень поддержки исключений в Web GL
        /// </summary>
        [ Option( "webGlExceptionSupport" ) ]
        public WebGLExceptionSupport WebGLExceptionSupport { get; private set; } = PlayerSettings.WebGL.exceptionSupport;

        /// <summary>
        /// Имя шаблона Web GL сборки. Задается в формате PROJECT:{Name} (для проектных) или APPLICATION:{Name} (для встроенных)
        /// </summary>
        [ Option( "webGlTemplate" ) ]
        public string WebGLTemplate { get; private set; } = PlayerSettings.WebGL.template;

        /// <summary>
        /// Дефолтная высота для Webgl контейнера
        /// </summary>
        [ Option( "defaultWebScreenHeight" ) ]
        public int DefaultWebScreenHeight { get; private set; } = PlayerSettings.defaultWebScreenHeight;

        /// <summary>
        /// Дефолтная высота для Webgl контейнера
        /// </summary>
        [ Option( "defaultWebScreenWidth" ) ]
        public int DefaultWebScreenWidth { get; private set; } = PlayerSettings.defaultWebScreenWidth;

        public virtual void OnPreBuild( IStepLocator locator )
        {
            if( locator.Get< BuildPipelineStep >().BuildTarget != BuildTarget.WebGL )
                return;

            // Выставляем уровень логгирования для WebGL
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport;

            if( !WebGLTemplate.StartsWith( "PROJECT:" ) && !WebGLTemplate.StartsWith( "APPLICATION:" ) )
                throw new ArgumentException( "webGLTemplate must start with 'PROJECT:' or 'APPLICATION:'" );

            PlayerSettings.WebGL.template = WebGLTemplate;

            PlayerSettings.defaultWebScreenHeight = DefaultWebScreenHeight;
            PlayerSettings.defaultWebScreenWidth = DefaultWebScreenWidth;
        }
    }
}
