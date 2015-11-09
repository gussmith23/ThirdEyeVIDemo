using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientServer
{
    static class DiffieHellman
    {
        private static long _P = 0;
        private static long _G = 0;

        public static long P { get { return _P; } }
        public static long G { get { return _G; } }
        
        public static void Init(int n)
        {
            GenerateP(n);
            GenerateG(n);
        }
        private static void GenerateP(long n)
        {
            //_P = (((2 ^ n) - 1) ^ 2) - 2;   // Carol prime
            _P = n * (2 ^ n) + 1;             // Cullen prime
        }
        private static void GenerateG(long n)
        {
            _G = 5;
        }


        public static long GenerateExchange(long secret)
        {
            return (G ^ secret) % P;
        }
        public static long Extract(long secret, long exchange)
        {
            return (exchange ^ secret) % P;
        }

    }
}
