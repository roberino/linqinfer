using LinqInfer.Data;
using LinqInfer.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Maths
{
    /// <summary>
    /// Represents a matrix of floating point numbers
    /// with various methods for supporting matrix operations
    /// </summary>
    public class Matrix : IEnumerable<Vector>, IEquatable<Matrix>, IExportableAsDataDocument, IImportableFromDataDocument, IJsonExportable, IMatrix
    {
        Lazy<Vector> _mean;
        Lazy<Matrix> _covariance;
        Lazy<Matrix> _cosineSimularity;
        protected readonly IList<Vector> _rows;

        public Matrix(IEnumerable<Vector> rows)
        {
            _rows = rows.ToList();

            if (_rows.Count == 0 || _rows.Select(r => r.Size).Distinct().Count() != 1)
            {
                throw new ArgumentException("Invalid sizes");
            }

            Rows = new IndexableEnumerable<IVector>(_rows);
            Columns = new IndexableEnumerable<IVector>(GetColumns());

            foreach (var r in _rows)
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
        public Vector MeanVector=> _mean.Value;

        /// <summary>
        /// Returns the covariance matrix
        /// </summary>
        public Matrix CovarianceMatrix => _covariance.Value;

        /// <summary>
        /// Returns the cosine distance between each row in the matrix
        /// </summary>
        public Matrix CosineSimularityMatrix => _cosineSimularity.Value;

        /// <summary>
        /// Returns a new matrix with the mean subtracted from the x values
        /// </summary>
        public Matrix MeanAdjust()
        {
            var mu = MeanVector;

            return new Matrix(_rows.Select(v => v - mu));
        }

        /// <summary>
        /// Gets the y dimension of the matrix
        /// </summary>
        public int Height => Rows.Count;

        /// <summary>
        /// Gets the x dimension of the matrix
        /// </summary>
        public int Width => Rows.Count == 0 ? 0 : Rows[0].Size;

        /// <summary>
        /// Returns true if the width and height are equal
        /// </summary>
        public bool IsSquare => Width == Height;

        /// <summary>
        /// Returns the value at the row and column index
        /// </summary>
        public double this[int rowIndex, int colIndex] => Rows[rowIndex][colIndex];

        /// <summary>
        /// Returns the rows of the matrix
        /// </summary>
        public IIndexedEnumerable<IVector> Rows { get; }

        /// <summary>
        /// Returns the columns of the matrix as vectors
        /// </summary>
        public IIndexedEnumerable<IVector> Columns { get; }

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

        /// <summary>
        /// Returns the variance of a row
        /// </summary>
        public double Variance(int x, bool isSampleData = true)
        {
            var mu_x = MeanVector[x];

            return Column(x).Select(v => Math.Pow(v - mu_x, 2)).Sum() / (Height - (isSampleData ? 1 : 0));
        }

        /// <summary>
        /// Returns the covariance of two rows
        /// </summary>
        public double Covariance(int x, int y, bool isSampleData = true)
        {
            var mu_x = MeanVector[x];
            var mu_y = MeanVector[y];
            
            double t = 0;

            foreach (var v in Rows)
            {
                t += (v[x] - mu_x) * (v[y] - mu_y);
            }

            return t / (Height - (isSampleData ? 1 : 0));
        }

        /// <summary>
        /// Returns the cosine distance between two rows
        /// </summary>
        public double CosineDistance(int x, int y) => Rows[y].ToColumnVector().CosineDistance(Rows[x].ToColumnVector());

        /// <summary>
        /// Transposes the matrix into a new matrix
        /// </summary>
        /// <returns></returns>
        public Matrix Transpose()
        {
            var newData = CreateArray(Height, Width);

            for (int y = 0; y < Height; y++)
            {
                var xv = _rows[y].GetUnderlyingArray();

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
                var row = ((Vector)Rows[y]).GetUnderlyingArray();

                for (int x = 0; x < row.Length; x++)
                {
                    action(x, y, row[x]);
                }
            }
        }

        public void Apply(Func<int, int, double, double> valueFunc)
        {
            for (int y = 0; y < _rows.Count; y++)
            {
                _rows[y].Apply((v, x) => valueFunc(x, y, v));
            }

            Setup();
            OnModify();
        }

        internal Matrix ConcatRows(params Vector[] rows)
        {
            foreach(var row in rows)
            {
                _rows.Add(row);
            }

            Setup();
            OnModify();

            return this;
        }

        internal void Overwrite(Matrix other)
        {
            foreach (var row in _rows)
            {
                row.DetachEvents();
            }

            _rows.Clear();

            foreach(var row in other._rows)
            {
                _rows.Add(row);
            }

            Setup();
            OnModify();
        }

        public static Matrix Create(params Vector[] rows)
        {
            return new Matrix(rows);
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

        public static Matrix DiagonalMatrix(Vector values)
        {
            var a = values.GetUnderlyingArray();
            return DiagonalMatrix(i => a[i], values.Size);
        }

        public static Matrix IdentityMatrix(int size)
        {
            return DiagonalMatrix(_ => 1, size);
        }

        public static Matrix RandomMatrix(int width, int height, Range range)
        {
            return new Matrix(Enumerable.Range(0, height).Select(n => Functions.RandomVector(width, range)));
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

            return new Matrix(m1._rows.Zip(m2._rows, (r1, r2) => r1 - r2));
        }

        public static Matrix operator +(Matrix m1, Matrix m2)
        {
            AssertDimensionalEquivalence(m1, m2);

            return new Matrix(m1._rows.Zip(m2._rows, (r1, r2) => r1 + r2));
        }

        public static Matrix operator *(double v, Matrix m2)
        {
            var newRows = new List<Vector>();

            foreach (var row in m2._rows)
            {
                newRows.Add(row * v);
            }

            return new Matrix(newRows);
        }

        public static Matrix operator *(Matrix m1, Matrix m2)
        {
            // a fairly naive and not particullarly efficient algorithm

            if (m2.Height != m1.Width)
            {
                throw new InvalidOperationException();
            }

            if (m2.Width == 1) return (m1 * m2.Columns.First()).ToColumnVector().AsMatrix();

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
                    double[] m1Rowi = m1._rows[i].GetUnderlyingArray();
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

        public IVector Multiply(IVector c)
        {
            // | a, b, c |   x     =   ax + by + cz
            // | d, e, f | * y         dx + ey + fz
            //               z

            var result = new double[Height];

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = Rows[i].DotProduct(c);
            }

            return new ColumnVector1D(result);
        }

        public static IVector operator *(Matrix m, IVector c)
        {
            return m.Multiply(c);
        }

        public static Matrix operator *(Matrix m, double s)
        {
            var data = new List<double[]>(m.Height);

            foreach (var r in m._rows.Select(r => r.GetUnderlyingArray()))
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
            return _rows.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Rows.GetEnumerator();
        }

        public virtual bool Equals(Matrix other)
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

            foreach (var r in _rows)
            {
                x[i] = r.GetUnderlyingArray();
                i++;
            }

            return x;
        }

        public virtual void WriteAsCsv(TextWriter output, char delimitter = ',', int precision = 8)
        {
            foreach (var vect in _rows)
            {
                output.WriteLine(vect.ToCsv(delimitter, precision));
            }
        }

        public override string ToString()
        {
            return _rows.Aggregate(new StringBuilder(), (s, v) => s.AppendLine('|' + v.ToCsv(2) + '|')).ToString();
        }

        internal Matrix SelectRows(params int[] rowIndexes)
        {
            if (rowIndexes.Any(i => i < 0 || i >= Height))
            {
                throw new ArgumentException("Invalid rows");
            }

            return new Matrix(rowIndexes.Select(y => _rows[y]));
        }

        internal Matrix SelectColumns(params int[] columnIndexes)
        {
            if (columnIndexes.Any(i => i < 0 || i >= Width))
            {
                throw new ArgumentException("Invalid columns");
            }

            return new Matrix(_rows.Select(v =>
            {
                var a = v.GetUnderlyingArray();
                return new Vector(columnIndexes.Select(i => a[i]).ToArray());
            }));
        }

        internal Matrix ChopColumns(int columnIndex)
        {
            return new Matrix(_rows.Select(v => v.Split(columnIndex).First()));
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

        void OnModify()
        {
            Modified?.Invoke(this, EventArgs.Empty);
        }

        void Setup()
        {
            _mean = new Lazy<Vector>(() =>
                _rows.Aggregate(new Vector(Width), (m, v) => m + v) / Height);

            _covariance = new Lazy<Matrix>(CalculateCovarianceMatrix);

            _cosineSimularity = new Lazy<Matrix>(CalculateCosineSimularityMatrix);
        }

        Matrix CalculateCovarianceMatrix()
        {
            var data = new List<double[]>(Width);

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

        Matrix CalculateCosineSimularityMatrix()
        {
            var data = new List<double[]>();

            foreach (var d in Enumerable.Range(0, Width))
            {
                var row = new double[Width];

                foreach (var x in Enumerable.Range(0, Width))
                {
                    if (x == d)
                    {
                        row[x] = 0;
                    }
                    else
                    {
                        row[x] = CosineDistance(x, d);
                    }
                }

                data.Add(row);
            }

            return new Matrix(data);
        }

        bool DimensionallyEquivalent(Matrix other) // better math term? 
        {
            if (other == null) return false;

            return (other.Width == Width || other.Height == Height);
        }

        static void AssertDimensionalCompatibility(Matrix m, Vector v)
        {
            if (m.Width != v.Size) throw new ArgumentException("Incompatible dimensions");
        }

        static void AssertDimensionalEquivalence(Matrix m1, Matrix m2)
        {
            if (!m1.DimensionallyEquivalent(m2)) throw new ArgumentException("Incompatible dimensions");
        }

        public virtual PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            foreach (var vect in _rows)
            {
                doc.Vectors.Add(new ColumnVector1D(vect));
            }

            return doc;
        }

        public virtual void ImportData(PortableDataDocument doc)
        {
            _rows.Clear();

            foreach (var vect in doc.Vectors.Select(v => v.ToColumnVector()))
            {
                _rows.Add(vect);
            }

            foreach (var r in _rows)
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

            foreach (var vect in _rows)
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

        IEnumerable<ColumnVector1D> GetColumns()
        {
            for (int i = 0; i < Width; i++)
            {
                yield return new ColumnVector1D(Column(i).ToArray());
            }
        }
    }
}