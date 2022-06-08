using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Handles Android specific options
    /// </summary>
    [ RunBefore( typeof( GeneralOptionsStep ) ) ]
#if UNITY_ANDROID
    public class AndroidOptionsStep : IRunPreBuild
#else
    public class AndroidOptionsStep : IBuildStep
#endif    
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
        [ Option( "gradlePath" ) ]
        public string GradlePath { get; private set; } = AndroidExternalToolsSettings.gradlePath;
        
        /// <summary>
        /// overrides path to jdk
        /// </summary>
        [ Option( "JdkRootPath" ) ]
        public string JdkRootPath { get; private set; } = AndroidExternalToolsSettings.jdkRootPath;

        /// <summary>
        /// overrides path to android ndk
        /// </summary>
        [ Option( "NdkRootPath" ) ]
        public string NdkRootPath { get; private set; } = AndroidExternalToolsSettings.ndkRootPath;

        /// <summary>
        /// overrides path to android sdk
        /// </summary>
        [ Option( "SdkRootPath" ) ]
        public string SdkRootPath { get; private set; } = AndroidExternalToolsSettings.sdkRootPath;

        /// <summary>
        /// overrides max jvm heap size
        /// </summary>
        [ Option( "MaxJvmHeapSize" ) ]
        public int MaxJvmHeapSize { get; private set; } = AndroidExternalToolsSettings.maxJvmHeapSize;

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

        //override Etc2 fallback
        [Option("androidEtc2Fallback")]
        public AndroidETC2Fallback AndroidETC2Fallback { get; private set; } = EditorUserBuildSettings.androidETC2Fallback;
        
        //экспортировать сборку как Android проект
        [ Option( "exportAsGoogleAndroidProject" ) ]
        public bool ExportAsGoogleAndroidProject { get; private set; } = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        
        //Create symbols.zip file
        [ Option( "androidCreateSymbolsZip" ) ]
        public bool AndroidCreateSymbolsZip { get; private set; } = EditorUserBuildSettings.androidCreateSymbolsZip;
        
        //Split Android binary
        [ Option( "useAPKExpansionFiles" ) ]
        public bool UseAPKExpansionFiles { get; private set; } = PlayerSettings.Android.useAPKExpansionFiles;

        //Override Android Min Sdk Version
        [ Option( "AndroidMinSdkVersion" ) ]
        public AndroidSdkVersions AndroidMinSdkVersion { get; private set; } = PlayerSettings.Android.minSdkVersion;

        //Override Android Target Sdk Version
        [ Option( "AndroidTargetSdkVersion" ) ]
        public AndroidSdkVersions AndroidTargetSdkVersion { get; private set; } = PlayerSettings.Android.targetSdkVersion;

        //Override Android Target Architectures
        [ Option( "AndroidTargetArchitecture" ) ]
        public AndroidArchitecture AndroidTargetArchitecture { get; private set; } = PlayerSettings.Android.targetArchitectures;

        //Allows to build apk per target Architecture
        [ Option( "BuildApkPerCpuArchitecture" ) ]
        public bool BuildApkPerCpuArchitecture { get; private set; } = PlayerSettings.Android.buildApkPerCpuArchitecture;

        //Override Android Apk Install Location
        [ Option( "AndroidPreferredInstallLocation" ) ]
        public AndroidPreferredInstallLocation AndroidPreferredInstallLocation { get; private set; } = PlayerSettings.Android.preferredInstallLocation;

        //Overrides Android Internet Permission
        [ Option( "AndroidForceInternetPermission" ) ]
        public bool AndroidForceInternetPermission { get; private set; } = PlayerSettings.Android.forceInternetPermission;

        //Overrides Android SDCard Write Permission
        [ Option( "AndroidForceSDCardPermission" ) ]
        public bool AndroidForceSDCardPermission { get; private set; } = PlayerSettings.Android.forceSDCardPermission;

        public virtual void OnPreBuild( IStepLocator locator )
        {
            if( locator.Get<BuildPipelineStep>().BuildTarget != BuildTarget.Android )
                return;

            // Прописываем BundleVersionCode.
            if( BundleVersionCode >= 0 )
            {
                PlayerSettings.Android.bundleVersionCode = BundleVersionCode;
            }
            
            EditorUserBuildSettings.androidETC2Fallback = AndroidETC2Fallback;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = ExportAsGoogleAndroidProject;
            EditorUserBuildSettings.androidCreateSymbolsZip = AndroidCreateSymbolsZip;

            EditorUserBuildSettings.androidBuildSubtarget = AndroidBuildSubtarget;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem;
            EditorUserBuildSettings.buildAppBundle = AndroidBuildAppBundle;
            
            PlayerSettings.Android.useCustomKeystore = UseCustomKeyStore;
            
            PlayerSettings.Android.useAPKExpansionFiles = UseAPKExpansionFiles;
            PlayerSettings.Android.minSdkVersion = AndroidMinSdkVersion;
            PlayerSettings.Android.targetSdkVersion = AndroidTargetSdkVersion;
            PlayerSettings.Android.targetArchitectures = AndroidTargetArchitecture;
            PlayerSettings.Android.buildApkPerCpuArchitecture = BuildApkPerCpuArchitecture;
            PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation;
            PlayerSettings.Android.forceInternetPermission = AndroidForceInternetPermission;
            PlayerSettings.Android.forceSDCardPermission = AndroidForceSDCardPermission;
            
            if( !string.IsNullOrEmpty( GradlePath ) )
            {
                AndroidExternalToolsSettings.gradlePath = GradlePath;
            }

            if( !string.IsNullOrEmpty( JdkRootPath ) )
            {
                AndroidExternalToolsSettings.jdkRootPath = JdkRootPath;
            }
            
            if( !string.IsNullOrEmpty( NdkRootPath ) )
            {
                AndroidExternalToolsSettings.ndkRootPath = NdkRootPath;
            }

            if( !string.IsNullOrEmpty( SdkRootPath ) )
            {
                AndroidExternalToolsSettings.sdkRootPath = SdkRootPath;
            }

            AndroidExternalToolsSettings.maxJvmHeapSize = MaxJvmHeapSize;
            
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
