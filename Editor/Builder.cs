using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[ assembly: InternalsVisibleTo( "UnityCore.BuilderCLI.TestsEditor" ) ]

namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Class that will discover and run build steps
    /// </summary>
	public class Builder : IStepLocator
	{
        public const string ProjectSettings = @"ProjectSettings/ProjectSettings.asset";
		public const string ProjectSettingsBkup = @"ProjectSettings/ProjectSettings.asset.bak";
     
        private static readonly Type _iBuildStepType = typeof( IBuildStep );

        private List<IBuildStep> _steps;

        /// <summary>
        /// Собрать проект
        /// </summary>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void BuildGame( IEnumerable< string > additionalOptions )
        {
            BuildGame( Array.Empty< Type >(), additionalOptions );
        }

        public void BuildGame( IEnumerable< Type > typeToInclude )
        {
            BuildGame( typeToInclude, Array.Empty< string >() );
        }

        public void BuildGame( IEnumerable<Type> typeToInclude, IEnumerable< string > additionalOptions )
        {
            foreach( var type in typeToInclude )
            {
                if( !_iBuildStepType.IsAssignableFrom( type ) )
                {
                    throw new NotSupportedException( $"\"{type.Name}\" must implement \"{_iBuildStepType}\" interface" );
                }
            }
            
            File.Delete( ProjectSettingsBkup );
            File.Copy( ProjectSettings, ProjectSettingsBkup );
            Debug.Log( "Backup ProjectSettings" );

            try
            {
                BuildGameImpl( typeToInclude, additionalOptions );
                Debug.Log( "Build completed" );
            }
            finally
            {
                if( File.Exists( ProjectSettingsBkup ) )
                {
                    Debug.Log( "Restore ProjectSettings" );
                    File.Delete( ProjectSettings );
                    File.Move( ProjectSettingsBkup, ProjectSettings );
                    AssetDatabase.ImportAsset( ProjectSettings );
                }
            } 
        }
        
        public T Get< T >()
            where T : class, IBuildStep
        {
            return _steps.First( s => s is T ) as T;
        }

        /// <summary>
        /// Discover, sort and construct build steps list
        /// </summary>
        /// <returns>Ordered build steps</returns>
        internal static List<IBuildStep> BuildStepsList()
        {
            // find all types implementing IBuildStep, that can be instantiated
            // we intentionaly ignore nested types, because tests have tons of them
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany( s => s.GetTypes() )
                .Where( p => !p.IsAbstract && !p.IsNested && _iBuildStepType.IsAssignableFrom( p ) )
                .ToList();

            // filter out types that have inheritors in list
            // this way, if somebody inherits from our builtin step, base class won't be here
            var filteredTypes = types.Where( t => !types.Any( x => x.BaseType == t ) );

            return BuildStepsList( filteredTypes );
        }

        /// <summary>
        /// Sort and construct build steps list.
        /// Only types implementing <see cref="IBuildStep"/> will be used. Other types will be ignored.
        /// Types must no be nested or they will be ignored
        /// </summary>
        /// <param name="types">Types that will be used for build steps</param>
        /// <returns>Ordered build steps</returns>
        internal static List<IBuildStep> BuildStepsList( IEnumerable<Type> types )
        {
            // this function is internal so may be called without checks
            // check again that we can intantiate the classes
            var instances = types
                .Where( p => !p.IsAbstract && !p.IsNested && _iBuildStepType.IsAssignableFrom( p ) )
                .Select( x => ( IBuildStep )Activator.CreateInstance( x ) ).ToArray();

            return SortSteps( instances );
        }

        /// <summary>
        /// Sorts given build steps
        /// </summary>
        /// <param name="steps">Steps to sort</param>
        /// <returns>Ordered build steps</returns>
        internal static List<IBuildStep> SortSteps( IReadOnlyCollection<IBuildStep> steps )
        {
            var rules = new List<(Type before, Type after)>();
            bool hasBuildPipeline = steps.Any( s => s.GetType() == typeof( BuildPipelineStep ) );

            foreach( var inst in steps )
            {
                foreach( var type in GetTypeHierarchy( inst ) )
                {
                    var ba = type.GetCustomAttributes( typeof( RunBeforeAttribute ), false );
                    foreach( RunBeforeAttribute beforeAttribute in ba )
                    {
                        foreach( var beforeType in beforeAttribute.Types )
                        {
                            rules.Add( (inst.GetType(), beforeType) );
                        }
                    }

                    var aa = type.GetCustomAttributes( typeof( RunAfterAttribute ), false );
                    foreach( RunAfterAttribute beforeAttribute in aa )
                    {
                        foreach( var afterType in beforeAttribute.Types )
                        {
                            rules.Add( (afterType, inst.GetType()) );
                        }
                    }
                }

                // force BuildPipelineStep to be the last one
                if( hasBuildPipeline && inst.GetType() != typeof( BuildPipelineStep ) )
                    rules.Add( (inst.GetType(), typeof( BuildPipelineStep )) );
            }

            return TopologicalSort( steps, rules );
        }
        
        private void BuildGameImpl( IEnumerable<Type> typesToInclude, IEnumerable<string> additionalOptions )
        {
            _steps = GetFinalSteps( typesToInclude, additionalOptions );
            
            var registry = new OptionsRegistry();
            registry.Collect( _steps );
            
            var cmdOptions = registry.ProcessOptions( additionalOptions );
            var envOptions = registry.ProcessEnvironment();

            var optionsStr = string.Join( "\n", cmdOptions.Concat( envOptions ).Select( t => $"    '{t.Item1}' = '{t.Item2}'" ) );
            Debug.Log( $"Running build with options:\n{optionsStr}" );

            DumpBuildParameters();

            foreach( var a in _steps.OfType<IRunPreBuild>() )
                a.OnPreBuild( this );

            foreach( var a in _steps.OfType<IRunPostBuild>().Reverse() )
                a.OnPostBuild( this );
        }

        private List< IBuildStep > GetFinalSteps( IEnumerable<Type> typesToInclude, IEnumerable< string > additionalOptions )
        {
            const string includeStepsName = "include_steps";
            const string excludeStepsName = "exclude_steps";
            
            string includeStepsValue = string.Empty;
            string excludeStepsValue = string.Empty;

            var optionsRegistry = new OptionsRegistry();
            
            optionsRegistry.Register< string >( includeStepsName, value => includeStepsValue = value );
            optionsRegistry.Register< string >( excludeStepsName, value => excludeStepsValue = value );

            optionsRegistry.ProcessOptions( additionalOptions );
            
            if( !string.IsNullOrEmpty( includeStepsValue ) && !string.IsNullOrEmpty( excludeStepsValue ) )
            {
                throw new NotSupportedException($"Using {includeStepsName} and {excludeStepsName} commands at the same time is not supported.");
            }

            var steps = typesToInclude.Any() ? BuildStepsList( typesToInclude ) : BuildStepsList();

            if( !string.IsNullOrEmpty( includeStepsValue ) )
            {
                var stepsToInclude = GetSteps( includeStepsValue ).ToArray();
                CheckStepsForCorrectData( stepsToInclude, steps );
                return steps.Where( step => stepsToInclude.Contains( step.GetType().Name ) ).ToList();
            }

            if( !string.IsNullOrEmpty( excludeStepsValue ) )
            {
                var stepsToExclude = GetSteps( excludeStepsValue ).ToArray();
                CheckStepsForCorrectData( stepsToExclude, steps );
                return steps.Where( step => !stepsToExclude.Contains( step.GetType().Name ) ).ToList();
            }

            return steps;
        }

        private void CheckStepsForCorrectData( IEnumerable< string > generatedSteps, IEnumerable< IBuildStep > allAvailableSteps )
        {
            foreach( string generatedStep in generatedSteps )
            {
                if( string.IsNullOrEmpty( generatedStep ) )
                {
                    throw new ArgumentNullException( nameof(generatedStep), "Step value is null" );
                }
                
                if( !allAvailableSteps.Any( step => step.GetType().Name == generatedStep ))
                {
                    throw new NotSupportedException($"It is impossible to use {generatedStep} step, because there is no any available type for it");
                }
            }
        }
        
        private IEnumerable< string > GetSteps( string stepsSource )
        {
            var steps = stepsSource.Trim().Split( new string[] { " " }, StringSplitOptions.RemoveEmptyEntries );

            if( steps == null || steps.Length == 0 )
            {
                yield return stepsSource.Trim();
                yield break;
            }
            
            foreach( string stepType in steps )
            {
                yield return stepType.Trim();
            }
        }
        
        private static IEnumerable<Type> GetTypeHierarchy( IBuildStep step )
        {
            var type = step.GetType();
            while(type != typeof(object))
            {
                yield return type;
                type = type.BaseType;
            }
        }

        private static List<T> TopologicalSort<T>( IReadOnlyCollection<T> nodes, List<(Type, Type)> edges )
        {
            // Empty list that will contain the sorted elements
            var ret = new List<T>();

            // Set of all nodes with no incoming edges
            var nodesToCheck = new Queue< T >( nodes.Where( n => edges.All( e => !e.Item2.IsAssignableFrom( n.GetType() ) ) ) );

            // while nodesToCheck is non-empty do
            while( nodesToCheck.Any() )
            {
                //  remove a node from nodesToCheck
                var node = nodesToCheck.Dequeue();

                // add node to tail of ret
                ret.Add( node );

                // for each node m with an edge e from n to m do
                foreach( var e in edges.Where( e => e.Item1.IsAssignableFrom( node.GetType() ) ).ToList() )
                {
                    var m = e.Item2;

                    // remove edge e from the graph
                    edges.Remove( e );

                    // if m has no other incoming edges then
                    if( edges.All( me => !me.Item2.IsAssignableFrom( m ) ) )
                    {
                        // insert m into S
                        var nodeToInsert = nodes.FirstOrDefault(n => n.GetType() == m);

                        if( nodeToInsert != null && !nodesToCheck.Contains( nodeToInsert ) )
                        {
                            nodesToCheck.Enqueue( nodeToInsert );
                        }
                    }
                }
            }

            return ret;
        }

        private void DumpBuildParameters()
        {
            var sb = new StringBuilder( "Build Steps parameters:\n" );

            foreach( var s in _steps )
                DumpObject( sb, s );

            Debug.Log( sb.ToString() );
        }

        private void DumpObject(StringBuilder sb, object obj)
        {
            var type = obj.GetType();
            foreach( var fld in type.GetFields() )
            {
                sb.Append( fld.Name ).Append( " = " );
                if( typeof( IEnumerable ).IsAssignableFrom( fld.FieldType ) && fld.FieldType != typeof( string ) )
                {
                    AppendList( sb, ( IEnumerable )fld.GetValue( obj ) );
                }
                else
                {
                    AppendValue( sb, fld.GetValue( obj ), fld.Name );
                }
                sb.Append( "\n" );
            }
            foreach( var prop in type.GetProperties() )
            {
                sb.Append( prop.Name ).Append( " = " );
                if( typeof( IEnumerable ).IsAssignableFrom( prop.PropertyType ) && prop.PropertyType != typeof( string ) )
                {
                    AppendList( sb, ( IEnumerable )prop.GetValue( obj, null ) );
                }
                else
                {
                    AppendValue( sb, prop.GetValue( obj, null ), prop.Name );
                }
                sb.Append( "\n" );
            }
        }

        private void AppendList( StringBuilder sb, IEnumerable list )
        {
            if( list != null )
            {
                foreach( var e in list )
                {
                    sb.Append( e ).Append( ", " );
                }
            }
        }

        private void AppendValue( StringBuilder sb, object obj, string membName )
        {
            var value = (obj ?? "").ToString();
            if( membName.ToLowerInvariant().Contains( "pass" ) && !string.IsNullOrEmpty( value ) )
            {
                value = new string( '*', value.Length );
            }
            sb.Append( value );
        }
    }
}