using System.Collections;
using NUnit.Framework;

namespace Rayark.Mast
{
    // Unit test is written with VitaTest, which is used in Cytus PSM.
    // We should survey new unit test framework for our project.

    [TestFixture]
    [Category("Sample Tests")]
    public class CoroutineTest
    {
        [Test]
        public void ResumingShouldBeCorrect()
        {
            data = -1;
            Coroutine co = new Coroutine(Flow());
            Assert.AreEqual(-1, data);
            co.Resume(0);
            Assert.AreEqual(0, data);
            co.Resume(0);
            Assert.AreEqual(3, data);
            co.Resume(0);
            Assert.AreEqual(4, data);
            Assert.IsTrue(co.Finished);
        }

        [Test]
        public void CallingFunctionShouldBeCorrect()
        {
            data = -1;
            Coroutine co = new Coroutine(Call());
            Assert.AreEqual(-1, data);
            co.Resume(0);
            Assert.AreEqual(0, data);
            co.Resume(0);
            co.Resume(0);
            Assert.AreEqual(-10, data);
            Assert.IsTrue(co.Finished);
        }

        [Test]
        public void YieldReturningResumable()
        {
            TestResumable counter = new TestResumable();
            Coroutine co = new Coroutine(counter.Join());

            for (int i = 1; i <= 5; ++i)
            {
                co.Resume(0);
                Assert.That(i == counter.Counter);
            }
        }


        private int data;
        private IEnumerator Flow()
        {
            data = 0;
            yield return null;
            data = 3;
            yield return null;
            data = 4;
        }

        private IEnumerator Call()
        {
            data = -5;
            yield return Flow();
            data = -10;
        }

        private class TestResumable : IResumable
        {
            public int Counter = 0;


            public void Resume(float delta)
            {
                if (!Finished)
                    Counter++;
            }

            public bool Finished
            {
                get { return Counter >= 5; }
            }
        }

        [Test]
        public void TestDeepCall()
        {
            data = 0;
            Coroutine c = new Coroutine(_DeepCallA());

            c.Resume(0);
            Assert.That(data == 2);
            c.Resume(0);
            Assert.That(data == 4);
            c.Resume(0);
            Assert.That(data == 6);
            c.Resume(0);
            Assert.That(data == 7);
            Assert.That(c.Finished == true);
        }


        IEnumerator _DeepCallA()
        {
            data = 1;
            yield return _DeepCallB();
            data = 7;
        }

        IEnumerator _DeepCallB()
        {
            data = 2;
            yield return null;
            data = 3;
            yield return _DeepCallC();
            data = 6;
            yield return null;
        }

        IEnumerator _DeepCallC()
        {
            data = 4;
            yield return null;
            data = 5;
        }


        [Test]
        public void TestBecome()
        {
            Coroutine test = new Coroutine(_BecomeTest0());
            data = 0;
            test.Resume(0);
            Assert.That(data == 1);

            test.Resume(0);
            Assert.That(data == 3);

            test.Resume(0);
            Assert.That(data == 6);

            test.Resume(0);
            Assert.That(data == 7);
            Assert.That(test.Finished);
        }


        IEnumerator _BecomeTest0()
        {
            data = 1;
            yield return null;
            yield return Coroutine.Become(_BecomeTest1());
            data = 2;
            yield return null;
        }

        IEnumerator _BecomeTest1()
        {
            data = 3;
            yield return null;
            data = 4;
            yield return Coroutine.Become(_BecomeTest2());
            data = 5;
            yield return null;
        }

        IEnumerator _BecomeTest2()
        {
            data = 6;
            yield return null;
            data = 7;
        }

    }

}
