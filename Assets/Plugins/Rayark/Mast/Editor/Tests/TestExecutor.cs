using NUnit.Framework;
using System.Collections;
namespace Rayark.Mast
{
    public class ExecutorTest
    {

        [Test]
        public void TestExecutorSimplePasses()
        {
            _data1 = 0;
            _data2 = 0;

            Executor executor = new Executor();
            executor.Add(_Flow1());
            executor.Add(_Flow2());

            executor.Resume(0.01f);
            Assert.AreEqual(0, _data1);
            Assert.AreEqual(1, _data2);
            executor.Resume(0.01f);
            Assert.AreEqual(3, _data1);
            Assert.AreEqual(4, _data2);
            executor.Resume(0.01f);
            Assert.AreEqual(4, _data1);
            Assert.AreEqual(5, _data2);

        }

        private int _data1;
        private IEnumerator _Flow1()
        {
            _data1 = 0;
            yield return null;
            _data1 = 3;
            yield return null;
            _data1 = 4;
        }

        private int _data2;
        private IEnumerator _Flow2()
        {
            _data2 = 1;
            yield return null;
            _data2 = 4;
            yield return null;
            _data2 = 5;
        }

        [Test]
        public void TestJoin()
        {
            _data1 = 0;
            _data2 = 0;

            Executor executor = new Executor();
            executor.Add(_Flow1());
            executor.Add(_Flow2());

            Coroutine c = new Coroutine(executor.Join());

            while (!c.Finished)
                c.Resume(0.01f);

            Assert.AreEqual(4, _data1);
            Assert.AreEqual(5, _data2);
        }

        [Test]
        public void TestJoinWhile()
        {
            _data1 = 0;
            _data2 = 0;

            Executor executor = new Executor();
            executor.Add(_Flow1());
            executor.Add(_Flow2());

            int count = 0;
            Coroutine c = new Coroutine(executor.JoinWhile( ()=> count < 2 ));

            while (!c.Finished)
            {
                c.Resume(0.01f);
                count++;
            }

            Assert.AreEqual(3, _data1);
            Assert.AreEqual(4, _data2);
        }

        [Test]
        public void TestTimedJoin()
        {
            _data1 = 0;
            _data2 = 0;

            Executor executor = new Executor();
            executor.Add(_Flow1());
            executor.Add(_Flow2());

            int count = 0;
            Coroutine c = new Coroutine(executor.TimedJoin(0.1f));

            while (!c.Finished)
            {
                c.Resume(0.04f);
                count++;
            }

            Assert.AreEqual(4, _data1);
            Assert.AreEqual(5, _data2);
        }
    }
}