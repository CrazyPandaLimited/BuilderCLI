using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private const string CommandValueSplitSymbol = ",";
        
        [SerializeField] private List<OptionEntry> _selectedOptions = new List<OptionEntry>();
        private List< IBuildStep > _steps;
        private GUIStyle _centeredLabel;
        private GUIStyle _optionsLabel;
        private Exception _collectException;
        private Vector2 _availableOptionsScrollPosition;
        private Vector2 _selectedOptionsScrollPosition;
        private List<OptionsRegistry.Option> _optionsList;
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

                _optionsList = options.AvailableOptions.OrderBy( option => option.Name ).ToList();
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
                            var visibleOptions = string.IsNullOrEmpty( _searchMask ) ? _optionsList : _optionsList.Where( o => o.Name.ToLowerInvariant().Contains( _searchMask.ToLowerInvariant() ) );

                            foreach( var option in visibleOptions )
                            {
                                using( new GUILayout.HorizontalScope() )
                                {
                                    GUILayout.Label( new GUIContent( $"-{option.Name}", $"{ string.Join(", ",option.Parameters.Select( pair => pair.ToString() )) }" ) );
                                    GUILayout.FlexibleSpace();
                                    if( GUILayout.Button( new GUIContent( "+", $"Add -{name}" ), EditorStyles.miniButton, GUILayout.MaxWidth( 16 ), GUILayout.MaxHeight( 16 ) ) )
                                    {
                                        var optionEntry = new OptionEntry()
                                        {
                                            Name = option.Name
                                        };
                                        
                                        foreach( var pair in option.Parameters )
                                        {
                                            optionEntry.Parameters.Add( new EntryParameter()
                                            {
                                                Name = pair.Name,
                                                Value = pair.DefaultValue
                                            } );
                                        }
                                        
                                        _selectedOptions.Add( optionEntry );
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

                            foreach( var option in _selectedOptions.ToList() )
                            {
                                using( new GUILayout.HorizontalScope() )
                                {
                                    var optionSource = _optionsList.Find( p => p.Name == option.Name );

                                    var array = option.Parameters.ToArray();
                                    
                                    for( var i = 0; i < array.Length; ++i )
                                    {
                                        var pair = array[ i ];

                                        var optionLabel = new GUIContent( $"{(i == 0 ? "-" : string.Empty)}{pair.Name}=" );
                                        var optionType  = optionSource.Parameters.Find( parameter => parameter.Name == pair.Name ).Type;
                                        EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize( optionLabel ).x;
                                        
                                        if( optionType == typeof( bool ) )
                                        {
                                            bool.TryParse( pair.Value, out var boolValue );
                                            pair.Value = EditorGUILayout.Toggle( optionLabel, boolValue ).ToString();
                                        }
                                        else if( optionType == typeof( int ) )
                                        {
                                            int.TryParse( pair.Value, out var intValue );
                                            pair.Value = EditorGUILayout.IntField( optionLabel, intValue ).ToString();
                                        }
                                        else if( optionType.IsEnum )
                                        {
                                            TryParseEnum( optionType, pair.Value, out var enumValue );
                                            pair.Value = EditorGUILayout.EnumPopup( optionLabel, enumValue as Enum, null, true ).ToString();
                                        }
                                        else
                                        {
                                            pair.Value = EditorGUILayout.TextField( optionLabel, pair.Value );
                                        }
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
                    var combinedOptions = _selectedOptions.Select( o => o.ToString() ).Append( "-showBuiltPlayer=true" );
                    new Builder().BuildGame( combinedOptions );
                }

                if( GUILayout.Button( "Validate command line" ) )
                {
                    var ex = ValidateCommandLine();

                    DisplayDialog( ex == null ? "Command line parsed successfully!" : $"Command line is invalid!\n\n{ex}");
                }

                if( GUILayout.Button( "Copy command line" ) )
                {
                    var ex = ValidateCommandLine();

                    if( ex == null )
                    {
                        var allArgs = new[] { $"-executeMethod CLI.Build" }
                            .Concat( _selectedOptions.Select( o => $"-{o.Name}={o.QuotedValue}" ) );

                        var joinedArgs = string.Join( " \\\n", allArgs );
                        GUIUtility.systemCopyBuffer = joinedArgs;

                        DisplayDialog( $"Command line copied to clipboard:\n\n{GUIUtility.systemCopyBuffer}");
                    }
                    else
                    {
                        DisplayDialog(  $"Command line is invalid!\n\n{ex}" );
                    }
                }

                if( GUILayout.Button( "Try to paste command args from buffer" ) )
                {
                    TryToSetOptionEntriesFromBuffer();
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

        private void TryToSetOptionEntriesFromBuffer()
        {
            var foundEntries = GetCommandLineArgsFromString( GUIUtility.systemCopyBuffer ).ToArray();

            var thereIsNoAnyEntryToSet = foundEntries.Length == 0;

            if( thereIsNoAnyEntryToSet )
            {
                DisplayDialog( "There is no any command arg to paste from buffer" );
                return;
            }
            
            _selectedOptions.Clear();
            _selectedOptions.AddRange( foundEntries );

            DisplayDialog( $"Following args were pasted from buffer:\n\n{string.Join( "\n", foundEntries.Select( entry => entry.ToString() ) )}" );
        }

        private IEnumerable< OptionEntry > GetCommandLineArgsFromString( string argsContent )
        {
            (string argNameGroup, string argContainsQoutesGroup) argNameGroupsData = ("ArgNameGroup", "ArgNameGroupContainsQoutes");
            (string argNameGroup, string argContainsQoutesGroup) argValueGroupsData = ("ArgValueGroup", "ArgValueGroupContainsQoutes");
            const string baseArgPatternFormat = "((?<{0}>\"(?<{1}>.*?[^\\\\])\")|(?<{1}>[\\w\\,]*))";
            var finalPatten = $"(?>-\\s*{string.Format( baseArgPatternFormat, argNameGroupsData.argContainsQoutesGroup,argNameGroupsData.argNameGroup )}((\\s*[\\=]\\s*)|(\\s*))" + 
                              $"{string.Format( baseArgPatternFormat, argValueGroupsData.argContainsQoutesGroup,argValueGroupsData.argNameGroup )})";

            var parsedArgs = Regex.Matches( argsContent, finalPatten, RegexOptions.Multiline );

            foreach( Match parsedArg in parsedArgs )
            {
                var argName = parsedArg.Groups[ argNameGroupsData.argNameGroup ].Value;

                var availableOption = _optionsList.FirstOrDefault( option => option.Name == argName );

                if( availableOption == null )
                {
                    continue;
                }

                var argValue = parsedArg.Groups[ argValueGroupsData.argNameGroup ].Value;
                var argValues = argValue.Contains( CommandValueSplitSymbol ) ? argValue.Split( new[] { CommandValueSplitSymbol }, StringSplitOptions.None ) : new[] { argValue };
                
                var optionEntry = new OptionEntry
                {
                    Name = argName,
                    IsQuoteForced = !string.IsNullOrEmpty( parsedArg.Groups[ argValueGroupsData.argContainsQoutesGroup ].Value )
                };
                
                for( var i = 0; i < argValues.Length; ++i )
                {
                    optionEntry.Parameters.Add( new EntryParameter()
                    {
                        Name = availableOption.Parameters[i].Name,
                        Value = argValues[i]
                    } );
                }
                
                yield return optionEntry;
            }
        }

        private void DisplayDialog( string message )
        {
            const string dialogTitle = "Command line Validation";
            const string buttonCloseTitle = "Ok";

            EditorUtility.DisplayDialog( dialogTitle, message,buttonCloseTitle );
        }

        [ Serializable ]
        private class OptionEntry
        {
            public List< EntryParameter > Parameters = new List< EntryParameter >();

            public string Name = string.Empty;
            public string Value => string.Join( CommandValueSplitSymbol, Parameters.Select( pair => pair.Value ) );

            public bool IsQuoteForced;
            public string QuotedValue => Value?.Contains( " " ) == true || IsQuoteForced ? $"\"{Value}\"" : Value ?? "";

            public override string ToString() => $"-{Name}={Value}";
        }

        [ Serializable ]
        private sealed class EntryParameter
        {
            public string Name = string.Empty;
            public string Value = string.Empty;
        }
    }
}
