using System;
using System.Collections;
using NUnit.Framework;

namespace Rayark.Mast
{
    [TestFixture]
    [Category("Monad Tests")]
    public class MonadTests
    {
        class ParseStringMonad : IMonad<int>
        {
            public int Result { get; private set; }
            public Exception Error { get; private set; }
            public IEnumerator Do()
            {
                // sleep 3 frames first
                yield return null;
                yield return null;
                yield return null;

                try {
                    Result = int.Parse(_input);
                } catch(FormatException e) {
                    Error = e;
                }
            }

            public ParseStringMonad(string input)
            {
                _input = input;
            }

            string _input;
        }

        // create a coroutine and resume it until the monad is finished
        static void _Wait<T>(IMonad<T> monad)
        {
            var co = new Coroutine(monad.Do());
            while(!co.Finished)
                co.Resume(0.03f);
        }

        [Test]
        public void SimpleMonadTest()
        {
            var m = new SimpleMonad<int>(10);
            _Wait(m);

            Assert.AreEqual(m.Result, 10);

            m = new SimpleMonad<int>(new System.ArgumentException("invalid argument"));
            _Wait(m);

            Assert.IsNotNull(m.Error);
            Assert.AreEqual(m.Error.Message, "invalid argument");
        }

        [Test]
        public void BindMonadTest()
        {
            var m1 = new SimpleMonad<string>("12345"); // m1 will output 12345
            var m2 = m1.Then(s => new ParseStringMonad(s));
            _Wait(m2);

            Assert.AreEqual(m2.Result, 12345);
        }

        [Test]
        public void BindMonadSimpleFuncTest()
        {
            var m1 = new SimpleMonad<string>("67890");
            // Then() can accept a function which returns a value instead of a Monad.
            var m2 = m1.Map(int.Parse);
            _Wait(m2);

            Assert.AreEqual(m2.Result, 67890);

        }

        [Test]
        public void BindMonadSimpleFuncTest2()
        {
            bool called = false;
            System.Action<string> setSpriteToImage = (string path) => {
                called = true;
            };

            var m1 = new SimpleMonad<string>("icon.png");
            var m2 = m1.Then( setSpriteToImage );
            _Wait(m2);

            Assert.IsTrue(called);
        }

        [Test]
        public void BindMonadWithLinqSyntax()
        {
            var mc = from str in new SimpleMonad<string>("12345")
                     from integer in new ParseStringMonad(str)
                     from integer2 in new SimpleMonad<int>(integer + 1)
                     select integer2 + 1;
            _Wait(mc);

            Assert.AreEqual(mc.Result, 12347);
        }

        [Test]
        public void BindMonadFirstErrorTest()
        {
            var m1 = new SimpleMonad<string>(new System.Exception("error 1")); // m1 will failed with this error
            var m2 = m1.Then(s => new ParseStringMonad(s));
            _Wait(m2);

            Assert.IsNotNull(m2.Error);
            Assert.AreEqual(m2.Error.Message, "error 1");
        }

        [Test]
        public void BindMonadSecondErrorTest()
        {
            var m1 = new SimpleMonad<string>("abcde"); // m1 output is not number
            var m2 = m1.Then(s => new ParseStringMonad(s));
            _Wait(m2);

            Assert.IsNotNull(m2.Error);
            Assert.IsInstanceOf(typeof(FormatException), m2.Error);
        }

        [Test]
        public void CatchMonadErrorTest()
        {
            var m1 = new ParseStringMonad("abcde");
            var m2 = m1.Catch(e => new SimpleMonad<int>(999) );
            _Wait(m2);

            Assert.IsNull(m2.Error);
            Assert.AreEqual(m2.Result, 999);
        }

        [Test]
        public void CatchMonadSuccessTest()
        {
            var m1 = new ParseStringMonad("12345");
            var m2 = m1.Catch(e => new SimpleMonad<int>(999) );
            _Wait(m2);

            Assert.IsNull(m2.Error);
            Assert.AreEqual(m2.Result, 12345);
        }

        [Test]
        public void BlockMonadTest()
        {
            var m1 = new BlockMonad<int>(TestTask1);
            var m2 = new BlockMonad<string>(TestTask2);
            _Wait(m1);
            Assert.IsNull(m1.Error);
            Assert.AreEqual(m1.Result, 10);

            _Wait(m2);
            Assert.IsNull(m2.Result);
            Assert.AreEqual(m2.Error.Message, "error 2");
        }

        IEnumerator TestTask1(IReturn<int> ret)
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            ret.Accept(10);
        }

