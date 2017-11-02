using LinqInfer.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;

namespace LinqInfer.Maths
{
    /// <summary>
    /// Represents a matrix of floating point numbers
    /// with various methods for supporting matrix operations
    /// </summary>
    public class Matrix : IEnumerable<Vector>, IEquatable<Matrix>, IExportableAsVectorDocument, IImportableAsVectorDocument, IJsonExportable
    {
        private Lazy<Vector> _mean;
        private Lazy<Matrix> _covariance;

        public Matrix(IEnumerable<Vector> rows)
        {
            var rowList = rows.ToList();

            if (rowList.Count == 0 || rowList.Select(r => r.Size).Distinct().Count() != 1)
            {
                throw new ArgumentException("Invalid sizes");
            }

            Rows = rowList;

            foreach (var r in rowList)
            {
                r.Modified += (s, e) =>
                {
                    Setup();
                    OnModify();
                };
            }

            Setup();
        }

        public Matrix(IEnumerable<double[]> rows) : this(rows.Select(r => new Vector(r)))
        {
        }

        public Matrix(double[][] data) : this(data.Select(x => new Vector(x)))
        {
        }

        /// <summary>
        /// Fires when underlying data changes
        /// </summary>
        public event EventHandler Modified;

        /// <summary>
        /// Returns a mean of each dimension
        /// </summary>
        public Vector MeanVector { get { return _mean.Value; } }

        /// <summary>
        /// Returns the covariance matrix
        /// </summary>
        public Matrix CovarianceMatrix { get { return _covariance.Value; } }

        /// <summary>
        /// Returns a new matrix with the mean subtracted from the x values
        /// </summary>
        public Matrix MeanAdjust()
        {
            var mu = MeanVector;

            return new Matrix(Rows.Select(v => v - mu));
        }

        /// <summary>
        /// Gets the y dimension of the matrix
        /// </summary>
        public int Height { get { return Rows.Count; } }

        /// <summary>
        /// Gets the x dimension of the matrix
        /// </summary>
        public int Width { get { return Rows.Count == 0 ? 0 : Rows[0].Size; } }

        /// <summary>
        /// Returns true if the width and height are equal
        /// </summary>
        public bool IsSquare { get { return Width == Height; } }

        /// <summary>
        /// Returns the value at the row and column index
        /// </summary>
        public double this[int rowIndex, int colIndex] { get { return Rows[rowIndex][colIndex]; } }

        public IList<Vector> Rows { get; }

