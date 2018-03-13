using NUnit.Framework;
using System.Collections;

namespace Rayark.Mast
{
    public class DeferTest
    {
        [Test]
        public void TestEmptyDefer()
        {
            int x = 3;

            using (var defer = new Defer())
            {
                x += 2;
            }
            Assert.AreEqual(5, x);
        }

        [Test]
        public void TestSingleDefer()
        {
            int x = 3;

            using (var defer = new Defer())
            {
                defer.Add(() => x = x * 2);
                x += 2;
            }
            Assert.AreEqual(10, x);
        }

        [Test]
        public void TestDeferOrder()
        {
            int x = 3;

            using (var defer = new Defer())
            {
                defer.Add(() => x = x * 2);
                defer.Add(() => x += 1);

                x += 2;
            }
            Assert.AreEqual(12, x);
        }


        class Data
        {
            public int X;
        }

        [Test]
        public void TestDeferWithCoroutine()
        {

            Data data = new Data();

            Coroutine co = new Coroutine(_DeferTestCorotuine(data));

            while (!co.Finished)
                co.Resume(0.01f);

            Assert.AreEqual(12, data.X);
        }


        IEnumerator _DeferTestCorotuine(Data data)
        {

            data.X = 3;

            using( var defer = new Defer())
            {
                defer.Add(() => data.X = data.X * 2);
                yield return null;
                defer.Add(() => data.X += 1);
                yield return null;
                data.X += 2;
                yield return null;

                if( data.X > 4)
                    yield break;
                defer.Add(() => data.X += 7);
            }
        }
    }
}