        IEnumerator TestTask2(IReturn<string> ret)
        {
            yield return null;
            yield return null;
            ret.Fail(new System.Exception("error 2"));
        }

        [Test]
        public void ConcurrentMonad2Test()
        {
            var m1 = new BlockMonad<int>(TestTask1);
            var m3 = new BlockMonad<string>(TestTask3);

            var mc = new ConcurrentMonad<int, string>(m1, m3);
            _Wait(mc);

            Assert.AreEqual(mc.Result.Item1, 10);
            Assert.AreEqual(mc.Result.Item2, "ok 3");
            Assert.IsNull(mc.Error);
        }

        IEnumerator TestTask3(IReturn<string> ret)
        {
            yield return null;
            yield return null;
            ret.Accept("ok 3");
        }

        [Test]
        public void ConcurrentMonad2ErrorTest()
        {
            var m1 = new BlockMonad<int>(TestTask1);
            var m2 = new BlockMonad<string>(TestTask2);

            var mc = new ConcurrentMonad<int, string>(m1, m2);
            _Wait(mc);

            Assert.IsNull(mc.Result);
            Assert.AreEqual(mc.Error.Message, "error 2");
        }

        [Test]
        public void ConcurrentMonad3Test()
        {
            var m1 = new BlockMonad<int>(TestTask1);
            var m3 = new BlockMonad<string>(TestTask3);
            var m4 = new BlockMonad<bool>(TestTask4);

            var mc = new ConcurrentMonad<int, string, bool>(m1, m3, m4);
            _Wait(mc);

            Assert.AreEqual(mc.Result.Item1, 10);
            Assert.AreEqual(mc.Result.Item2, "ok 3");
            Assert.AreEqual(mc.Result.Item3, true);
            Assert.IsNull(mc.Error);
        }

        IEnumerator TestTask4(IReturn<bool> ret)
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            ret.Accept(true);
        }

        [Test]
        public void ConcurrentMonad3ErrorTest()
        {
            var m1 = new BlockMonad<int>(TestTask1);
            var m2 = new BlockMonad<string>(TestTask2);
            var m4 = new BlockMonad<bool>(TestTask4);

            var mc = new ConcurrentMonad<int, string, bool>(m1, m2, m4);
            _Wait(mc);

            Assert.IsNull(mc.Result);
            Assert.AreEqual(mc.Error.Message, "error 2");
        }

        [Test]
        public void ConcurrentMonadArrayTest()
        {
            var mc = Monad.WhenAll(
                new BlockMonad<int>(r => TestTask5( 3, 3, r )),
                new BlockMonad<int>(r => TestTask5(4, 5, r)),
                new BlockMonad<int>(r => TestTask5(2, 1, r)),
                new BlockMonad<int>(r => TestTask5(1, 3, r))
                );

            _Wait(mc);

            Assert.AreEqual(mc.Result[0], 3);
            Assert.AreEqual(mc.Result[1], 4);
            Assert.AreEqual(mc.Result[2], 2);
            Assert.AreEqual(mc.Result[3], 1);
            Assert.IsNull(mc.Error);
        }

        public IEnumerator TestTask5( int value, int iter, IReturn<int> ret)
        {
            for( int i = 0; i < iter; ++i)
            {
                yield return null;
            }

            ret.Accept(value);
        }

        [Test]
        public void ConcurrentMonadArrayErrorTest()
        {
            var mc = Monad.WhenAll(
                new BlockMonad<int>(r => TestTask5(3, 3, r)),
                new BlockMonad<int>(r => TestTask5(4, 5, r)),
                new BlockMonad<int>(r => TestTask5(2, 1, r)),
                new BlockMonad<int>(r => TestTask5(1, 3, r)),
                new BlockMonad<int>(r => TestTask6("error", 3, r))
                );

            _Wait(mc);
            Assert.IsNull(mc.Result);
            Assert.AreEqual(mc.Error.Message, "error");
        }

        public IEnumerator TestTask6(string failMsg, int iter, IReturn<int> ret)
        {
            for (int i = 0; i < iter; ++i)
            {
                yield return null;
            }

            ret.Fail(new System.Exception(failMsg));
        }

        [Test]
        public void ThreadedMonadTest()
        {
            int t = 0;

            var m1 = new ThreadedMonad<int>(() =>
            {
                System.Threading.Thread.Sleep(500);
                return 3*t;
            });

            var m2 = new BlockMonad<int>( r =>
            {
                r.Accept(2);
                return Coroutine.Sleep(0.1f);
            }).Then( res => t = res );

            var m3 = Monad.WhenAll(m1, m2);
            _Wait(m3);

            Assert.AreEqual(6, m3.Result.Item1);
        }
    }
}