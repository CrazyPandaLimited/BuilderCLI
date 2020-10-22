using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CrazyPanda.UnityCore.BuildUtils
{
    public sealed class AndroidMinificationOptionsStep : IRunPreBuild
    {
        private static readonly string _pathToProguardFile = Path.Combine( Application.dataPath, "Plugins", "Android", "proguard-user.txt" );
        
        private readonly HashSet< string > _proguardRulesToAdd = new HashSet< string >();
        private readonly HashSet< string > _proguardRulesToRemove = new HashSet< string >();

#if UNITY_2020_1_OR_NEWER
        [ Option( "androidMinification" ) ]
        public bool EnableAndroidMinification { get; private set; } = PlayerSettings.Android.minifyRelease;
#else
        [ Option( "androidMinification" ) ]
        public AndroidMinification AndroidMinification { get; private set; } = EditorUserBuildSettings.androidReleaseMinification;
#endif

        [ Option( "addProguardRule" ) ]
        public void AddProguardRule( string minificationRule )
        {
            AddProguardRule( _proguardRulesToAdd, minificationRule );
        }

        [ Option( "removeProguardRule" ) ]
        public void RemoveProguardMinificationRule( string minificationRule )
        {
            AddProguardRule( _proguardRulesToRemove, minificationRule );
        }

        public void OnPreBuild( IStepLocator locator )
        {
#if UNITY_2020_1_OR_NEWER
            PlayerSettings.Android.minifyDebug = EnableAndroidMinification;
            PlayerSettings.Android.minifyRelease = EnableAndroidMinification;
#else
            EditorUserBuildSettings.androidDebugMinification = AndroidMinification;
            EditorUserBuildSettings.androidReleaseMinification = AndroidMinification;
#endif

            WriteProguardData();
        }

        private void WriteProguardData()
        {
#if UNITY_2020_1_OR_NEWER            
            if( !EnableAndroidMinification  )
#else
            if( AndroidMinification != AndroidMinification.Proguard )
#endif            
            {
                return;
            }

            var doesProguardFileExists = File.Exists( _pathToProguardFile );

            if( !doesProguardFileExists )
            {
                Directory.CreateDirectory( Path.GetDirectoryName( _pathToProguardFile ) );
            }

            var existingProguardFileContent = doesProguardFileExists ? File.ReadAllLines( _pathToProguardFile ).Select( line => line.Trim() ) : Enumerable.Empty< string >();

            using( var writer = doesProguardFileExists ? new StreamWriter( _pathToProguardFile, false ) : File.CreateText( _pathToProguardFile ) )
            {
                _proguardRulesToAdd.UnionWith( existingProguardFileContent );

                foreach( string minificationRule in _proguardRulesToAdd )
                {
                    var needToWriteLine = _proguardRulesToRemove.All( ruleToDelete => ruleToDelete != minificationRule );

                    if( needToWriteLine )
                    {
                        writer.WriteLine( minificationRule );
                    }
                }
            }
        }

        private void AddProguardRule( ICollection< string > collectionToUse, string minificationRule )
        {
            if( string.IsNullOrEmpty( minificationRule ) )
                throw new ArgumentNullException( $"{nameof( minificationRule )} can not be null or empty!" );

            collectionToUse.Add( minificationRule.Trim() );
        }
    }
}
