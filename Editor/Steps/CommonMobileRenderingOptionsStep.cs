using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEditor.EditorUserBuildSettings;

namespace CrazyPanda.UnityCore.BuildUtils
{
    public sealed class CommonMobileRenderingOptionsStep : IRunPreBuild
    {
        /// <summary>
        /// let unity decide, which render to use
        /// </summary>
        [ Option( "UseAutographicApi" ) ]
        public bool UseAutographicApi { get; private set; } =
            PlayerSettings.GetUseDefaultGraphicsAPIs( activeBuildTarget );
        
        /// <summary>
        /// use multithread rendering
        /// </summary>
        [ Option( "MultiThreadRendering" ) ]
        public bool MultiThreadRendering { get; private set; } =
            PlayerSettings.GetMobileMTRendering( BuildPipeline.GetBuildTargetGroup( activeBuildTarget ) );
        
        /// <summary>
        /// override colorspace
        /// </summary>
        [ Option( "ColorSpace" ) ]
        public ColorSpace ColorSpace { get; private set; } = PlayerSettings.colorSpace;
        
        /// <summary>
        /// Use FrameTimingStats
        /// </summary>
        [ Option( "EnableFrameTimingStats" ) ]
        public bool EnableFrameTimingStats { get; private set; } = PlayerSettings.enableFrameTimingStats;

        /// <summary>
        /// Overrides Interface orientation
        /// </summary>
        [ Option( "InterfaceOrientation" ) ]
        public UIOrientation InterfaceOrientation { get; private set; } = PlayerSettings.defaultInterfaceOrientation;

        public void OnPreBuild( IStepLocator locator )
        {
            var activeBuildTarget = locator.Get< BuildPipelineStep >().BuildTarget;
            var activeBuildTargetGroup = BuildPipeline.GetBuildTargetGroup( activeBuildTarget );
            
            PlayerSettings.colorSpace = ColorSpace;
            PlayerSettings.enableFrameTimingStats = EnableFrameTimingStats;
            PlayerSettings.SetUseDefaultGraphicsAPIs( activeBuildTarget, UseAutographicApi);
            PlayerSettings.SetMobileMTRendering( activeBuildTargetGroup, MultiThreadRendering );
            PlayerSettings.defaultInterfaceOrientation = InterfaceOrientation;
        }
    }
}