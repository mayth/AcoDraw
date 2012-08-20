using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace AcoDraw
{
    static class MainClass
    {
        static readonly System.Text.RegularExpressions.Regex sizeRegex = new System.Text.RegularExpressions.Regex("([0-9]+)x([0-9]+)");

        public static void Main(string[] args)
        {
            if (!args.Any())
            {
                ShowUsage();
                return;
            }

            #region Default Values
            // out file
            var outputFileName = args[0] + ".png";
            // cell = 10x10
            var cellSize = new Size(10, 10);
            // canvas = 25x12 cells
            var canvasSize = new Size(25, 12);
            #endregion

            #region Parse Arguments
            foreach (var optStr in args.Skip(1))
            {
                var opt = optStr.Split(new[] {':'}, 2);
                switch (opt[0])
                {
                    case "/out":
                        outputFileName = opt[1];
                        break;
                    case "/canvas":
                        try
                        {
                            canvasSize = GetSizeFromString(opt[1]);
                        }
                        catch (FormatException e)
                        {
                            Console.WriteLine("Invalid Argument (canvas). Error: " + e.Message);
                            ShowUsage();
                            return;
                        }
                        break;
                    case "/cell":
                        try
                        {
                            cellSize = GetSizeFromString(opt[1]);
                        }
                        catch (FormatException e)
                        {
                            Console.WriteLine("Invalid Argument (cell). Error: " + e.Message);
                            ShowUsage();
                            return;
                        }
                        break;
                    default:
                        Console.WriteLine("Invalid Argument: {0}", opt[0]);
                        ShowUsage();
                        return;
                }
            }
            #endregion
            
            Console.WriteLine("=== Arguments ===");
            Console.WriteLine("Input File:\t" + args[0]);
            Console.WriteLine("Output File:\t" + outputFileName);
            Console.WriteLine("Canvas Size:\t(columns)x(rows) = {0}x{1}", canvasSize.Width, canvasSize.Height);
            Console.WriteLine("Cell Size:\t(width)x(height) = {0}x{1}", cellSize.Width, cellSize.Height);
            Console.WriteLine();

            // read colors from the file.
            Console.WriteLine("Reading colors from the file...");
            var colors = new List<Color>(ReadColors(args[0]));
            Console.WriteLine(" * Read {0} color ", colors.Count);

            Console.WriteLine("Creating image...");

            // draw
            DrawAndSave(colors, cellSize, canvasSize, outputFileName, ImageFormat.Png);

            Console.WriteLine("Finish");
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage: AcoDraw [filename] /out:[filename] /canvas:[column]x[row] /cell:[width]x[height]");
        }

        static Size GetSizeFromString(string s)
        {
            var match = sizeRegex.Match(s);
            if (!match.Success)
                throw new FormatException("The given value is not matched to the format: " + s);

            int width;
            if (!int.TryParse(match.Groups[1].Value, out width))
                throw new FormatException("width cannot be parsed. Group value: " + match.Groups[1].Value);

            int height;
            if (!int.TryParse(match.Groups[2].Value, out height))
                throw new FormatException("height cannot be parsed. Group value: " + match.Groups[2].Value);

            return new Size(width, height);
        }

        static ICollection<Color> ReadColors(string path)
        {
            ushort numOfColors;
            var colors = new List<Color>();

            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                reader.ReadBytes(2);  // skip version number
                numOfColors = Utility.ToUInt16(reader.ReadBytes(2));

                foreach (var i in Enumerable.Range(0, numOfColors))
                {
                    var colorSpace = Utility.ToUInt16(reader.ReadBytes(2));
                    switch (colorSpace)
                    {
                        case 0:
                            colors.Add(ColorConverter.FromRgb(reader.ReadBytes(8)));
                            break;
                        case 1:
                            colors.Add(ColorConverter.FromHsb(reader.ReadBytes(8)));
                            break;
                        case 2:
                            colors.Add(ColorConverter.FromCmyk(reader.ReadBytes(8)));
                            break;
                        case 7:
                            colors.Add(ColorConverter.FromLab(reader.ReadBytes(8)));
                            break;
                        case 8:
                            colors.Add(ColorConverter.FromGrayscale(reader.ReadBytes(8)));
                            break;
                        case 9:
                            colors.Add(ColorConverter.FromWideCmyk(reader.ReadBytes(8)));
                            break;
                        default:
                            throw new InvalidDataException("The color space (ID: " + colorSpace + ") is not supported.");
                    }
                }
            }

            return colors;
        }

        static void DrawAndSave(IList<Color> colors, Size cellSize, Size canvasSize, string path, ImageFormat format)
        {
            using (var img = new Bitmap(cellSize.Width * canvasSize.Width, cellSize.Height * canvasSize.Height))
            using (var g = Graphics.FromImage(img))
            {
                foreach (var row in Enumerable.Range(0, canvasSize.Height))
                {
                    foreach (var col in Enumerable.Range(0, canvasSize.Width))
                    {
                        g.FillRectangle(new SolidBrush(colors[col + row * canvasSize.Width]),
                                            col * cellSize.Width, row * cellSize.Height,
                                            cellSize.Width, cellSize.Height);
                    }
                }
                img.Save(path, format);
            }
        }
    }
}
