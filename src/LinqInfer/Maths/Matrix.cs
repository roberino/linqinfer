using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqInfer.Maths
{
    public class Matrix : IEnumerable<Vector>, IEquatable<Matrix>
    {
        private readonly Lazy<Vector> _mean;
        private readonly Lazy<Matrix> _covariance;

        public Matrix(IEnumerable<Vector> rows)
        {
            var rowList = rows.ToList();

            if (rowList.Count == 0 || rowList.Select(r => r.Size).Distinct().Count() != 1)
            {
                throw new ArgumentException("Invalid sizes");
            }

            Rows = rowList;

            _mean = new Lazy<Vector>(() =>
                Rows.Aggregate(new Vector(Width), (m, v) => m + v) / Height);

            _covariance = new Lazy<Matrix>(CalculateCovarianceMatrix);
        }

        public Matrix(IEnumerable<double[]> rows) : this(rows.Select(r => new Vector(r)))
        {
        }

        public Vector MeanVector { get { return _mean.Value; } }

        public Matrix CovarianceMatrix { get { return _covariance.Value; } }

        public int Height { get { return Rows.Count; } }

        public int Width { get { return Rows[0].Size; } }

        public double this[int rowIndex, int colIndex] { get { return Rows[rowIndex][colIndex]; } }

        public IList<Vector> Rows { get; private set; }

        public IEnumerable<double> Column(int index)
        {
            foreach (var row in Rows)
            {
                yield return row[index];
            }
        }

        public double Variance(int x)
        {
            var mu_x = MeanVector[x];

            return Column(x).Select(v => Math.Pow(v - mu_x, 2)).Sum();
        }

        public double Covariance(int x, int y)
        {
            var mu_x = MeanVector[x];
            var mu_y = MeanVector[y];

            return Rows.Select(v => (v[x] - mu_x) * (v[y] - mu_y)).Sum();
        }

        public Matrix Rotate()
        {
            var newData = new List<double[]>();

            for (int y = 0; y < Width; y++)
            {
                var row = new double[Height];
                for (int x = 0; x < Height; x++)
                {
                    row[x] = this[Height - x - 1, y];
                }
                newData.Add(row);
            }

            return new Matrix(newData);
        }

        public static Matrix operator -(Matrix m1, Matrix m2)
        {
            AssertDimensionalEquivalence(m1, m2);

            return new Matrix(m1.Rows.Zip(m2.Rows, (r1, r2) => r1 - r2));
        }

        public static Matrix operator +(Matrix m1, Matrix m2)
        {
            AssertDimensionalEquivalence(m1, m2);

            return new Matrix(m1.Rows.Zip(m2.Rows, (r1, r2) => r1 + r2));
        }

        public static Matrix operator *(Matrix m1, Matrix m2)
        {
            AssertDimensionalEquivalence(m1, m2);

            return new Matrix(m1.Rows.Zip(m2.Rows, (r1, r2) => r1 * r2));
        }

        public static Matrix operator *(Matrix m, Vector v)
        {
            AssertDimensionalCompatibility(m, v);

            return new Matrix(m.Rows.Select(x => x * v));
        }

        public static Matrix operator /(Matrix m1, Matrix m2)
        {
            AssertDimensionalEquivalence(m1, m2);

            return new Matrix(m1.Rows.Zip(m2.Rows, (r1, r2) => r1 / r2));
        }

        public static Matrix operator /(Matrix m, Vector v)
        {
            AssertDimensionalCompatibility(m, v);

            return new Matrix(m.Rows.Select(x => x / v));
        }

        public IEnumerator<Vector> GetEnumerator()
        {
            return Rows.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Rows.GetEnumerator();
        }

        public bool Equals(Matrix other)
        {
            if (!DimensionallyEquivalent(other)) return false;

            return this.Zip(other, (r1, r2) => r1.Equals(r2)).All(x => x);
        }

        public override int GetHashCode()
        {
            return StructuralComparisons.StructuralEqualityComparer.GetHashCode(Rows);
        }

        public override string ToString()
        {
            return Rows.Aggregate(new StringBuilder(), (s, v) => s.AppendLine('|' + v.ToCsv(2) + '|')).ToString();
        }

        private Matrix CalculateCovarianceMatrix()
        {
            var data = new List<double[]>();

            foreach (var d in Enumerable.Range(0, Width))
            {
                var row = new double[Width];

                foreach (var x in Enumerable.Range(0, Width))
                {
                    if (x == d)
                    {
                        row[x] = Variance(x);
                    }
                    else
                    {
                        row[x] = Covariance(x, d);
                    }
                }

                data.Add(row);
            }

            return new Matrix(data);
        }

        private bool DimensionallyEquivalent(Matrix other) // better math term? 
        {
            if (other == null) return false;

            return (other.Width == Width || other.Height == Height);
        }

        private static void AssertDimensionalCompatibility(Matrix m, Vector v)
        {
            if (m.Width != v.Size) throw new ArgumentException("Incompatible dimensions");
        }

        private static void AssertDimensionalEquivalence(Matrix m1, Matrix m2)
        {
            if (!m1.DimensionallyEquivalent(m2)) throw new ArgumentException("Incompatible dimensions");
        }
    }
}