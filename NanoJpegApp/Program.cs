using System;
using System.IO;
using System.Text;
using NanoJpeg;

namespace NanoJpegApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length < 2)
            {
                Console.WriteLine("Example Usage:");
                Console.WriteLine(@"NanoJpegApp.exe C:\input.jpg C:\output.ppm");
            }
            else
            {
                try
                {
                    string inPath = args[0];
                    string outPath = args[1];
                    string inFilename = Path.GetFileName(inPath);
                    string outFilename = Path.GetFileName(outPath);

                    //// Reading ////

                    Console.WriteLine("Starting to read {0}...", inFilename);

                    byte[] data = File.ReadAllBytes(inPath);
                    var img = new Image(data);

                    bool isRgb = img.ChannelCount > 1;
                    Console.WriteLine($"Success reading {inFilename}: {img.Width}x{img.Height} - {(isRgb ? "RGB" : "Gray")}");
                    Console.WriteLine();

                    //// Writing ////

                    outPath = Path.ChangeExtension(outPath, isRgb ? ".ppm" : ".pgm");
                    Console.WriteLine($"Starting to write to {outFilename}...");

                    string outDir = Path.GetDirectoryName(outFilename);
                    if (outDir != string.Empty && !Directory.Exists(outDir)) { Directory.CreateDirectory(outDir); }
                    string headerString = string.Format($"P{(img.ChannelCount > 1 ? 6 : 5)}\n{img.Width} {img.Height}\n255\n");
                    byte[] headerBytes = Encoding.ASCII.GetBytes(headerString);

                    using (var fsOut = File.Create(outPath))
                    {
                        fsOut.Write(headerBytes, 0, headerBytes.Length);
                        fsOut.Write(img.Data, 0, img.Data.Length);
                    }

                    Console.WriteLine($"Success writing {outFilename}: {headerBytes.Length + img.Data.Length}bytes");
                }
                catch (DecodeException njex)
                {
                    Console.WriteLine("Error decoding jpeg: " + njex.ErrorCode);
                    Console.WriteLine(njex.StackTrace);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }

                Console.WriteLine();
                Console.WriteLine("Press any key to close...");
                Console.ReadKey();
            }
        }
    }
}
