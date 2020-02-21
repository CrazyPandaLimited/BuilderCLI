using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Class that will discover and run build steps
    /// </summary>
	public class Builder : IStepLocator
	{
		public const string ProjectSettings = @"ProjectSettings/ProjectSettings.asset";
		public const string ProjectSettingsBkup = @"ProjectSettings/ProjectSettings.asset.bak";

        private List<IBuildStep> _steps;

        /// <summary>
        /// Собрать проект
        /// </summary>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void BuildGame(IEnumerable<string> additionalOptions)
        {
            File.Delete( ProjectSettingsBkup );
            File.Copy( ProjectSettings, ProjectSettingsBkup );
            Debug.Log( "Backup ProjectSettings" );

            try
            {
                BuildGameImpl( additionalOptions );
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
                .Where( p => !p.IsAbstract && !p.IsNested && typeof( IBuildStep ).IsAssignableFrom( p ) )
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
            var type = typeof( IBuildStep );

            // this function is internal so may be called without checks
            // check again that we can intantiate the classes
            var instances = types
                .Where( p => !p.IsAbstract && !p.IsNested && type.IsAssignableFrom( p ) )
                .Select( x => ( IBuildStep )Activator.CreateInstance( x ) );

            return SortSteps( instances );
        }

        /// <summary>
        /// Sorts given build steps
        /// </summary>
        /// <param name="steps">Steps to sort</param>
        /// <returns>Ordered build steps</returns>
        internal static List<IBuildStep> SortSteps( IEnumerable<IBuildStep> steps )
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

        private void BuildGameImpl( IEnumerable<string> additionalOptions )
        {
            _steps = BuildStepsList();
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

        private static IEnumerable<Type> GetTypeHierarchy( IBuildStep step )
        {
            var type = step.GetType();
            while(type != typeof(object))
            {
                yield return type;
                type = type.BaseType;
            }
        }

        private static List<T> TopologicalSort<T>( IEnumerable<T> nodes, List<(Type, Type)> edges )
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

            // if graph has edges then
            if( edges.Any() )
            {
                // return error (graph has at least one cycle)
                throw new ArgumentException( $"Cannot build sorted list around {edges.First().Item1.Name}" );
            }
            else
            {
                // return L (a topologically sorted order)
                return ret;
            }
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
            if( membName.ToLower().Contains( "pass" ) && !string.IsNullOrEmpty( value ) )
            {
                value = new string( '*', value.Length );
            }
            sb.Append( value );
        }
    }
}