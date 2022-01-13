using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace CrazyPanda.UnityCore.BuildUtils
{
    [ Category( "ModuleTests" ), Category( "LocalTests" ) ]
    class OptionParsingTests
    {
        [Test]
        public void CollectPublicOpts()
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag2();

            r.Collect( new[] { s } );
            var names = r.AvailableOptions.Select( o => o.Name );

            Assert.That( names, Contains.Item( "public_prop" ) );
            Assert.That( names, Contains.Item( "public_mthd" ) );
        }

        [Test]
        public void CollectPrivateOpts()
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag2();

            r.Collect( new[] { s } );
            var names = r.AvailableOptions.Select( o => o.Name );

            Assert.That( names, Contains.Item( "private_prop" ) );
            Assert.That( names, Contains.Item( "private_mthd" ) );
        }

        [Test]
        public void ParseStringProp()
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag();

            r.Collect( new[] { s } );
            r.ProcessOptions( new[] { "-str=value" } );

            Assert.That( s.StringProp, Is.EqualTo( "value" ) );
        }

        [Test]
        public void ParseStringMethod()
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag();

            r.Collect( new[] { s } );
            r.ProcessOptions( new[] { "-strm=value" } );

            Assert.That( s.StringProp, Is.EqualTo( "value" ) );
        }

        [Test]
        public void ParseIntProp()
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag();

            r.Collect( new[] { s } );
            r.ProcessOptions( new[] { "-int=123" } );

            Assert.That( s.IntProp, Is.EqualTo( 123 ) );
        }

        [Test]
        public void ParseIntMethod()
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag();

            r.Collect( new[] { s } );
            r.ProcessOptions( new[] { "-intm=123" } );

            Assert.That( s.IntProp, Is.EqualTo( 123 ) );
        }

        [Test]
        public void ParseFloatProp()
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag();

            r.Collect( new[] { s } );
            r.ProcessOptions( new[] { "-flt=1.5" } );

            Assert.That( s.FloatProp, Is.EqualTo( 1.5f ) );
        }

        [Test]
        public void ParseFloatMethod()
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag();

            r.Collect( new[] { s } );
            r.ProcessOptions( new[] { "-fltm=1.5" } );

            Assert.That( s.FloatProp, Is.EqualTo( 1.5f ) );
        }

        [Test]
        public void ParseBoolProp()
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag();

            r.Collect( new[] { s } );
            r.ProcessOptions( new[] { "-bol=true" } );

            Assert.That( s.BoolProp, Is.EqualTo( true ) );
        }

        [Test]
        public void ParseBoolMethod()
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag();

            r.Collect( new[] { s } );
            r.ProcessOptions( new[] { "-bolm=true" } );

            Assert.That( s.BoolProp, Is.EqualTo( true ) );
        }

        [Test]
        public void ParseEnumProp()
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag();

            r.Collect( new[] { s } );
            r.ProcessOptions( new[] { "-enm=webgl" } );

            Assert.That( s.EnumProp, Is.EqualTo( BuildTarget.WebGL ) );
        }

        [Test]
        public void ParseEnumMethod()
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag();

            r.Collect( new[] { s } );
            r.ProcessOptions( new[] { "-enmm=webgl" } );

            Assert.That( s.EnumProp, Is.EqualTo( BuildTarget.WebGL ) );
        }

        [ TestCase( new object[] { "sss", 4444, "333s" }, ExpectedResult = new object[] { "sss", 4444, "333s" } ) ]
        [ TestCase( new object[] { "sss", 4444 }, ExpectedResult = new object[] { "sss", 4444, "test_value" } ) ]
        [ TestCase( new object[] { "sss" }, ExpectedResult = new object[] { "sss", 0, "test_value" } ) ]
        public IEnumerable<object> ParseMultipleMethodTest( object[] valuesToTest )
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag();
            r.Collect( new[] { s } );
            r.ProcessOptions( new[] { $"-mlt_values_step={string.Join( ",", valuesToTest )}" } );
            return s.MultipleValues;
        }
        
        [Test]
        public void ParseEnvVariableOption()
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag();

            r.Collect( new[] { s } );

            Environment.SetEnvironmentVariable( "str", "value" );
            r.ProcessEnvironment();

            Assert.That( s.StringProp, Is.EqualTo( "value" ) );
        }

        [Test]
        public void ParseEnvVariableOptionValueRoundBraces()
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag();

            r.Collect( new[] { s } );
            Environment.SetEnvironmentVariable( "STR_VALUE", "value" );
            r.ProcessOptions( new[] { "-str=$(STR_VALUE)" } );

            Assert.That( s.StringProp, Is.EqualTo( "value" ) );
        }

        [Test]
        public void ParseEnvVariableOptionValueCurlyBraces()
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag();

            r.Collect( new[] { s } );
            Environment.SetEnvironmentVariable( "STR_VALUE", "value" );
            r.ProcessOptions( new[] { "-str=${STR_VALUE}" } );

            Assert.That( s.StringProp, Is.EqualTo( "value" ) );
        }

        [Test]
        public void ParseEnvVariableOptionFromEnvValue()
        {
            var r = new OptionsRegistry();
            var s = new OptionsBag();

            r.Collect( new[] { s } );

            Environment.SetEnvironmentVariable( "str", "${STR_VALUE}" );
            Environment.SetEnvironmentVariable( "STR_VALUE", "value" );
            r.ProcessEnvironment();

            Assert.That( s.StringProp, Is.EqualTo( "value" ) );
        }

        class OptionsBag : IBuildStep
        {
            private object[] _multipleValues = Array.Empty< object >();
            [Option( "str" )] public string StringProp { get; set; }
            [Option( "int" )] public int IntProp { get; set; }
            [Option( "flt" )] public float FloatProp { get; set; }
            [Option( "bol" )] public bool BoolProp { get; set; }
            [Option( "enm" )] public BuildTarget EnumProp { get; set; }

            public IEnumerable< object > MultipleValues => _multipleValues;

            [Option( "strm" )] public void StringMethod( string val )
                => StringProp = val;
            [Option( "intm" )] public void IntMethod( int val )
                => IntProp = val;
            [Option( "fltm" )] public void FloatMethod( float val )
                => FloatProp = val;
            [Option( "bolm" )] public void BoolMethod( bool val )
                => BoolProp = val;
            [Option( "enmm" )] public void EnumMethod( BuildTarget val )
                => EnumProp = val;

            [ Option( "mlt_values_step" ) ] 
            public void MultipleParametersMethod( string val, int val1 = 0, string val2 = "test_value" )
            {
                _multipleValues = new object[] { val, val1, val2 };
            }
        }

        class OptionsBag2 : IBuildStep
        {
            [Option( "private_prop" )] private string PrivateProp { get; set; }
            [Option( "private_mthd" )] private void PrivateMethod( string val ) { }

            [Option( "public_prop" )] public string PublicProp { get; set; }
            [Option( "public_mthd" )] public void PublicMethod( string val ) { }
        }
    }
}
