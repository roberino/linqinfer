﻿using System;
using System.Security.Cryptography;

namespace LinqInfer.Maths
{
    class Random : IDisposable
    {
        readonly RandomNumberGenerator _randG;

        public Random()
        {
            _randG = RandomNumberGenerator.Create();
        }

        public double NextDouble()
        {
            var data = new byte[sizeof(long)];

            _randG.GetBytes(data);

            var last = data[sizeof(long) - 1];

            data[sizeof(long) - 1] = (byte)Math.Round(last / 255d * 126d);

            return BitConverter.ToInt64(data, 0) / (double)long.MaxValue;
        }

        public int Next(int maxExclusive)
        {
            var data = new byte[sizeof(int)];
            var max = BitConverter.GetBytes(maxExclusive - 1);

            _randG.GetBytes(data);

            for (int i = 0; i < data.Length; i++)
            {
                var x = data[i];
                var m = max[i];
                if (m < 255) data[i] = (byte)Math.Round(x / 255d * m);
            }

            var r = BitConverter.ToInt32(data, 0);

            return r;
        }

        public void Dispose()
        {
            _randG.Dispose();
        }
    }
}