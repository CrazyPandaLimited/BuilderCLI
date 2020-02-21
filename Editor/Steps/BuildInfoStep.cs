using System;
using System.IO;
using CrazyPanda.UnityCore.DefinesEditor;
using UnityEditor;
using UnityEngine;

namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Generates <see cref="BuildInfo"/> asset before build and removes it after
    /// </summary>
    [ RunAfter( typeof( GeneralOptionsStep ) ) ]
    public class BuildInfoStep : IRunPreBuild, IRunPostBuild
    {
        private const string BuildInfoFullPath = "Assets/Resources/" + BuildInfo.BuildInfoPath + ".asset";

        /// <summary>
        /// Собирает информацию о билде и создает файл
        /// </summary>
        public virtual void OnPreBuild( IStepLocator locator )
        {
            var buildInfo = ScriptableObject.CreateInstance<BuildInfo>();

            buildInfo.BuildTimestamp = DateTime.Now;
#if UNITY_5_6_OR_NEWER
            buildInfo.BundleIdentifier = PlayerSettings.applicationIdentifier;
#else
			buildInfo.BundleIdentifier = PlayerSettings.bundleIdentifier;
#endif
            buildInfo.BundleVersion = !string.IsNullOrEmpty( PlayerSettings.bundleVersion ) ? PlayerSettings.bundleVersion : "N/A";
            buildInfo.BuildDefines = CompilerDefinesUtils.ReadDefines().GetAllDefinesByPlatform(EditorUserBuildSettings.selectedBuildTargetGroup).ToArray();

            // Если собирается на сервере CI ряд параметров берётся из переменных окружения.
            if( EnvVar( "TEAMCITY_VERSION" ) != null ) // 
            {
                buildInfo.MilestoneCodename = EnvVar( "TEAMCITY_PROJECT_NAME" ) ?? "TEAMCITY N/A";
                buildInfo.BuildJob = EnvVar( "TEAMCITY_BUILDCONF_NAME" ) ?? "TEAMCITY N/A";
                buildInfo.SourceCodeVersion = EnvVar( "BUILD_VCS_NUMBER" ) ?? "TEAMCITY N/A";
                if( !int.TryParse( EnvVar( "BUILD_NUMBER" ) ?? "-1", out buildInfo.BuildNumber ) )
                {
                    buildInfo.BuildNumber = -1;
                }
            }
            else
            {
                buildInfo.MilestoneCodename = "N/A";
                buildInfo.BuildJob = "N/A";
                buildInfo.SourceCodeVersion = "N/A";
                buildInfo.BuildNumber = -1;
            }

            Debug.Log( "BuildInfo: \n" + buildInfo );

            var directoryName = Path.GetDirectoryName( BuildInfoFullPath );
            if( !Directory.Exists( directoryName ) )
            {
                Directory.CreateDirectory( directoryName );
            }

            AssetDatabase.CreateAsset( buildInfo, BuildInfoFullPath );
            AssetDatabase.ImportAsset( BuildInfoFullPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport );
        }

        /// <summary>
        /// Удаляет файл на локальной машине
        /// </summary>
        public virtual void OnPostBuild( IStepLocator locator )
        {
            AssetDatabase.DeleteAsset( BuildInfoFullPath );
            AssetDatabase.Refresh( ImportAssetOptions.Default );
        }

        private static string EnvVar( string name )
        {
            var v = Environment.GetEnvironmentVariable( name );
            return string.IsNullOrEmpty( v ) ? null : v;
        }
    }
}
