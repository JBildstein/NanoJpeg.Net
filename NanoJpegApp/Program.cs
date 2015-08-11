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

                    using (NJImage img = new NJImage())
                    {
                        img.Decode(inPath);

                        Console.WriteLine("Success reading {0}: {1}x{2} - {3}", inFilename, img.Width, img.Height, img.IsColor ? "RGB" : "Gray");
                        Console.WriteLine();

                        //// Writing ////

                        outPath = Path.ChangeExtension(outPath, img.IsColor ? ".ppm" : ".pgm");
                        Console.WriteLine("Starting to write to {0}...", outFilename);

                        string outDir = Path.GetDirectoryName(outFilename);
                        if (outDir != string.Empty && !Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
                        string headerString = string.Format("P{0}\n{1} {2}\n255\n", (img.IsColor ? 6 : 5), img.Width, img.Height);
                        byte[] headerBytes = Encoding.ASCII.GetBytes(headerString);

                        using (FileStream fsOut = File.Create(outPath))
                        {
                            fsOut.Write(headerBytes, 0, headerBytes.Length);

                            unsafe
                            {
                                byte* ptr = img.Image;
                                for (int i = 0; i < img.ImageSize; i++)
                                {
                                    fsOut.WriteByte(ptr[i]);
                                }
                            }
                        }

                        Console.WriteLine("Success writing {0}: {1}bytes", outFilename, headerBytes.Length + img.ImageSize);
                    }
                }
                catch (NJException njex) { Console.WriteLine("Error decoding jpeg: " + njex.ErrorCode); }
                catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); }
            }
        }
    }
}
