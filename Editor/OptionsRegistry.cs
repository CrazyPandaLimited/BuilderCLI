using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CrazyPanda.UnityCore.BuildUtils
{
    /// <summary>
    /// Collects and parses Options
    /// </summary>
    class OptionsRegistry
    {
        private List<Option> _options = new List<Option>();

        public IEnumerable< Option > AvailableOptions => _options;

        /// <summary>
        /// Регистрирует опцию
        /// </summary>
        /// <param name="optionName">Имя опции</param>
        /// <param name="action">Обработчик опции</param>
        public void Register<T>( string optionName, Action<T> action )
        {
            _options.Add( new Option
            {
                Parameters = new List< OptionParameter >
                {
                    new OptionParameter( optionName, typeof( T ) )
                },
                Name = optionName,
                Setter = x => ConvertValue( x, action.Target, action.Method ),
            } );
        }

        /// <summary>
        /// Собирает все опции с переданных <see cref="IBuildStep"/>
        /// </summary>
        /// <param name="steps">Шаги для сбора опций</param>
        public void Collect( IEnumerable<IBuildStep> steps )
        {
            foreach( var step in steps )
            {
                var props = step.GetType().GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
                foreach( var prop in props )
                {
                    var attr = prop.GetCustomAttributes( typeof( OptionAttribute ), true ).FirstOrDefault() as OptionAttribute;
                    
                    if( attr != null )
                    {
                        var setter = prop.GetSetMethod( true );

                        if( setter == null )
                            throw new Exception( $"Property {step.GetType().Name}.{prop.Name} marked with {nameof( OptionAttribute )} must have setter" );

                        _options.Add( new Option
                        {
                            Parameters = new List< OptionParameter >()
                            {
                                new OptionParameter(attr.OptionName, prop.PropertyType)  
                            },
                            Name = attr.OptionName,
                            Setter = x => ConvertValue( x, step, setter ),
                        } );
                    }
                }

                var methods = step.GetType().GetMethods( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
                foreach( var method in methods )
                {
                    var attr = method.GetCustomAttributes( typeof( OptionAttribute ), true ).FirstOrDefault() as OptionAttribute;

                    if( attr != null )
                    {
                        var parameters = method.GetParameters();
                        if( parameters.Length == 0 )
                            throw new Exception( $"Method {step.GetType().Name}.{method.Name} marked with {nameof( OptionAttribute )} must take at least 1 parameter" );

                        if( parameters.Length > 0 && parameters.Skip( 1 ).Any( p => !p.IsOptional ) )
                            throw new Exception( $"Method {step.GetType().Name}.{method.Name} marked with {nameof( OptionAttribute )} must have optional parameters except first" );

                        var optionNames = new[] { attr.OptionName }.Concat( attr.ExtraOptionNames ).ToArray();

                        if( optionNames.Length > parameters.Length )
                            throw new Exception($"Method {step.GetType().Name}.{method.Name} marked with {nameof( OptionAttribute )} has only {parameters.Length} parameters, but you are using {optionNames.Length}");
                        
                        var option = new Option
                        {
                            Setter = x => ConvertValue( x, step, method ),
                            Name = attr.OptionName
                        };
                        
                        for( var i = 0; i < optionNames.Length; ++i )
                        {
                            option.Parameters.Add( new OptionParameter( optionNames[ i ], parameters[ i ].ParameterType, parameters[i].DefaultValue ) );
                        }
                        
                        _options.Add( option );
                    }
                }
            }
        }

        /// <summary>
        /// Парсер параметров коммандной строки
        /// </summary>
        public IReadOnlyList<(string name, string value)> ProcessOptions( IEnumerable<string> options )
        {
            var ret = new List<(string, string)>();
            foreach( var argstr in options )
            {
                var option = _options.FirstOrDefault( x => argstr.StartsWith( $"-{x.Name}=", true, CultureInfo.InvariantCulture ) );
                if( option != null )
                {
                    var pair = argstr.Split( '=' );
                    if( pair.Length != 2 )
                    {
                        Debug.LogError( $"Wrong option format : '{argstr}'" );
                        continue;
                    }

                    ProcessOption( option, pair[ 1 ].Trim( '"' ), ret );
                }
            }

            return ret;
        }

        /// <summary>
        /// Попытаться считать значения опций из переменных окружения
        /// </summary>
        public IReadOnlyList<(string, string)> ProcessEnvironment()
        {
            var ret = new List<(string, string)>();

            foreach( var option in _options )
            {
                ProcessOption( option, Environment.GetEnvironmentVariable( option.Name ), ret );
            }

            return ret;
        }

        private void ProcessOption( Option option, string val, List<(string, string)> list )
        {
            var parsedValues = TryGetValuesFromEnvironment( val );

            if( parsedValues.Count == 0 )
            {
                return;
            }
            
            var maskedValue  = MaskPassword( option.Name, string.Join( ",", parsedValues ) );
            list.Add( (option.Name, maskedValue) );
            
            try
            {
                option.Setter?.Invoke( parsedValues );
            }
            catch( Exception e )
            {
                throw new Exception( $"Error parsing option '-{option.Name}={maskedValue}': {e.Message}", e );
            }
        }

        private IReadOnlyList< string > TryGetValuesFromEnvironment( string source )
        {
            var values = new List< string >();
            
            // если значение опции - строка вида ${SomeName} или $(SomeName), то подставляем вместо неё значение соответствующей переменной окружения
            if( source == null )
            {
                return values;
            }

            foreach( string val in source.Split( ',' ) )
            {
                var envName = val.Trim();
                if( (envName.StartsWith( "${" ) && envName.EndsWith( "}" )) ||
                    (envName.StartsWith( "$(" ) && envName.EndsWith( ")" )) )
                {
                    envName = envName.Remove( 0, 2 );               // TrimStart("${")
                    envName = envName.Remove( envName.Length - 1 ); // TrimEnd("}");
                }

                values.Add( Environment.GetEnvironmentVariable( envName ) ?? val );
            }

            return values;
        }

        private static string MaskPassword( string name, string value )
        {
            return name.ToLower().Contains( "pass" ) && !string.IsNullOrEmpty( value ) ? new string( '*', value.Length ) : value;
        }

        private void ConvertValue( IReadOnlyCollection< string > values, object target, MethodInfo setter )
        {
            var parameters = setter.GetParameters();

            var convertedValues = values.Select( ( value, position ) =>
            {
                var conv = TypeDescriptor.GetConverter( parameters[ position ].ParameterType );
                return conv.ConvertFrom( null, CultureInfo.InvariantCulture, value );
            } );
            
            var args = convertedValues.Concat( Enumerable.Repeat( Type.Missing, parameters.Length - values.Count ) ).ToArray();
            setter.Invoke( target, args );
        }
        
        internal sealed class Option
        {
            public List< OptionParameter > Parameters = new List< OptionParameter >();
            public Action< IReadOnlyCollection< string > > Setter;
            public string Name = string.Empty;
        }
        
        internal readonly struct OptionParameter
        {
            public readonly string Name;
            public readonly Type Type;
            public readonly string DefaultValue;
            
            private readonly string _stringValue;

            public OptionParameter( string name, Type type, object defaultValue = null )
            {
                Name = name;
                Type = type;
                DefaultValue = defaultValue?.ToString() ?? string.Empty;
                _stringValue = $"{Type} {Name}{(!string.IsNullOrEmpty( DefaultValue ) ? $" = {DefaultValue}" : string.Empty)}";
            }

            public override string ToString() => _stringValue;
        }
    }
}
