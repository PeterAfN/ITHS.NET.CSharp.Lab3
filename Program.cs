using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ITHS.NET.Peter.Palosaari.Lab3
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            string path = VerifyFilePath(args);
            if (string.IsNullOrEmpty(path))
                return;

            var data = ReadFile(path);
            if (data == null)
                return;

            FileType fileType = DetermineFileType(data);
            PrintOutput(fileType, data);
        }

        private static void PrintOutput(FileType fileType, byte[] data = null)
        {
            switch (fileType)
            {
                case FileType.Other:
                    Console.WriteLine("This is not a valid .bmp or .png file!");
                    break;
                case FileType.Png:
                    Console.Write("This is a .png image. ");
                    PrintPngResolution(data);
                    PrintPngChunkInfo(data);
                    break;
                case FileType.Bmp:
                    Console.Write("This is a .bmp image. ");
                    PrintBmpResolution(data);
                    break;
            }
        }

        private static void PrintPngChunkInfo(byte[] data)
        {
            Console.WriteLine("Png chunk information:\n");
            Console.WriteLine("Nr\tType\tLength of whole chunk (length+type+data+crc)");
            Console.WriteLine("--\t----\t--------------------------------------------");

            int nr = 1;
            string typeHex = string.Empty;
            int startPos = 8;
            int endPos = startPos + 4;
            string lengthHex = string.Empty;
            const int chunkLength = 4;
            const int chunkType = 4;
            const int chunkCrc = 4;

            while ( Hex2Ascii(typeHex) != "IEND" )
            {
                typeHex = string.Empty;

                for (int i = startPos; i < endPos; i++)
                {
                    lengthHex += ToHex(data, i);
                }
                int length = int.Parse(lengthHex, NumberStyles.HexNumber);
                int size = chunkLength + chunkType + length + chunkCrc;

                for (int i = endPos; i < endPos+4; i++)
                {
                    typeHex += ToHex(data, i);
                }
                string typeAscii = Hex2Ascii(typeHex);

                startPos += size;
                endPos = startPos + 4;
                lengthHex = string.Empty;

                Console.WriteLine($"{nr}\t{typeAscii}\t{size} bytes");
                nr++;
            }
        }

        private static string Hex2Ascii(string hex)
        {
            string res = string.Empty;
            for (int a = 0; a <hex.Length ; a += 2)
            {
                string Char2Convert = hex.Substring(a, 2);
                int n = Convert.ToInt32(Char2Convert, 16);
                char c = (char)n;
                res += c.ToString();
            }
            return res;
        }

        private static void PrintPngResolution(byte[] data)
        {
            string wHex = string.Empty;
            for (int i = 16; i < 20; i++) //Width - IHDR chunk, byte 16 to 19
            {
                wHex += ToHex(data, i);
            }
            int width = int.Parse(wHex, NumberStyles.HexNumber);

            string hHex = string.Empty;
            for (int i = 20; i < 24; i++) //Height - IHDR chunk, byte 20 to 23
            {
                hHex += ToHex(data, i);
            }
            int height= int.Parse(hHex, NumberStyles.HexNumber);
            Console.Write($"Resolution: {width}x{height} pixels.\n\n");
        }

        private static void PrintBmpResolution(byte[] data)
        {
            string wHex = string.Empty;
            for (int i = 21; i > 17; i--) //Width - 4 bytes	at 0012h
            {
                wHex += ToHex(data, i);
            }
            int width = int.Parse(wHex, NumberStyles.HexNumber);

            string hHex = string.Empty;
            for (int i = 25; i > 21; i--) //Height - 4 bytes at 0016h
            {
                hHex += ToHex(data, i);
            }
            int height= int.Parse(hHex, NumberStyles.HexNumber);
            Console.Write($"Resolution: {width}x{height} pixels.\n");
        }

        private static string ToHex(byte[] data, int i)
        {
            return data[i].ToString("X").Equals("0") ? "00" : data[i].ToString("X");
        }

        private enum FileType
        {
            Other = 1, Png = 2, Bmp = 3
        }

        private static FileType DetermineFileType(byte[] data)
        {
            if (IsPng(data))
                return FileType.Png;
            if (IsBmp(data))
                return FileType.Bmp;
            return FileType.Other;
        }

        private static bool IsBmp(byte[] data)
        {
            var bmpSignatures = new List<byte[]>
            {
                new byte[] {66, 77}, //**BM** – Windows 3.1x, 95, NT, … etc.
                new byte[] {66, 65}, //**BA** – OS/2 struct bitmap array
                new byte[] {67, 73}, //**CI** – OS/2 struct color icon
                new byte[] {67, 80}, //**CP** – OS/2 const color pointer
                new byte[] {73, 67}, //**IC** – OS/2 struct icon
                new byte[] {80, 84}  //**PT** – OS/2 pointer
            };
            bool bmpValid = true;
            foreach (var item in bmpSignatures)
            {
                bmpValid = true;
                for (int i = 0; i < item.Length; i++)
                {
                    if (item[i] != data[i])
                        bmpValid = false;
                }
                if (bmpValid)
                    break;
            }
            return bmpValid;
        }

        private static bool IsPng(byte[] data)
        {
            byte[] pngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 }; //dec
            bool pngValid = true;
            for (int i = 0; i < pngSignature.Length; i++)
            {
                if (pngSignature[i] != data[i])
                    pngValid = false;
            }
            return pngValid;
        }

        private static byte[] ReadFile(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open);
                int fileSize = (int)fs.Length;
                var data = new byte[fileSize];
                fs.Read(data, 0, fileSize);
                fs.Close();
                return data;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected error while reading file:");
                Console.WriteLine(e.Message);
                return null;
            }
        }

        private static string VerifyFilePath(string[] args)
        {
            bool valid = false;
            string path = string.Empty;
            switch (args.Length)
            {
                case 0:
                    break;
                case 1:
                    if (File.Exists(args[0]))
                    {
                        path = args[0];
                        valid = true;
                    }
                    break;
                default:
                    path = string.Join(" ", args);
                    if (File.Exists(path))
                        valid = true;
                    break;
            }
            if (valid)
                return path;
            Console.WriteLine("File not found.");
            return null;
        }
    }
}
