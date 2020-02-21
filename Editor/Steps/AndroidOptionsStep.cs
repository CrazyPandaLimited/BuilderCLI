using System;
using System.IO;
using UnityEditor;

namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Handles Android specific options
    /// </summary>
    public class AndroidOptionsStep : IRunPreBuild
    {
        /// <summary>
        /// Код Android сборки
        /// </summary>
        [Option( "bundleVersionCode" )]
        public int BundleVersionCode { get; private set; } = PlayerSettings.Android.bundleVersionCode;

        /// <summary>
        /// Имя файла keystore для android сборки. если не задан, используется файл указанный в player settings
        /// </summary>
        [Option( "androidKeystore" )]
        public string AndroidKeystore { get; private set; } = PlayerSettings.Android.keystoreName;

        /// <summary>
        /// Пароль для keystore для android сборки
        /// </summary>
        [Option( "androidKeystorePassword" )]
        public string AndroidKeystorePassword { get; private set; } = PlayerSettings.Android.keystorePass;

        /// <summary>
        /// Имя ключа в keystore для android сборки. если не задан, используется ключ указанный  в player settings
        /// </summary>
        [Option( "androidKeyalias" )]
        public string AndroidKeyalias { get; private set; } = PlayerSettings.Android.keyaliasName;

        /// <summary>
        /// Пароль для ключа в keystore для android сборки
        /// </summary>
        [Option( "androidKeyaliasPassword" )]
        public string AndroidKeyaliasPassword { get; private set; } = PlayerSettings.Android.keyaliasPass;

        /// <summary>
        /// Система сборки Android: Internal, Gradle, ADT, VisualStudio (скорее всего все проекты хотят собирать именно с Gradle)
        /// </summary>
        [Option( "androidBuildSystem" )]
        public AndroidBuildSystem AndroidBuildSystem { get; private set; } = EditorUserBuildSettings.androidBuildSystem;

        public virtual void OnPreBuild( IStepLocator locator )
        {
            if( locator.Get<BuildPipelineStep>().BuildTarget != BuildTarget.Android )
                return;

            // Прописываем BundleVersionCode.
            if( BundleVersionCode >= 0 )
            {
                PlayerSettings.Android.bundleVersionCode = BundleVersionCode;
            }

            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem;

            if( !string.IsNullOrEmpty( AndroidKeyalias ) )
            {
                PlayerSettings.Android.keyaliasName = AndroidKeyalias;
                PlayerSettings.Android.useCustomKeystore = true;
            }

            if( string.IsNullOrEmpty( PlayerSettings.Android.keyaliasName ) || !PlayerSettings.Android.useCustomKeystore )
            {
                PlayerSettings.Android.useCustomKeystore = false;
                return; // сборка без подписывания
            }

            if( string.IsNullOrEmpty( AndroidKeyaliasPassword ) )
            {
                throw new Exception( "Key password not set!" );
            }

            PlayerSettings.Android.keyaliasPass = AndroidKeyaliasPassword;

            if( !string.IsNullOrEmpty( AndroidKeystore ) )
            {
                PlayerSettings.Android.keystoreName = AndroidKeystore;
            }

            if( !File.Exists( PlayerSettings.Android.keystoreName ) )
            {
                throw new Exception( "Keystore file '" + PlayerSettings.Android.keystoreName + "' is not exists!" );
            }

            if( string.IsNullOrEmpty( AndroidKeystorePassword ) )
            {
                throw new Exception( "Keystore password not set!" );
            }

            PlayerSettings.Android.keystorePass = AndroidKeystorePassword;
        }
    }
}
