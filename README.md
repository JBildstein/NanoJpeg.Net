# NanoJpeg.Net-unsafe
C# port of NanoJpeg with unsafe code (i.e. pointer)

## Origin

NanoJpeg is a minimal Jpeg decoder by Martin Fiedler written in C and can be found here: http://keyj.emphy.de/nanojpeg/

## Port

The port is a little bit different than the original version. It's more C#/.Net-ish in its structure and naming.
For better performance it uses unsafe code.
For safety, the allocated memory is either wrapped in a try/finally block and/or is released by the `Dispose` method or class finalizer.

If you do not want to or can't use unsafe code, check the port with only managed code: https://github.com/Deathspike/NanoJPEG.NET 

Performance of this version is about 50% better than the managed version.

## Usage

Basic usage is like this:
```csharp
using (NJImage img = new NJImage())
{
    img.Decode(@"path\to\file.jpg");
    //Now the pointer img.Image points to the pixel data
}
```

There are also a few overloads of the `Decode` method taking streams, a byte array and byte pointer. `Decode(byte*, int, bool)` is the main method that is called by each other overloads.

You can call the `Decode` method as many times as you like, but note that all properties (like `Width`, `Height`, `Image`, etc.) will always be in respect to the latest decoded image.

**The NJImage class is not thread safe!!!**

## Example Program

The folder NanoJpegApp contains a small console application that opens the jpeg and saves it as either ppm (rgb) or pgm (gray).
```
NanoJpegApp.exe C:\input.jpeg C:\output.ppm
```