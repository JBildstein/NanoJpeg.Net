# NanoJpeg.Net
C# port of NanoJpeg

## Origin

NanoJpeg is a minimal Jpeg decoder by Martin Fiedler written in C and can be found here: http://keyj.emphy.de/nanojpeg/

## Port

The port is a bit different than the original version.
It's more in line with .NET naming standards and includes some modifications for performance and to make the decoding logic thread safe.
The first version of this port used unmanaged code but the new version is based on purely managed code.
The new version has the same or better performance than the old unmanaged version by using `Span<T>` and `Vector`.

There is also another port using only managed code: https://github.com/Deathspike/NanoJPEG.NET 

## Usage

Basic usage is like this:
```csharp
using System.IO;
using NanoJpeg;

...

byte[] data = File.ReadAllBytes(@"path\to\file.jpg");
var img = new Image(data);
byte[] rawPixels = img.Data;
```

## Performance

```
BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i5-6600K CPU 3.50GHz (Skylake), 1 CPU, 4 logical and 4 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  Job-AYWZSH : .NET Framework 4.8 (4.8.4042.0), X64 RyuJIT
  Job-EHRCEX : .NET Core 2.2.8 (CoreCLR 4.6.28207.03, CoreFX 4.6.28208.02), X64 RyuJIT
  Job-SDUCOV : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT

InvocationCount=1  UnrollFactor=1
```

|       Method |       Runtime |     Mean |   Error |  StdDev |   Median |
|------------- |-------------- |---------:|--------:|--------:|---------:|
|   New (Span) |      .NET 4.8 | 133.1 ms | 2.60 ms | 4.62 ms | 132.1 ms |
|      Managed |      .NET 4.8 | 153.0 ms | 2.61 ms | 2.45 ms | 154.4 ms |
|    Unmanaged |      .NET 4.8 | 137.9 ms | 2.74 ms | 6.35 ms | 133.9 ms |
|||||||
|   New (Span) | .NET Core 2.2 | 108.4 ms | 1.58 ms | 1.32 ms | 107.6 ms |
|      Managed | .NET Core 2.2 | 157.0 ms | 3.12 ms | 3.34 ms | 158.6 ms |
|    Unmanaged | .NET Core 2.2 | 135.1 ms | 2.73 ms | 2.92 ms | 133.8 ms |
|||||||
|   New (Span) | .NET Core 3.1 | 108.9 ms | 2.28 ms | 2.34 ms | 108.8 ms |
|      Managed | .NET Core 3.1 | 149.1 ms | 2.46 ms | 2.06 ms | 148.6 ms |
|    Unmanaged | .NET Core 3.1 | 137.2 ms | 2.80 ms | 5.53 ms | 134.0 ms |

**Legend:**

 - **New (Span):** new and reworked version using `Span<T>` and `Vector`
 - **Managed:** this version here: https://github.com/Deathspike/NanoJPEG.NET
 - **Unmanaged:** the old version using unmanaged code

## Example Program

The folder NanoJpegApp contains a small console application that opens the jpeg and saves it as either ppm (rgb) or pgm (gray).
```
NanoJpegApp.exe C:\input.jpeg C:\output.ppm
```