using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientServer
{
    class ThreadSafeRandom
    {
        private static readonly Random _global = new Random();
        [ThreadStatic]
        private static Random _local;

        public ThreadSafeRandom()
        {
            if (_local == null)
            {
                int seed;
                lock (_global)
                {
                    seed = _global.Next();
                }
                _local = new Random(seed);
            }
        }
        public int Next()
        {
            return _local.Next();
        }
        public int Next(int min, int max)
        {
            return _local.Next(min, max);
        }
        public int Next(int max)
        {
            return _local.Next(max);
        }
    }
}
