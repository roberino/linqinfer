using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqInfer.Maths
{
    public sealed class MultiVector : IVector
    {
        IReadOnlyCollection<IVector> _vectors;

        public MultiVector(params IVector[] vectors)
        {
            _vectors = vectors;
        }

        internal MultiVector(IEnumerable<IVector> vectors)
        {
            _vectors = (vectors as List<IVector>) ?? vectors.ToList();
        }

        public double this[int index]
        {
            get
            {
                int lastIndex = 0;
                int nextIndex = 0;

                foreach (var vector in _vectors)
                {
                    nextIndex += vector.Size;

                    if (index < nextIndex)
                    {
                        return vector[index - lastIndex];
                    }

                    lastIndex = nextIndex;
                }

                throw new IndexOutOfRangeException(index.ToString());
            }
        }

        public double Sum => _vectors.Select(v => v.Sum).Sum();

        public IEnumerable<IVector> InnerVectors => _vectors.AsEnumerable();

        public int Size => _vectors.Sum(v => v.Size);

        public bool IsIsomorphicTo(MultiVector other)
        {
            if (other == null) return false;
            if (other.Size != Size) return false;
            if (other._vectors.Count != _vectors.Count) return false;
            if (ReferenceEquals(this, other)) return true;

            return other._vectors.Zip(_vectors, (v1, v2) => v1.Size == v2.Size).All(x => x);
        }

        public double DotProduct(IVector vector)
        {
            return ((MultiVector)MultiplyBy(vector)).Sum;
        }

        public IVector MultiplyBy(Matrix matrix)
        {
            var results = new double[matrix.Height];

            for (var i = 0; i < results.Length; i++)
            {
                results[i] = matrix.Rows[i].DotProduct(this);
            }

            return new ColumnVector1D(results);
        }

        public IVector HorizontalMultiply(IMatrix matrix)
        {
            // 1, 2, 3 * x a
            //           y b
            //           z c
            // = [1x + 2y + 3z, 1a + 2b + 3c]

            var result = new double[matrix.Width];
            var j = 0;

            foreach (var row in matrix.Rows)
            {
                var rowVals = row.ToColumnVector().GetUnderlyingArray();

                for (var i = 0; i < rowVals.Length; i++)
                {
                    result[i] += this[j] * rowVals[i];
                }

                j++;
            }

            return new Vector(result);
        }

        public IVector MultiplyBy(IVector vector)
        {
            if (vector is Vector v)
            {
                var arr = v.GetUnderlyingArray();
                int i = 0;

                var vectorResults = new List<IVector>();

                foreach (var vect in _vectors)
                {
                    var virtVect = new PartialVector(arr, i, vect.Size);

                    vectorResults.Add(virtVect.MultiplyBy(vect));

                    i += vect.Size;
                }

                return new MultiVector(vectorResults);
            }
            else
            {
                if (vector is MultiVector mv && IsIsomorphicTo(mv))
                {
                    return new MultiVector(
                        _vectors
                        .Zip(mv._vectors, (v1, v2) => v1.MultiplyBy(v2))
                        .ToList());
                }

                return vector.MultiplyBy(this);
            }
        }

        public bool Equals(IVector other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Size != other.Size) return false;

            if (other is MultiVector)
            {
                foreach(var vp in ((MultiVector)other)._vectors.Zip(_vectors, (x, y) => new { x = x, y = y }))
                {
                    if (!vp.x.Equals(vp.y)) return false;
                }

                return true;
            }
            else
            {
                return other.Equals(ToColumnVector());
            }
        }

        public byte[] ToByteArray()
        {
            var byteCol = _vectors.Select(v => new { type = v.GetType().Name, bytes = v.ToByteArray() }).ToList();
            var header = byteCol.Select(v => $"{v.type}/{v.bytes.Length};").Aggregate(new StringBuilder(), (s, h) => s.Append(h)).ToString();
            var headerBytes = Encoding.ASCII.GetBytes(header);
            var headerLen = BitConverter.GetBytes(headerBytes.Length);
            var allBytes = new byte[byteCol.Sum(b => b.bytes.Length) + headerBytes.Length + headerLen.Length];

            Array.Copy(headerLen, allBytes, headerLen.Length);
            Array.Copy(headerBytes, 0, allBytes, headerLen.Length, headerBytes.Length);

            int i = headerBytes.Length + headerLen.Length;

            foreach(var byteArr in byteCol)
            {
                Array.Copy(byteArr.bytes, 0, allBytes, i, byteArr.bytes.Length);
                i += byteArr.bytes.Length;
            }

            return allBytes;
        }

        public static MultiVector FromByteArray(byte[] bytes)
        {
            var headerLen = BitConverter.ToInt32(bytes, 0);
            var header = Encoding.ASCII.GetString(bytes, sizeof(int), headerLen);

            var vectors = new List<IVector>();

            var i = sizeof(int) + headerLen;

            foreach (var item in header.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Split('/')).Select(p => new { type = p[0], len = int.Parse(p[1]) }))
            {
                var factory = VectorSerialiser.FindVectorFactory(item.type);

                var vbytes = new byte[item.len];

                Array.Copy(bytes, i, vbytes, 0, item.len);

                vectors.Add(factory.Invoke(vbytes));

                i += item.len;
            }

            return new MultiVector(vectors.ToArray());
        }

        public ColumnVector1D ToColumnVector()
        {
            int i = 0;

            var colVects = _vectors.Select(v => v.ToColumnVector()).ToList();
            var size = colVects.Sum(v => v.Size);
            var data = new double[size];

            foreach(var colVect in colVects)
            {
                Array.Copy(colVect.GetUnderlyingArray(), 0, data, i, colVect.Size);

                i += colVect.Size;
            }

            return new ColumnVector1D(data);
        }

        public override int GetHashCode()
        {
            int h = 0;

            foreach(var vect in _vectors)
            {
                h ^= vect.GetHashCode();
            }

            return h;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IVector);
        }

        public override string ToString()
        {
            return ToColumnVector().ToCsv(3);
        }
    }
}