        /// <summary>
        /// Returns the columns of the matrix as vectors
        /// </summary>
        public IEnumerable<ColumnVector1D> Columns
        {
            get
            {
                for (int i = 0; i < Width; i++)
                {
                    yield return new ColumnVector1D(Column(i).ToArray());
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

        public double ColumnSum(int index)
        {
            double total = 0;

            for(var i = 0; i < Height; i++)
            {
                total += Rows[i][index];
            }

            return total;
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

            // return Rows.Select(v => (v[x] - mu_x) * (v[y] - mu_y)).Sum() / (Height - (isSampleData ? 1 : 0));

            double t = 0;

            foreach (var v in Rows)
            {
                t += (v[x] - mu_x) * (v[y] - mu_y);
            }

            return t / (Height - (isSampleData ? 1 : 0));
        }

        /// <summary>
        /// Transposes the matrix into a new matrix
        /// </summary>
        /// <returns></returns>
        public Matrix Transpose()
        {
            var newData = CreateArray(Height, Width);

            for (int y = 0; y < Height; y++)
            {
                var xv = Rows[y].GetUnderlyingArray();

                for (int x = 0; x < xv.Length; x++)
                {
                    newData[x][y] = xv[x];
                }
            }

            return new Matrix(newData);
        }

        /// <summary>
        /// Rotates the elements in the matrix (clockwise by default)
        /// </summary>
        public Matrix Rotate(bool clockwise = true)
        {
            var newData = new List<double[]>();

            if (clockwise)
            {
                for (int y = 0; y < Width; y++)
                {
                    var row = new double[Height];
                    for (int x = 0; x < row.Length; x++)
                    {
                        row[x] = this[Height - x - 1, y];
                    }
                    newData.Add(row);
                }
            }
            else
            {
                for (int y = 0; y < Width; y++)
                {
                    var row = new double[Height];
                    for (int x = 0; x < row.Length; x++)
                    {
                        row[x] = this[x, Width - y - 1];
                    }
                    newData.Add(row);
                }
            }

            return new Matrix(newData);
        }

        public void Iterate(Action<int, int, double> action)
        {
            for (int y = 0; y < Rows.Count; y++)
            {
                var row = Rows[y].GetUnderlyingArray();

                for (int x = 0; x < row.Length; x++)
                {
                    action(x, y, row[x]);
                }
            }
        }

        public void Apply(Func<int, int, double, double> valueFunc)
        {
            for (int y = 0; y < Rows.Count; y++)
            {
                Rows[y].Apply((v, x) => valueFunc(x, y, v));
            }

            Setup();
            OnModify();
        }

        public static Matrix DiagonalMatrix(Func<int, double> valueFactory, int size)
        {
            Contract.Assert(size > 0);

            var data = CreateArray(size, size);

            for (int i = 0; i < size; i++)
            {
                data[i][i] = valueFactory(i);
            }

            return new Matrix(data);
        }

        public static Matrix Create(params Vector[] rows)
        {
            return new Matrix(rows);
        }

        public static Matrix DiagonalMatrix(Vector values)
        {
            var a = values.GetUnderlyingArray();
            return DiagonalMatrix(i => a[i], values.Size);
        }

        public static Matrix IdentityMatrix(int size)
        {
            return DiagonalMatrix(_ => 1, size);
        }

        internal static Matrix Multiply(Matrix a, Matrix b)
        {
            if (a.Height != b.Width)
            {
                throw new InvalidOperationException();
            }

            var ab = new Matrix(CreateArray(a.Height, b.Width));
            var bcols = b.Columns.ToArray();

            ab.Apply((i, k, _) => a.Rows[k].DotProduct(bcols[i]));

            return ab;
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

            double[][] c = CreateArray(m1.Height, m2.Width);

            var m2Colj = new double[m1.Width];

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

        public ColumnVector1D Multiply(ColumnVector1D c)
        {
            // a, b * x     =   ax + by
            // c, d   y         cx + dx

            var result = new double[Height];

            for (var i = 0; i < result.Length; i++)
            {
                var x = Rows[i] * c;
                result[i] = x.Sum();
            }

            return new ColumnVector1D(result);
        }

        public static ColumnVector1D operator *(Matrix m, ColumnVector1D c)
        {
            // | a, b, c | * x     =   ax + by + cz
            // | d, e, f |   y         dx + ey + fz
            //               z

            var result = new double[m.Height];
            int i = 0;

            foreach (var row in m.Rows)
            {
                result[i++] = c.DotProduct(row);
            }

            return new ColumnVector1D(result);

            // return new ColumnVector1D(m.Rows.Select(r => (r * c).Sum()).ToArray());
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

            foreach (var r in Rows)
            {
                x[i] = r.GetUnderlyingArray();
                i++;
            }

            return x;
        }

        public void WriteAsCsv(TextWriter output, char delimitter = ',', int precision = 8)
        {
            foreach (var vect in Rows)
            {
                output.WriteLine(vect.ToCsv(delimitter, precision));
            }
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

        private void OnModify()
        {
            Modified?.Invoke(this, EventArgs.Empty);
        }

        private void Setup()
        {
            _mean = new Lazy<Vector>(() =>
                Rows.Aggregate(new Vector(Width), (m, v) => m + v) / Height);

            _covariance = new Lazy<Matrix>(CalculateCovarianceMatrix);
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

        public BinaryVectorDocument ToVectorDocument()
        {
            var doc = new BinaryVectorDocument();

            foreach (var vect in Rows)
            {
                doc.Vectors.Add(new ColumnVector1D(vect));
            }

            return doc;
        }

        public void FromVectorDocument(BinaryVectorDocument doc)
        {
            Rows.Clear();

            foreach (var vect in doc.Vectors)
            {
                Rows.Add(vect);
            }

            foreach (var r in Rows)
            {
                r.Modified += (s, e) =>
                {
                    Setup();
                    OnModify();
                };
            }

            Setup();
        }

        public void WriteJson(TextWriter output)
        {
            Contract.Ensures(output != null);
            output.WriteLine("[");

            bool isFirst = true;

            foreach(var vect in Rows)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    output.Write(",");
                    output.WriteLine();
                }
                vect.WriteJson(output);
            }

            output.WriteLine("]");
        }
    }
}