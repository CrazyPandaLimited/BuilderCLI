using UnityEditor;
using UnityEngine.Rendering;

namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Handles iOS specific options
    /// </summary>
    [ RunBefore( typeof( DefinesStep ), typeof( GeneralOptionsStep ) ) ]
#if UNITY_IOS
    public class AppleOptionsStep : IRunPreBuild
#else    
    public class AppleOptionsStep : IBuildStep
#endif
    {
        /// <summary>
        /// Код команды Apple
        /// </summary>
        [Option( "appleTeamId" )]
        public string AppleTeamId { get; private set; } = PlayerSettings.iOS.appleDeveloperTeamID;

        /// <summary>
        /// Использовать автоматическое подписывание сборки.
        /// </summary>
        [Option( "appleAutoSign" )]
        public bool AppleUseAutoSigning { get; private set; } = PlayerSettings.iOS.appleEnableAutomaticSigning;

        [ Option( "appleBuildNumber" ) ]
        public string AppleBuildNumber { get; private set; } = PlayerSettings.iOS.buildNumber;

        /// <summary>
        /// Идентификатор Provision профиля.
        /// </summary>
        [Option( "appleProvisionId" )]
        public string AppleManualProvisionId { get; private set; } = PlayerSettings.iOS.iOSManualProvisioningProfileID;

        /// <summary>
        /// Тип Provision профиля (Development, Distribution).
        /// </summary>
        [Option( "appleProvisionType" )]
        public ProvisioningProfileType AppleManualProvisionType { get; private set; } = PlayerSettings.iOS.iOSManualProvisioningProfileType;

        /// <summary>
        /// Сделать билд для симулятора.
        /// </summary>
        [Option( "iosSimulator" )]
        public bool IosSimulator { get; private set; } = false;

        public virtual void OnPreBuild( IStepLocator locator )
        {
            if( locator.Get<BuildPipelineStep>().BuildTarget != BuildTarget.iOS )
                return;

            // билд для симулятора
            if( IosSimulator )
            {
                PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
                // recomended by Unity for IOS simulator
                PlayerSettings.SetUseDefaultGraphicsAPIs( BuildTarget.iOS, false );
                PlayerSettings.SetGraphicsAPIs( BuildTarget.iOS, new[] { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLES2, GraphicsDeviceType.Metal } );

                locator.Get<DefinesStep>().AddDefine( "TARGET_OS_SIMULATOR" ); // the same as Xcode sets for Simulator mode
            }

            PlayerSettings.iOS.appleDeveloperTeamID = AppleTeamId;
            PlayerSettings.iOS.appleEnableAutomaticSigning = AppleUseAutoSigning;
            PlayerSettings.iOS.iOSManualProvisioningProfileID = AppleManualProvisionId;
            PlayerSettings.iOS.iOSManualProvisioningProfileType = AppleManualProvisionType;
            PlayerSettings.iOS.buildNumber = AppleBuildNumber;
        }
    }
}
