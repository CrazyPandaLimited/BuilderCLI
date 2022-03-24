using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Handles options needed to build a player. Calls <see cref="BuildPipeline.BuildPlayer(string[], string, BuildTarget, BuildOptions)"/>
    /// </summary>
    public class BuildPipelineStep : IRunPreBuild
    {
        private string _buildFile = "";

        /// <summary>
        /// Идентификатор bundle'а.
        /// </summary>
        [Option( "bundleIdentifier" )]
        public string BundleIdentifier { get; private set; } = PlayerSettings.applicationIdentifier;

        /// <summary>
        /// Целевая платформа
        /// </summary>
        [Option( "switchBuildTarget" )]
        public BuildTarget BuildTarget { get; private set; } = EditorUserBuildSettings.activeBuildTarget;

        /// <summary>
        /// Директория сборки. абсолютный путь или относительно директории проекта.
        /// </summary>
        [Option( "buildDir" )]
        public string BuildDir { get; private set; }

        /// <summary>
        /// Включает/выключает development сборку.
        /// </summary>
        [Option( "developmentBuild" )]
        public bool Development { get; private set; } = EditorUserBuildSettings.development;

        /// <summary>
        /// Разрешить отладку.
        /// </summary>
        [Option( "allowDebuging" )]
        public bool AllowDebugging { get; private set; } = EditorUserBuildSettings.allowDebugging;

        /// <summary>
        /// Билд в режиме сервера
        /// </summary>
        [Option( "serverBuild" )]
        public bool ServerBuild { get; private set; } = EditorUserBuildSettings.enableHeadlessMode;

        /// <summary>
        /// Wait for managed debugger;
        /// </summary>
        [ Option( "waitForManagedDebugger" ) ]
        public bool WaitForManagedDebugger { get; private set; } = EditorUserBuildSettings.waitForManagedDebugger;

        /// <summary>
        /// Добавляет собранный проект к существующему в целевой директории.
        /// </summary>
        [Option( "forceAppendToExistProject" )]
        public bool ForceAppendToExistProject { get; private set; } = false;

        /// <summary>
        /// Показывает директорию с билдом в проводнике.
        /// </summary>
        [Option( "showBuiltPlayer" )]
        public bool ShowBuiltPlayer { get; private set; } = false;

        /// <summary>
        /// Использует LZ4 сжатие.
        /// </summary>
        [Option( "useLZ4Compression" )]
        public bool UseLZ4Compression { get; private set; } = false;

        /// <summary>
        /// Не собирает билд, а просто выводит значения, которые были бы использованы для сборки.
        /// </summary>
        [Option( "dryRun" )]
        public bool DryRun { get; private set; } = false;
        
        //Build with deep profiling support
        [ Option( "BuildWithDeepProfilingSupport" ) ]
        public bool BuildWithDeepProfilingSupport { get; private set; } = false;

        /// <summary>
        /// Scripts only build
        /// </summary>
        [ Option( "ScriptsOnlyBuild" ) ]
        public bool ScriptsOnlyBuild { get; private set; } = false;

        //Scripting configurations

        /// <summary>
        /// Override Backend implementation
        /// </summary>
        [ Option( "ScriptingBackend" ) ]
        public ScriptingImplementation ScriptingBackend { get; private set; } = PlayerSettings.GetScriptingBackend( GetActiveBuildTargetGroup() );

        /// <summary>
        /// Override Api Compatibility Level
        /// </summary>
        [ Option( "ApiCompatibilityLevel" ) ]
        public ApiCompatibilityLevel ApiCompatibilityLevel { get; private set; } = PlayerSettings.GetApiCompatibilityLevel( GetActiveBuildTargetGroup() );

        /// <summary>
        /// Override IL2CPP Compiler options
        /// </summary>
        [ Option( "Il2CppCompilerConfiguration" ) ]
        public Il2CppCompilerConfiguration Il2CppCompilerConfiguration { get; private set; } =
            PlayerSettings.GetIl2CppCompilerConfiguration( GetActiveBuildTargetGroup() );

        /// <summary>
        /// Use Incremental GC
        /// </summary>
        [Option("UseIncrementalGC")]
        public bool UseIncrementalGC { get; private set; } = PlayerSettings.gcIncremental;
        
        /// <summary>
        /// Allows to use c# unsafe code
        /// </summary>
        [ Option( "AllowUnsafeCode" ) ]
        public bool AllowUnsafeCode { get; private set; } = PlayerSettings.allowUnsafeCode;
        
        //Optimizitations
        
        /// <summary>
        /// Base collisions data into meshes
        /// </summary>
        [ Option( "BakeCollisionMeshes" ) ]
        public bool BakeCollisionMeshes { get; private set; } = PlayerSettings.bakeCollisionMeshes;
        
        /// <summary>
        ///Allows to strip engine code. Used only at IL2CPP 
        /// </summary>
        [ Option( "StripEngineCode" ) ]
        public bool StripEngineCode { get; private set; } = PlayerSettings.stripEngineCode;

        /// <summary>
        /// Overrides code stripping level
        /// </summary>
        [ Option( "CodeStrippingLevel" ) ]
        public ManagedStrippingLevel CodeStrippingLevel { get; private set; } = PlayerSettings.GetManagedStrippingLevel( GetActiveBuildTargetGroup() );

        /// <summary>
        /// Removes Unused mesh components from build
        /// </summary>
        [ Option( "StripUnusedMeshComponents" ) ]
        public bool StripUnusedMeshComponents { get; private set; } = PlayerSettings.stripUnusedMeshComponents;

        //Stacktrace

        /// <summary>
        /// Overrides Error StackTrace LogType
        /// </summary>
        [ Option( "ErrorStackTraceLogType" ) ]
        public StackTraceLogType ErrorStackTraceLogType { get; private set; } = PlayerSettings.GetStackTraceLogType( LogType.Error );

        /// <summary>
        /// Overrides Assert StackTrace LogType
        /// </summary>
        [ Option( "AssertStackTraceLogType" ) ]
        public StackTraceLogType AssertStackTraceLogType { get; private set; } = PlayerSettings.GetStackTraceLogType( LogType.Assert );

        /// <summary>
        /// Overrides Exception StackTrace LogType
        /// </summary>
        [ Option( "ExceptionStackTraceLogType" ) ]
        public StackTraceLogType ExceptionStackTraceLogType { get; private set; } = PlayerSettings.GetStackTraceLogType( LogType.Exception );

        /// <summary>
        /// Overrides Log StackTrace LogType
        /// </summary>
        [ Option( "LogStackTraceLogType" ) ]
        public StackTraceLogType LogStackTraceLogType { get; private set; } = PlayerSettings.GetStackTraceLogType( LogType.Log );

        /// <summary>
        /// Overrides Log StackTrace LogType
        /// </summary>
        [ Option( "WarningStackTraceLogType" ) ]
        public StackTraceLogType WarningStackTraceLogType { get; private set; } = PlayerSettings.GetStackTraceLogType( LogType.Warning );

#if UNITY_2021        
        /// <summary>
        /// Options to control code generation for IL2CPP compiler.
        /// </summary>
        [ Option( "il2CppCodeGeneration" ) ]
        public Il2CppCodeGeneration Il2CppCodeGeneration { get; private set; } = EditorUserBuildSettings.il2CppCodeGeneration;
#endif
        public List<string> Scenes { get; private set; } = EditorBuildSettings.scenes.Where( scene => scene.enabled ).Select( scene => scene.path ).ToList();

        /// <summary>
        /// Полный целевой путь сборки (директория + файл если нужен)
        /// </summary>
        public string BuildFullPath
        {
            get
            {
                return Path.Combine( BuildFullDir, BuildFile );
            }
        }

        /// <summary>
        /// Полный целевой путь к директории сборки
        /// </summary>
        public string BuildFullDir
        {
            get
            {
                var buildDir = BuildDir ?? "";
                return new Uri( Path.IsPathRooted( buildDir ) ? buildDir : Path.Combine( ProjectRoot, buildDir ) ).LocalPath;
            }
        }

        /// <summary>
        /// Имя файла сборки. Если не указан, используется последняя часть bundleIdentifier. На iOS отсутсвует.
        /// </summary>
        [Option( "buildFile" )]
        public string BuildFile
        {
            get
            {
                var ext = BuildFileExtension;
                if( string.IsNullOrEmpty( ext ) )
                {
                    return "";
                }
                var fileName = string.IsNullOrEmpty( _buildFile ) ? Path.GetExtension( BundleIdentifier ).TrimStart( '.' ) : _buildFile;
                if( !fileName.EndsWith( "." + ext ) )
                {
                    fileName = fileName.TrimEnd( '.' ) + "." + ext;
                }
                return fileName;
            }
            set
            {
                _buildFile = value;
            }
        }

        /// <summary>
        /// Расширение файла сборки соответсвующее целевой платформе
        /// </summary>
        private string BuildFileExtension
        {
            get
            {
                switch( BuildTarget )
                {
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                        return "exe";
#if UNITY_2019_2_OR_NEWER
                    case BuildTarget.StandaloneLinux64:
#else
                    case BuildTarget.StandaloneLinux:
                    case BuildTarget.StandaloneLinux64:
                    case BuildTarget.StandaloneLinuxUniversal:
#endif
                        return "x86";
                    case BuildTarget.Android:
                        if (EditorUserBuildSettings.buildAppBundle)
                        {
                            return "aab";
                        }
                        return "apk";
#if UNITY_2017_3_OR_NEWER
                    case BuildTarget.StandaloneOSX:
#else
					case BuildTarget.StandaloneOSXIntel:
					case BuildTarget.StandaloneOSXIntel64:
					case BuildTarget.StandaloneOSXUniversal:
#endif
                        return "app";
                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Расположение корневой папки проекта (той, в которой лежат Assets, ProjectSettings, etc)
        /// </summary>
        private string ProjectRoot
        {
            get
            {
                return Path.GetFullPath( Path.Combine( Application.dataPath, ".." ) );
            }
        }

        public virtual void OnPreBuild( IStepLocator locator )
        {
            // Опции билдера
            var opts = BuildOptions.None;
            if( Development )
            {
                opts |= BuildOptions.Development;
            }

            if( AllowDebugging )
            {
                opts |= BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler;
                
                if( BuildWithDeepProfilingSupport )
                {
                    opts |= BuildOptions.EnableDeepProfilingSupport;
                }

                if( ScriptsOnlyBuild )
                {
                    opts |= BuildOptions.BuildScriptsOnly;
                }
            }
            
            if( ShowBuiltPlayer )
            {
                opts |= BuildOptions.ShowBuiltPlayer;
            }

            if( ForceAppendToExistProject )
            {
                opts |= BuildOptions.AcceptExternalModificationsToPlayer;
            }

            if( UseLZ4Compression )
            {
                opts |= BuildOptions.CompressWithLz4;
            }

            if( ServerBuild )
            {
                opts |= BuildOptions.EnableHeadlessMode;
            }
            
#if UNITY_2021
            EditorUserBuildSettings.il2CppCodeGeneration = Il2CppCodeGeneration;
#endif            
            
#if UNITY_2020_2_OR_NEWER
            SerializedObject projectSettingsManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath( "ProjectSettings/ProjectSettings.asset")[0]);
            projectSettingsManager.FindProperty("overrideDefaultApplicationIdentifier").boolValue = true;
            projectSettingsManager.ApplyModifiedProperties();
#endif
            EditorUserBuildSettings.waitForManagedDebugger = WaitForManagedDebugger;
            
            //Scripting options
            var currentBuildTarget = BuildPipeline.GetBuildTargetGroup( BuildTarget );
           
            PlayerSettings.SetScriptingBackend( currentBuildTarget, ScriptingBackend  );
            PlayerSettings.SetApiCompatibilityLevel( currentBuildTarget, ApiCompatibilityLevel );
            PlayerSettings.SetIl2CppCompilerConfiguration( currentBuildTarget, Il2CppCompilerConfiguration );

            PlayerSettings.SetManagedStrippingLevel( currentBuildTarget, CodeStrippingLevel );
            PlayerSettings.stripEngineCode = StripEngineCode;
            
            // Прописываем bundleId
            PlayerSettings.SetApplicationIdentifier( currentBuildTarget, BundleIdentifier );

            PlayerSettings.gcIncremental = UseIncrementalGC;
            PlayerSettings.allowUnsafeCode = AllowUnsafeCode;
            PlayerSettings.bakeCollisionMeshes = BakeCollisionMeshes;
            PlayerSettings.stripUnusedMeshComponents = StripUnusedMeshComponents;
            
            PlayerSettings.SetStackTraceLogType( LogType.Assert, AssertStackTraceLogType );
            PlayerSettings.SetStackTraceLogType( LogType.Error, ErrorStackTraceLogType );
            PlayerSettings.SetStackTraceLogType( LogType.Exception, ExceptionStackTraceLogType );
            PlayerSettings.SetStackTraceLogType( LogType.Log, LogStackTraceLogType );
            PlayerSettings.SetStackTraceLogType( LogType.Warning, WarningStackTraceLogType );
            
            if( string.IsNullOrEmpty( BuildDir ) )
            {
                BuildDir = EditorUserBuildSettings.GetBuildLocation( BuildTarget );
            }

            // запоминаем директорию сборки
            EditorUserBuildSettings.SetBuildLocation( BuildTarget, BuildDir );

            // Собственно, сборка
            Debug.Log( "Full build path: '" + BuildFullPath + "'" );
            Directory.CreateDirectory( BuildFullDir );

            if( !DryRun )
            {
                var report = BuildPipeline.BuildPlayer( Scenes.ToArray(), BuildFullPath, BuildTarget, opts );

                if( report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded )
                {
                    throw new Exception( $"Build failed with {report.summary.totalErrors} errors and {report.summary.totalWarnings} warnings" );
                }
            }
            else
            {
                var scenes = string.Join( ", ", Scenes );
                var optsStr = opts.ToString();
                Debug.Log( $"Dry Run selected. Will build player with:\nScenes: {scenes}\nBuildOptions: {optsStr}" );
            }
        }

        /// <summary>
        /// Добавляет сцену в билд.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        [Option( "addScene", "scenePosition" )]
        public void AddScene( string path, int pos = -1 )
        {
            if( string.IsNullOrEmpty( path ) )
                throw new ArgumentNullException( $"{nameof( path )} can not be null or empty!" );

            path = NormalizePath( ResolveScenePath( path ) ).ToLowerInvariant();
            var idx = Scenes.FindIndex( x => NormalizePath( x ).ToLowerInvariant() == path );
            if( idx != -1 )
            {
                Scenes.RemoveAt( idx );
            }
            if( pos == -1 )
            {
                Scenes.Add( path );
            }
            else
            {
                Scenes.Insert( Mathf.Clamp( pos, 0, Scenes.Count ), path );
            }
        }

        /// <summary>
        /// Удаляет сцену из билда.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        [Option( "withoutScene" )]
        public void RemoveScene( string path )
        {
            if( string.IsNullOrEmpty( path ) )
                throw new ArgumentNullException( $"{nameof( path )} can not be null or empty!" );

            path = NormalizePath( ResolveScenePath( path ) ).ToLowerInvariant();
            var idx = Scenes.FindIndex( x => NormalizePath( x ).ToLowerInvariant() == path );
            if( idx != -1 )
            {
                Scenes.RemoveAt( idx );
            }
        }

        /// <summary>
        /// Удаляет из билда все сцены и добавляет указанную.
        /// </summary>
        /// <param name="path"></param>
        [Option( "setScene" )]
        public void SetScene( string path )
        {
            Scenes.Clear();
            AddScene( path );
        }

        public static string NormalizePath( string path )
        {
            if( string.IsNullOrEmpty( path ) )
            {
                return "";
            }
            path = path.Replace( "\\", "/" );
            string prevPath;
            do
            {
                prevPath = path;
                path = prevPath.Replace( "//", "/" );
            }
            while( prevPath != path );
            return path;
        }

        /// <summary>
        /// Определение полного пути к сцене 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static string ResolveScenePath( string name )
        {
            if( string.IsNullOrEmpty( name ) )
                throw new ArgumentNullException( $"{nameof( name )} can not be null or empty!" );

            if( string.IsNullOrEmpty( Path.GetExtension( name ) ) )
            {
                name += ".unity";
            }

            var path = name;
            if( !File.Exists( path ) )
            {
                path = Path.Combine( Path.GetFileName( Application.dataPath ), name );
                if( !File.Exists( path ) )
                {
                    path = Path.Combine( Path.GetFileName( Application.dataPath ), "Scenes", name );
                }
            }
            return path;
        }

        private static BuildTargetGroup GetActiveBuildTargetGroup()
        {
            return BuildPipeline.GetBuildTargetGroup( EditorUserBuildSettings.activeBuildTarget );
        }
    }
}
