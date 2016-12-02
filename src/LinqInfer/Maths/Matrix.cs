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

        public Matrix(double[][] data) : this(data.Select(x => new Vector(x)))
        {
        }

        public Vector MeanVector { get { return _mean.Value; } }

        public Matrix CovarianceMatrix { get { return _covariance.Value; } }

        public Matrix MeanAdjust()
        {
            var mu = MeanVector;

            return new Matrix(Rows.Select(v => v - mu));
        }

        public int Height { get { return Rows.Count; } }

        public int Width { get { return Rows[0].Size; } }

        public bool IsSquare { get { return Width == Height; } }

        public double this[int rowIndex, int colIndex] { get { return Rows[rowIndex][colIndex]; } }

        public IList<Vector> Rows { get; private set; }

        public IEnumerable<Vector> Columns
        {
            get
            {
                for (int i = 0; i < Width; i++)
                {
                    yield return new Vector(Column(i).ToArray());
                }
            }
        }

        public IEnumerable<double> Column(int index)
        {
            foreach (var row in Rows)
            {
                yield return row[index];
            }
        }

        public double Variance(int x, bool isSampleData = true)
        {
            var mu_x = MeanVector[x];

            return Column(x).Select(v => Math.Pow(v - mu_x, 2)).Sum() / (Height - (isSampleData ? 1 : 0));
        }

        /// <summary>
        /// Returns the covariance of two elements
        /// </summary>
        public double Covariance(int x, int y, bool isSampleData = true)
        {
            var mu_x = MeanVector[x];
            var mu_y = MeanVector[y];

            return Rows.Select(v => (v[x] - mu_x) * (v[y] - mu_y)).Sum() / (Height - (isSampleData ? 1 : 0));
        }

        /// <summary>
        /// Rotates the elements in the matrix clockwise
        /// </summary>
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
            // a fairly naive and not particullarly efficient algorithm

            if (m2.Height != m1.Width)
            {
                throw new InvalidOperationException();
            }

            if (m2.Width == 1) return (m1 * m2.Columns.First()).AsMatrix();

            //if (!(m1.IsSquare && m2.IsSquare)) throw new InvalidOperationException();

            // var data = new List<double[]>(m1.Height);

            //for (var y = 0; y < m1.Height; y++)
            //{
            //    var newrow = new double[m1.Width];
            //    var row = m1.Rows[y].GetUnderlyingArray();

            //    for (var x = 0; x < m1.Width; x++)
            //    {
            //        for (int y2 = 0; y2 < m2.Height; y2++)
            //        {
            //            newrow[x] += row[y2] * m2.Rows[y2][x];
            //        }
            //    }

            //    data.Add(newrow);
            //}

            double[][] c = CreateArray(m1.Height, m2.Width);

            var m2Colj = new double[m2.Width];

            for (int j = 0; j < m2.Width; j++)
            {
                for (int k = 0; k < m1.Width; k++)
                {
                    m2Colj[k] = m2[k, j];
                }

                for (int i = 0; i < m1.Height; i++)
                {
                    double[] m1Rowi = m1.Rows[i].GetUnderlyingArray();
                    double s = 0;

                    for (int k = 0; k < m1.Width; k++)
                    {
                        s += m1Rowi[k] * m2Colj[k];
                    }

                    c[i][j] = s;
                }
            }

            return new Matrix(c);

            // return new Matrix(data);
        }

        public static ColumnVector1D operator *(Matrix m, Vector v)
        {
            AssertDimensionalCompatibility(m, v);

            var data = new double[v.Size];

            int yi = 0;

            foreach (var y in m.Rows.Select(r => r.GetUnderlyingArray()))
            {
                for (int x = 0; x < v.Size; x++)
                {
                    data[yi] += y[x] * v[x];
                }
                yi++;
            }

            return new ColumnVector1D(data);
        }

        public static ColumnVector1D operator *(Matrix x, ColumnVector1D c)
        {
            // a, b * x     =   ax + by
            // c, d   y         cx + dx
                
            return new ColumnVector1D(x.Rows.Select(v => (v * c).Sum()).ToArray());
        }

        public static Matrix operator *(Matrix m, double s)
        {
            var data = new List<double[]>(m.Height);

            foreach (var r in m.Rows.Select(r => r.GetUnderlyingArray()))
            {
                var row = new double[m.Width];

                for (var x = 0; x < r.Length; x++)
                {
                    row[x] = r[x] * s;
                }
            }

            return new Matrix(data);
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

        public double[][] ToArray()
        {
            var x = new double[Height][];
            var i = 0;

            foreach(var r in Rows)
            {
                x[i] = r.GetUnderlyingArray(); 
                i++;
            }

            return x;
        }

        public override string ToString()
        {
            return Rows.Aggregate(new StringBuilder(), (s, v) => s.AppendLine('|' + v.ToCsv(2) + '|')).ToString();
        }

        internal Matrix SelectRows(params int[] rowIndexes)
        {
            if (rowIndexes.Any(i => i < 0 || i >= Height))
            {
                throw new ArgumentException("Invalid rows");
            }

            return new Matrix(rowIndexes.Select(y => Rows[y]));
        }

        internal Matrix SelectColumns(params int[] columnIndexes)
        {
            if (columnIndexes.Any(i => i < 0 || i >= Width))
            {
                throw new ArgumentException("Invalid columns");
            }

            return new Matrix(Rows.Select(v =>
            {
                var a = v.GetUnderlyingArray();
                return new Vector(columnIndexes.Select(i => a[i]).ToArray());
            }));
        }

        internal Matrix ChopColumns(int columnIndex)
        {
            return new Matrix(Rows.Select(v => v.Split(columnIndex).First()));
        }

        internal static double[][] CreateArray(int width, int height)
        {
            var x = new double[height][];

            for (int i = 0; i < height; i++)
            {
                x[i] = new double[width];
            }

            return x;
        }

        private Matrix CalculateCovarianceMatrix()
        {
            var data = new List<double[]>();

            if (Height == 1)
            {
                foreach (var d in Enumerable.Range(0, Width))
                {
                    data.Add(Enumerable.Range(0, Width).Select(_ => 0d).ToArray());
                }
            }
            else
            {
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