using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;

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
        /// Использовать собственный KeyStore
        /// </summary>
        [Option( "androidUseCustomKeystore" )]
        public bool UseCustomKeyStore { get; private set; } = PlayerSettings.Android.useCustomKeystore;

        /// <summary>
        /// Имя файла keystore для android сборки. если не задан, используется файл указанный в player settings
        /// </summary>
        [Option( "androidKeystore" )]
        public string AndroidKeystore { get; private set; } = PlayerSettings.Android.keystoreName;

        /// <summary>
        /// Кастомный путь до папки с грейдл дистрибутивом
        /// </summary>
        [Option("gradlePath")]
        public string GradlePath { get; private set; }

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
        
        /// <summary>
        /// Собрать Android App Bundle (aab file) вместо apk
        /// </summary>
        [Option( "buildAppBundle" )]
        public bool AndroidBuildAppBundle { get; private set; } = EditorUserBuildSettings.buildAppBundle;

        /// <summary>
        /// Оверрайдить формат текстур для Android
        /// </summary>
        [Option( "androidBuildSubtarget" )]
        public MobileTextureSubtarget AndroidBuildSubtarget { get; private set; } = EditorUserBuildSettings.androidBuildSubtarget;

        public virtual void OnPreBuild( IStepLocator locator )
        {
            if( locator.Get<BuildPipelineStep>().BuildTarget != BuildTarget.Android )
                return;

            // Прописываем BundleVersionCode.
            if( BundleVersionCode >= 0 )
            {
                PlayerSettings.Android.bundleVersionCode = BundleVersionCode;
            }

            EditorUserBuildSettings.androidBuildSubtarget = AndroidBuildSubtarget;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem;
            EditorUserBuildSettings.buildAppBundle = AndroidBuildAppBundle;
            
            PlayerSettings.Android.useCustomKeystore = UseCustomKeyStore;

            if( !string.IsNullOrEmpty( GradlePath ) )
            {
                AndroidExternalToolsSettings.gradlePath = GradlePath;
            }

            if( PlayerSettings.Android.useCustomKeystore )
            {
                PlayerSettings.Android.keystoreName = AndroidKeystore;
                if( !File.Exists( PlayerSettings.Android.keystoreName ) )
                {
                    throw new Exception( "Keystore file '" + PlayerSettings.Android.keystoreName + "' is not exists!" );
                }

                PlayerSettings.Android.keystorePass = AndroidKeystorePassword;
                if( string.IsNullOrEmpty( PlayerSettings.Android.keystorePass ) )
                {
                    throw new Exception( "Keystore password not set!" );
                }

                PlayerSettings.Android.keyaliasName = AndroidKeyalias;
                if( string.IsNullOrEmpty( PlayerSettings.Android.keyaliasName ) )
                {
                    throw new Exception( "KeyAlias not set!" );
                }

                PlayerSettings.Android.keyaliasPass = AndroidKeyaliasPassword;
                if( string.IsNullOrEmpty( PlayerSettings.Android.keyaliasPass ) )
                {
                    throw new Exception( "KeyAlias password not set!" );
                }
            }
        }
    }
}
