```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.19045.6332/22H2/2022Update)
AMD Ryzen 7 5800H with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 8.0.20 (8.0.2025.41914), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.20 (8.0.2025.41914), X64 RyuJIT AVX2


```
| Method | Sample               | Mean        | Error     | StdDev    | Gen0   | Allocated |
|------- |--------------------- |------------:|----------:|----------:|-------:|----------:|
| **Phone**  | **Conta(...)VCJ9. [94]** | **1,045.62 ns** | **20.117 ns** | **24.706 ns** | **0.0229** |     **192 B** |
| **Phone**  | **emai(...)0958 [1059]** | **1,102.36 ns** |  **9.635 ns** |  **7.523 ns** | **0.2518** |    **2120 B** |
| **Phone**  | **jwt (...)ciOi [4111]** | **1,972.47 ns** |  **2.970 ns** |  **2.318 ns** |      **-** |         **-** |
| **Phone**  | **noise(...)xxxxx [45]** |    **24.17 ns** |  **0.127 ns** |  **0.106 ns** |      **-** |         **-** |
