using System.Collections;
using System;

namespace Rayark.Mast
{
    class Joinable : IEnumerator, IDisposable
    {
        IEnumerator _target;
        bool _running;

        public Joinable(IEnumerator target)
        {
            _target = target;
            _running = true;
        }

        public object Current
        {
            get
            {
                return _target.Current;
            }
        }

        public bool MoveNext()
        {
            _running = _target.MoveNext();
            return _running;
        }

        public void Reset()
        {
            _target.Reset();
        }

        public IEnumerator Join()
        {
            while (_running)
                yield return null;
        }

        public void Dispose()
        {
            var dispoable = _target as IDisposable;
            if( dispoable != null )
                dispoable.Dispose();
        }
    }
}