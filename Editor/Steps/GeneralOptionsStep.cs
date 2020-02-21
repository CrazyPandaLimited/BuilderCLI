using System;
using UnityEditor;

namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Handles general build options shared between platforms
    /// </summary>
    public class GeneralOptionsStep : IRunPreBuild
    {
        /// <summary>
        /// Имя продукта
        /// </summary>
        [Option( "productName" )]
        public string ProductName { get; private set; } = PlayerSettings.productName;

        /// <summary>
        /// Версия bundle'а
        /// </summary>
        [Option( "bundleVersion" )]
        public string BundleVersion { get; private set; } = PlayerSettings.bundleVersion;

        /// <summary>
        /// Показывает splash screen
        /// </summary>
        [Option( "showSplashScreen" )]
        public bool ShowSplashScreen { get; private set; } = false;

        /// <summary>
        /// Показывает логотип Unity
        /// </summary>
        [Option( "showUnityLogo" )]
        public bool ShowUnityLogo { get; private set; } = false;

        public virtual void OnPreBuild( IStepLocator locator )
        {
            var buildTarget = locator.Get<BuildPipelineStep>().BuildTarget;

            // Прописываем product name
            PlayerSettings.productName = ProductName;

            // Прописываем основную версию для всех платформ
            if( buildTarget == BuildTarget.iOS )
            {
                PlayerSettings.bundleVersion = BundleVersion.Substring( 0, Math.Min( 18, BundleVersion.Length ) ); // на iOS version ограничен 18-ю символами
            }
            else
            {
                PlayerSettings.bundleVersion = BundleVersion;
            }

            // Добавляем проверки на NullReferences и выход за границу массива при генерации IL2CPP
            PlayerSettings.SetAdditionalIl2CppArgs( "--emit-null-checks --enable-array-bounds-check" );

            // Отключаем лого
            PlayerSettings.SplashScreen.show = ShowSplashScreen;
            PlayerSettings.SplashScreen.showUnityLogo = ShowUnityLogo;
        }
    }
}
