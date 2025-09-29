```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.19045.6332/22H2/2022Update)
AMD Ryzen 7 5800H with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 8.0.20 (8.0.2025.41914), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.20 (8.0.2025.41914), X64 RyuJIT AVX2


```
| Method         | Mean     | Error    | StdDev   | P95      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Baseline_Email | 600.1 ns | 11.50 ns | 10.76 ns | 613.7 ns |  1.00 |    0.00 | 0.0229 |     192 B |        1.00 |
| Hardened_Email | 596.1 ns |  7.14 ns |  6.33 ns | 603.6 ns |  0.99 |    0.02 | 0.0229 |     192 B |        1.00 |
| Phone          | 767.8 ns | 13.01 ns | 12.17 ns | 780.3 ns |  1.28 |    0.03 | 0.0229 |     192 B |        1.00 |
| Jwt            | 530.0 ns | 10.08 ns |  9.90 ns | 541.3 ns |  0.88 |    0.02 |      - |         - |        0.00 |
