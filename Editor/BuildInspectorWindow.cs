using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Inspector window.
    /// Shows discovered build steps and command line options.
    /// Allows you to specifiy options and run the build from Editor.
    /// </summary>
    class BuildInspectorWindow : EditorWindow
    {
        [SerializeField] private List<OptionEntry> _selectedOptions = new List<OptionEntry>();
        private List< IBuildStep > _steps;
        private GUIStyle _centeredLabel;
        private GUIStyle _optionsLabel;
        private Exception _collectException;
        private Vector2 _availableOptionsScrollPosition;
        private Vector2 _selectedOptionsScrollPosition;
        private List<(string Name, Type Type)> _optionsList;
        private string _searchMask;

        [MenuItem("UnityCore/CLI.Build Inspector")]
        private static void Open()
        {
            GetWindow<BuildInspectorWindow>().Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent( "CLI.Build Inspector" );
            minSize = new Vector2( 700, 500 );

            _steps = Builder.BuildStepsList();

            try
            {
                var options = new OptionsRegistry();
                options.Collect( _steps );

                _optionsList = options.ToList();
                _optionsList.Sort();
            }
            catch( Exception e )
            {
                _collectException = e;
                Debug.LogException( e );
            }
        }

        private void OnGUI()
        {
            if( _centeredLabel == null )
            {
                _centeredLabel = new GUIStyle( EditorStyles.label ) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic };
                _optionsLabel = new GUIStyle( EditorStyles.boldLabel ) { alignment = TextAnchor.UpperLeft, margin = new RectOffset( 4, 4, 0, 4 ) };
            }

            using( new GUILayout.VerticalScope( EditorStyles.inspectorFullWidthMargins ) )
            {
                DrawBuildPlan();

                GUILayout.Space( 10 );

                // Options block
                using( new GUILayout.VerticalScope() )
                {
                    using( new GUILayout.HorizontalScope() )
                    {
                        DrawAvailableOptions();

                        GUILayout.Space( 5 );

                        DrawSelectedOptions();
                    }

                    GUILayout.Space( 5 );

                    DrawButtons();

                    GUILayout.Space( 5 );
                }
            }
        }

        private void DrawBuildPlan()
        {
            using( new GUILayout.VerticalScope() )
            {
                GUILayout.Label( "Build plan:", EditorStyles.boldLabel );

                int index = 0;
                foreach( var agent in _steps.OfType<IRunPreBuild>() )
                {
                    GUILayout.Label( $"{++index}. {agent.GetType().Name} PreBuild" );
                }

                foreach( var agent in _steps.OfType<IRunPostBuild>().Reverse() )
                {
                    GUILayout.Label( $"{++index}. {agent.GetType().Name} PostBuild" );
                }
            }
        }

        private void DrawAvailableOptions()
        {
            using( new GUILayout.VerticalScope( GUILayout.MaxWidth( position.width / 2 ) ) )
            {
                using( new GUILayout.HorizontalScope(  ) )
                {
                    GUILayout.Label( "Available options:", _optionsLabel );
                    GUILayout.FlexibleSpace();

                    _searchMask = EditorGUILayout.TextField( _searchMask, GUILayout.Width( 150 ) );
                    if( GUILayout.Button("X", EditorStyles.miniButton, GUILayout.MaxWidth( 16 ), GUILayout.MaxHeight( 16 ) ) )
                    {
                        _searchMask = null;
                    }
                }

                if( _collectException == null )
                {
                    using( var scroll = new GUILayout.ScrollViewScope( _availableOptionsScrollPosition ) )
                    {
                        _availableOptionsScrollPosition = scroll.scrollPosition;
                        using( new GUILayout.VerticalScope() )
                        {
                            var visibleOptions = string.IsNullOrEmpty( _searchMask ) ? _optionsList : _optionsList.Where( o => o.Name.ToLower().Contains( _searchMask.ToLower() ) );

                            foreach( var (name, type) in visibleOptions )
                            {
                                using( new GUILayout.HorizontalScope() )
                                {
                                    GUILayout.Label( new GUIContent( $"-{name}", $"{type.Name} {name}" ) );
                                    GUILayout.FlexibleSpace();
                                    if( GUILayout.Button( new GUIContent( "+", $"Add -{name}" ), EditorStyles.miniButton, GUILayout.MaxWidth( 16 ), GUILayout.MaxHeight( 16 ) ) )
                                    {
                                        _selectedOptions.Add( new OptionEntry() { Name = name } );
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    GUILayout.Label( "Error while parsing options:" );
                    GUILayout.Label( _collectException.ToString(), EditorStyles.wordWrappedLabel );
                }
            }
        }

        private void DrawSelectedOptions()
        {
            using( new GUILayout.VerticalScope( GUILayout.MaxWidth( position.width / 2 ) ) )
            {
                GUILayout.Label( "Selected options:", _optionsLabel );

                using( var scroll = new GUILayout.ScrollViewScope( _selectedOptionsScrollPosition ) )
                {
                    _selectedOptionsScrollPosition = scroll.scrollPosition;
                    using( new GUILayout.VerticalScope() )
                    {
                        if( _selectedOptions.Count > 0 )
                        {
                            var oldLabelWidth = EditorGUIUtility.labelWidth;

                            EditorGUIUtility.labelWidth = Mathf.Max( _selectedOptions.Max( o => EditorStyles.label.CalcSize( new GUIContent( $"-{o.Name}=" ) ).x ), 150 );
                            foreach( var option in _selectedOptions.ToList() )
                            {
                                using( new GUILayout.HorizontalScope() )
                                {
                                    var optionLabel = new GUIContent( $"-{option.Name}=" );
                                    var optionType = _optionsList.Find( p => p.Name == option.Name ).Type;

                                    if( optionType == typeof( bool ) )
                                    {
                                        bool.TryParse( option.Value, out var boolValue );
                                        option.Value = EditorGUILayout.Toggle( optionLabel, boolValue ).ToString();
                                    }
                                    else if( optionType == typeof( int ) )
                                    {
                                        int.TryParse( option.Value, out var intValue );
                                        option.Value = EditorGUILayout.IntField( optionLabel, intValue ).ToString();
                                    }
                                    else if( optionType.IsEnum )
                                    {
                                        TryParseEnum( optionType, option.Value, out var enumValue );
                                        option.Value = EditorGUILayout.EnumPopup( optionLabel, enumValue as Enum, null, true ).ToString();
                                    }
                                    else
                                    {
                                        option.Value = EditorGUILayout.TextField( optionLabel, option.Value );
                                    }

                                    if( GUILayout.Button( "–", EditorStyles.miniButton, GUILayout.MaxWidth( 16 ), GUILayout.MaxHeight( 16 ) ) )
                                    {
                                        _selectedOptions.Remove( option );
                                    }
                                }
                            }
                            EditorGUIUtility.labelWidth = oldLabelWidth;
                        }
                        else
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.Label( "Press + in the left panel to add options to this list", _centeredLabel );
                            GUILayout.FlexibleSpace();
                        }
                    }
                }
            }
        }

        private void DrawButtons()
        {
            using( new GUILayout.HorizontalScope() )
            {
                GUILayout.FlexibleSpace();
                if( GUILayout.Button( "Run build with options" ) )
                {
                    var combinedOptions = _selectedOptions.Select( o => $"-{o.Name}={o.Value}" ).Append( "-showBuiltPlayer=true" );
                    new Builder().BuildGame( combinedOptions );
                }

                if( GUILayout.Button( "Validate command line" ) )
                {
                    var ex = ValidateCommandLine();

                    if( ex == null )
                    {
                        EditorUtility.DisplayDialog( "Command line Validation", "Command line parsed successfully!", "Ok" );
                    }
                    else
                    {
                        EditorUtility.DisplayDialog( "Command line Validation", $"Command line is invalid!\n\n{ex}", "Ok" );
                    }
                }

                if( GUILayout.Button( "Copy command line" ) )
                {
                    var ex = ValidateCommandLine();

                    if( ex == null )
                    {
                        var allArgs = new[] { "-executeMethod CLI.Build" }
                            .Concat( _selectedOptions.Select( o => $"-{o.Name}={o.QuotedValue}" ) );

                        var joinedArgs = string.Join( " \\\n", allArgs );
                        GUIUtility.systemCopyBuffer = joinedArgs;

                        EditorUtility.DisplayDialog( "Command line Validation", $"Command line copied to clipboard:\n\n{GUIUtility.systemCopyBuffer}", "Ok" );
                    }
                    else
                    {
                        EditorUtility.DisplayDialog( "Command line Validation", $"Command line is invalid!\n\n{ex}", "Ok" );
                    }
                }
                GUILayout.FlexibleSpace();
            }
        }

        private Exception ValidateCommandLine()
        {
            var tmpOptions = new OptionsRegistry();
            tmpOptions.Collect( _steps );

            try
            {
                tmpOptions.ProcessOptions( _selectedOptions.Select( o => $"-{o.Name}={o.Value}" ) );
                return null;
            }
            catch( Exception e )
            {
                return e;
            }
        }

        private bool TryParseEnum( Type type, string value, out Enum result )
        {
            try
            {
                result = ( Enum )Enum.Parse( type, value );
                return true;
            }
            catch
            {
                var values = Enum.GetValues( type );
                result = ( Enum )values.GetValue( 0 );
                return false;
            }
        }

        [Serializable]
        private class OptionEntry
        {
            public string Name;
            public string Value;

            public string QuotedValue => Value?.Contains( " " ) == true ? $"\"{Value}\"" : Value ?? "";
        }
    }
}
