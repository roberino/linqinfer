``` ini

BenchmarkDotNet=v0.10.12, OS=Windows 10 Redstone 2 [1703, Creators Update] (10.0.15063.909)
Intel Core i7-6500U CPU 2.50GHz (Skylake), 1 CPU, 4 logical cores and 2 physical cores
Frequency=2531248 Hz, Resolution=395.0620 ns, Timer=TSC
.NET Core SDK=2.0.2
  [Host]     : .NET Core 2.0.3 (Framework 4.6.25815.02), 64bit RyuJIT
  DefaultJob : .NET Core 2.0.3 (Framework 4.6.25815.02), 64bit RyuJIT


```
|                  Method | Width | Height |                Mean |             Error |             StdDev |              Median |
|------------------------ |------ |------- |--------------------:|------------------:|-------------------:|--------------------:|
|         **Matrix_Multiply** |     **5** |      **5** |      **33,946.5694 ns** |       **971.0194 ns** |      **2,738.7776 ns** |      **33,774.1072 ns** |
|         Matrix_Addition |     5 |      5 |       3,323.1778 ns |        94.4132 ns |        275.4079 ns |       3,261.2334 ns |
| Matrix_CovarianceMatrix |     5 |      5 |           0.6336 ns |         0.0596 ns |          0.1662 ns |           0.6074 ns |
|         **Matrix_Multiply** |     **5** |    **100** |                  **NA** |                **NA** |                 **NA** |                  **NA** |
|         Matrix_Multiply |     5 |    100 |                  NA |                NA |                 NA |                  NA |
|         Matrix_Addition |     5 |    100 |      27,428.6191 ns |       769.7525 ns |      2,183.6598 ns |      26,952.6496 ns |
|         Matrix_Addition |     5 |    100 |      26,901.3215 ns |       869.7379 ns |      2,481.4112 ns |      26,692.3557 ns |
| Matrix_CovarianceMatrix |     5 |    100 |           0.6817 ns |         0.0608 ns |          0.1715 ns |           0.6416 ns |
| Matrix_CovarianceMatrix |     5 |    100 |           0.5802 ns |         0.0571 ns |          0.1242 ns |           0.5857 ns |
|         **Matrix_Multiply** |   **100** |      **5** |                  **NA** |                **NA** |                 **NA** |                  **NA** |
|         Matrix_Addition |   100 |      5 |       6,040.8511 ns |       241.2339 ns |        688.2539 ns |       5,860.2884 ns |
| Matrix_CovarianceMatrix |   100 |      5 |           0.7531 ns |         0.0576 ns |          0.1137 ns |           0.7156 ns |
|         **Matrix_Multiply** |   **100** |    **100** | **139,485,381.1102 ns** | **3,643,033.5127 ns** | **10,626,899.7513 ns** | **139,162,477.8569 ns** |
|         Matrix_Multiply |   100 |    100 | 140,546,906.7055 ns | 2,786,435.3458 ns |  7,340,585.2312 ns | 140,475,176.4250 ns |
|         Matrix_Addition |   100 |    100 |      61,767.4328 ns |     1,476.8427 ns |      1,450.4569 ns |      61,227.4114 ns |
|         Matrix_Addition |   100 |    100 |      70,838.3600 ns |     2,434.0918 ns |      7,138.7698 ns |      69,363.4229 ns |
| Matrix_CovarianceMatrix |   100 |    100 |           0.5270 ns |         0.0560 ns |          0.1144 ns |           0.4807 ns |
| Matrix_CovarianceMatrix |   100 |    100 |           0.7465 ns |         0.0673 ns |          0.0629 ns |           0.7524 ns |
|         **Matrix_Multiply** |   **500** |      **5** |                  **NA** |                **NA** |                 **NA** |                  **NA** |
|         Matrix_Addition |   500 |      5 |      13,199.3597 ns |       394.2379 ns |      1,137.4666 ns |      12,878.0616 ns |
| Matrix_CovarianceMatrix |   500 |      5 |           1.0209 ns |         0.0520 ns |          0.0461 ns |           1.0172 ns |
|         **Matrix_Multiply** |   **500** |    **100** |                  **NA** |                **NA** |                 **NA** |                  **NA** |
|         Matrix_Multiply |   500 |    100 |                  NA |                NA |                 NA |                  NA |
|         Matrix_Addition |   500 |    100 |     296,486.6562 ns |     6,010.6207 ns |     16,755.2080 ns |     294,034.3758 ns |
|         Matrix_Addition |   500 |    100 |     296,699.2176 ns |     7,883.1231 ns |     22,618.1579 ns |     292,808.6322 ns |
| Matrix_CovarianceMatrix |   500 |    100 |      12,616.2101 ns |       247.9812 ns |        339.4395 ns |      12,460.1709 ns |
| Matrix_CovarianceMatrix |   500 |    100 |          27.4219 ns |         0.7355 ns |          2.1222 ns |          27.3334 ns |

Benchmarks with issues:
  MatrixBenchmarks.Matrix_Multiply: DefaultJob [Width=5, Height=100]
  MatrixBenchmarks.Matrix_Multiply: DefaultJob [Width=5, Height=100]
  MatrixBenchmarks.Matrix_Multiply: DefaultJob [Width=100, Height=5]
  MatrixBenchmarks.Matrix_Multiply: DefaultJob [Width=500, Height=5]
  MatrixBenchmarks.Matrix_Multiply: DefaultJob [Width=500, Height=100]
  MatrixBenchmarks.Matrix_Multiply: DefaultJob [Width=500, Height=100]
