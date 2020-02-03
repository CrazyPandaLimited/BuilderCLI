using NUnit.Framework;
using System;
using System.Linq;
using UnityEditor;

namespace CrazyPanda.UnityCore.BuildUtils
{
    class BuildOrderTests
    {
        // classes order is maintained alphabetically:
        // A -> B -> C -> D ....

        [RunBefore( typeof( B1 ) )]
        class A1 : IBuildStep { }
        class B1 : IBuildStep { }

        [Test]
        public void TestOrderRunBefore()
        {
            var a = new A1();
            var b = new B1();
            var s = new IBuildStep[] { b, a };

            var sorted = Builder.SortSteps(s);

            Assert.That( sorted, Is.EqualTo( new IBuildStep[] { a, b } ) );
        }

        class A2 : IBuildStep { }
        [RunAfter( typeof( A2 ) )]
        class B2 : IBuildStep { }

        [Test]
        public void TestOrderRunAfter()
        {
            var a = new A2();
            var b = new B2();
            var s = new IBuildStep[] { b, a };

            var sorted = Builder.SortSteps( s );

            Assert.That( sorted, Is.EqualTo( new IBuildStep[] { a, b } ) );
        }

        [RunBefore( typeof( B3 ), typeof( C3 ) )]
        class A3 : IBuildStep { }
        [RunBefore( typeof( C3 ) )]
        class B3 : IBuildStep { }
        class C3 : IBuildStep { }

        [Test]
        public void TestOrderMultipleRunBefore()
        {
            var a = new A3();
            var b = new B3();
            var c = new C3();
            var s = new IBuildStep[] { b, a, c };

            var sorted = Builder.SortSteps( s );

            Assert.That( sorted, Is.EqualTo( new IBuildStep[] { a, b, c } ) );
        }

        class A4 : IBuildStep { }
        [RunAfter( typeof( A4 ) )]
        class B4 : IBuildStep { }
        [RunAfter( typeof( A4 ), typeof( B4 ) )]
        class C4 : IBuildStep { }

        [Test]
        public void TestOrderMultipleRunAfter()
        {
            var a = new A4();
            var b = new B4();
            var c = new C4();
            var s = new IBuildStep[] { b, a, c };

            var sorted = Builder.SortSteps( s );

            Assert.That( sorted, Is.EqualTo( new IBuildStep[] { a, b, c } ) );
        }

        [RunBefore( typeof( B5 ) )]
        class A5 : IBuildStep { }
        [RunBefore( typeof( C5 ) )]
        class B5 : IBuildStep { }
        class C5 : IBuildStep { }

        [Test]
        public void TestOrderTransitiveRunBefore()
        {
            var a = new A5();
            var b = new B5();
            var c = new C5();
            var s = new IBuildStep[] { b, a, c };

            var sorted = Builder.SortSteps( s );

            Assert.That( sorted, Is.EqualTo( new IBuildStep[] { a, b, c } ) );
        }

        class A6 : IBuildStep { }
        [RunAfter( typeof( A6 ) )]
        class B6 : IBuildStep { }
        [RunAfter( typeof( B6 ) )]
        class C6 : IBuildStep { }

        [Test]
        public void TestOrderTransitiveRunAfter()
        {
            var a = new A6();
            var b = new B6();
            var c = new C6();
            var s = new IBuildStep[] { b, a, c };

            var sorted = Builder.SortSteps( s );

            Assert.That( sorted, Is.EqualTo( new IBuildStep[] { a, b, c } ) );
        }

        [RunAfter( typeof( C7 ) )]
        class A7 : IBuildStep { }
        [RunAfter( typeof( A7 ) )]
        class B7 : IBuildStep { }
        [RunAfter( typeof( B7 ) )]
        class C7 : IBuildStep { }

        [Test]
        public void TestOrderCycle()
        {
            var a = new A7();
            var b = new B7();
            var c = new C7();
            var s = new IBuildStep[] { b, a, c };

            Assert.That( () => Builder.SortSteps( s ), Throws.ArgumentException );
        }

        class A8 : IBuildStep { }
        [RunAfter( typeof( A8 ) )]
        [RunBefore( typeof( C8 ) )]
        class B8 : IBuildStep { }
        class B8D : B8 { }
        class C8 : IBuildStep { }

        [Test]
        public void TestOrderDerivedClass()
        {
            var a = new A8();
            var b = new B8D();
            var c = new C8();
            var s = new IBuildStep[] { b, a, c };

            var sorted = Builder.SortSteps( s );

            Assert.That( sorted, Is.EqualTo( new IBuildStep[] { a, b, c } ) );
        }

        [RunBefore(typeof(B9))]
        class A9 : IBuildStep { }
        [RunBefore(typeof(C9))]
        class A9D : A9 { }
        class B9 : IBuildStep { }
        class C9 : IBuildStep { }

        [Test]
        public void TestOrderDerivedClassMultipleAttr()
        {
            var a = new A9D();
            var b = new B9();
            var c = new C9();
            var s = new IBuildStep[] { b, a, c };

            var sorted = Builder.SortSteps( s );

            var aidx = sorted.IndexOf( a );
            var bidx = sorted.IndexOf( b );
            var cidx = sorted.IndexOf( c );

            Assert.That( aidx, Is.LessThan( bidx ) );
            Assert.That( aidx, Is.LessThan( cidx ) );
        }
    }
}