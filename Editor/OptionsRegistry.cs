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
    class OptionsRegistry : IEnumerable<(string, Type)>
    {
        private List<Option> _options = new List<Option>();

        /// <summary>
        /// Регистрирует опцию
        /// </summary>
        /// <param name="optionName">Имя опции</param>
        /// <param name="action">Обработчик опции</param>
        public void Register<T>( string optionName, Action<T> action )
        {
            _options.Add( new Option
            {
                Name = optionName,
                Setter = x => ConvertValue(x, action.Target, action.Method),
                Type = typeof( T )
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
                            Name = attr.OptionName,
                            Setter = x => ConvertValue( x, step, setter ),
                            Type = prop.PropertyType
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

                        _options.Add( new Option
                        {
                            Name = attr.OptionName,
                            Setter = x => ConvertValue( x, step, method ),
                            Type = parameters[ 0 ].ParameterType
                        } );
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

                    var val = TryGetValueFromEnvironment( pair[ 1 ].Trim( '"' ) );
                    ProcessOption( option, val, ret );
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

        public IEnumerator<(string, Type)> GetEnumerator() => _options.Select( o => (o.Name, o.Type) ).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void ProcessOption( Option option, string val, List<(string, string)> list )
        {
            val = TryGetValueFromEnvironment( val );

            if( val != null )
            {
                var name = option.Name;
                var maskedValue = MaskPassword( name, val );
                list.Add( (name, maskedValue) );

                try
                {
                    option.Setter?.Invoke( val );
                }
                catch( Exception e )
                {
                    throw new Exception( $"Error parsing option '-{name}={maskedValue}': {e.Message}", e );
                }
            }
        }

        private string TryGetValueFromEnvironment( string val )
        {
            // если значение опции - строка вида ${SomeName} или $(SomeName), то подставляем вместо неё значение соответствующей переменной окружения
            if( val == null )
                return null;

            var envName = val.Trim();
            if( (envName.StartsWith( "${" ) && envName.EndsWith( "}" )) ||
                (envName.StartsWith( "$(" ) && envName.EndsWith( ")" )) )
            {
                envName = envName.Remove( 0, 2 ); // TrimStart("${")
                envName = envName.Remove( envName.Length - 1 ); // TrimEnd("}");
            }

            var envVal = Environment.GetEnvironmentVariable( envName );
            if( envVal != null )
            {
                val = envVal;
            }

            return val;
        }

        private static string MaskPassword( string name, string value )
        {
            return name.ToLower().Contains( "pass" ) && !string.IsNullOrEmpty( value ) ? new string( '*', value.Length ) : value;
        }

        private void ConvertValue( string value, object target, MethodInfo setter )
        {
            var parameters = setter.GetParameters();
            var conv = TypeDescriptor.GetConverter( parameters[ 0 ].ParameterType );
            var convertedValue = conv.ConvertFrom( null, CultureInfo.InvariantCulture, value );

            var args = new object[] { convertedValue }.Concat( Enumerable.Repeat( Type.Missing, parameters.Length - 1 ) ).ToArray();
            setter.Invoke( target, args );
        }

        private class Option
        {
            public string Name;
            public Action<string> Setter;
            public Type Type;
        }
    }
}
