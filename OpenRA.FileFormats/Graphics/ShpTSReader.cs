#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;


namespace OpenRA.FileFormats
{

    public struct THeader
    {
        public ushort A;
        // Unknown
        // Width and Height of the images
        public ushort Width;
        public ushort Height;
        public ushort NumImages;
    } // end THeader

    public class THeader_Image
    {
        public ushort x;
        public ushort y;
        public ushort cx;
        public ushort cy;
        // cx and cy are width n height of stored image
        public byte compression;
        public byte[] align;
        public byte[] transparent;
        public int zero;
        public int offset;
        public byte[] Image;
    } // end THeader_Image

    public struct TSHPData
    {
        public THeader_Image Header_Image;
        public byte[] Databuffer;
        public byte[] FrameImage;
    } // end TSHPData

    public struct TSHP
    {
        public THeader Header;
        public TSHPData[] Data;        
    } // end TSHP


    public class ShpTSReader : IEnumerable<THeader_Image>
    {
        public readonly int ImageCount;
        public readonly ushort Width;
        public readonly ushort Height;
        public readonly ushort Width2;
        public readonly ushort Height2;
        public int arroff = 0;
        public int erri = 0;
        public int errj = 0;
        public int errk = 0;
        public int errl = 0;

        public static int FindNextOffsetFrom(TSHP SHP, int Init, int Last)
        {
            int result;
            result = 0;
            Last++;
            while ((result == 0) && (Init < Last))
            {
                result = SHP.Data[Init].Header_Image.offset;
                Init++;
            }
            return result;
        }
        

        private readonly List<THeader_Image> headers = new List<THeader_Image>();

        public ShpTSReader(Stream stream)
        {

            TSHP SHP = new TSHP();            
                int FileSize;
                int x;
                int k = 0;
                int l = 0;            

                int Image_Size;
                int NextOffset;

                byte[] FData;
                byte cp;
                byte[] Databuffer;

                BinaryReader sreader = new BinaryReader(stream);
                FileSize = (int)sreader.BaseStream.Length;
                // Get Header
                SHP.Header.A = sreader.ReadUInt16();
                //
                SHP.Header.Width = sreader.ReadUInt16();
                //
                SHP.Header.Height = sreader.ReadUInt16();
                //
                SHP.Header.NumImages = sreader.ReadUInt16();          

                SHP.Data = new TSHPData[SHP.Header.NumImages + 1];

                ImageCount = SHP.Header.NumImages;
               
                
                for (x = 1; x <= SHP.Header.NumImages; x++)
                {
                    SHP.Data[x].Header_Image = new THeader_Image();
 
                    SHP.Data[x].Header_Image.x = sreader.ReadUInt16();
                    SHP.Data[x].Header_Image.y = sreader.ReadUInt16();
                    SHP.Data[x].Header_Image.cx = sreader.ReadUInt16();
                    SHP.Data[x].Header_Image.cy = sreader.ReadUInt16();

                    SHP.Data[x].Header_Image.compression = sreader.ReadByte();
                    SHP.Data[x].Header_Image.align = sreader.ReadBytes(3);
                    sreader.ReadInt32();
                    SHP.Data[x].Header_Image.zero = sreader.ReadByte();
                    SHP.Data[x].Header_Image.transparent = sreader.ReadBytes(3);

                    SHP.Data[x].Header_Image.offset = sreader.ReadInt32();

                }


                Width = SHP.Header.Width;
                Height = SHP.Header.Height;

                for (int i = 0; i < ImageCount; i++)
                {                    
                    headers.Add(SHP.Data[i+1].Header_Image);
                }

                // Read and decode each image from the file
                for (x = 1; x <= SHP.Header.NumImages; x++)
                {
                    headers[x - 1].Image = new byte[(Width * Height)];
                    for (int i = 0; i < headers[x - 1].Image.Length; i++)
                        headers[x - 1].Image[i] = 0;

                    FData = new byte[(Width * Height)];

                    // Does it really reads the frame?
                    if (SHP.Data[x].Header_Image.offset != 0)
                    {
                        try
                        {
                        // Now it checks the compression:
                        if ((SHP.Data[x].Header_Image.compression == 3))
                        {
                             // decode it
                            // Compression 3
                            NextOffset = FindNextOffsetFrom(SHP, x + 1, SHP.Header.NumImages);
                            if (NextOffset != 0)
                            {
                                
                                Image_Size = NextOffset - SHP.Data[x].Header_Image.offset;
                                Databuffer = new byte[Image_Size];
                                for (int i = 0; i < Image_Size; i++)
                                {
                                    sreader.BaseStream.Position = SHP.Data[x].Header_Image.offset + i;
                                    Databuffer[i] = sreader.ReadByte();
                                }
                                SHP.Data[x].Databuffer = new byte[(SHP.Data[x].Header_Image.cx * SHP.Data[x].Header_Image.cy)];
                                Decode3(Databuffer, ref SHP.Data[x].Databuffer, SHP.Data[x].Header_Image.cx, SHP.Data[x].Header_Image.cy, ref FileSize);

                                k = 0;
                                l = 0;
                                for (int i = 0; i < Height; i++)
                                {
                                    erri = i;
                                    for (int j = SHP.Data[x].Header_Image.x; j < Width; j++)
                                    {
                                        errj = j;
                                        errl = l;
                                        errk = k;
                                        arroff = i + j + l;

                                        if (((j + 1) > (SHP.Data[x].Header_Image.cx + SHP.Data[x].Header_Image.x)) || ((i + 1) > (SHP.Data[x].Header_Image.cy)))
                                            cp = 0;
                                        else
                                            cp = SHP.Data[x].Databuffer[i + (j - SHP.Data[x].Header_Image.x) + l];

                                        FData[i + j + k] = cp;

                                        if (j == (SHP.Data[x].Header_Image.cx + SHP.Data[x].Header_Image.x - 1))
                                            l = l + (SHP.Data[x].Header_Image.cx - 1);

                                        if (j == (Width - 1))
                                            k = k + (Width - 1);
                                    } 
                                }
                                //FData = headers[x - 1].Image;
                                k = 0;
                                for (int i = 0; i < (Height - SHP.Data[x].Header_Image.y); i++)
                                {
                                    for (int j = 0; j < Width; j++)
                                    {
                                        headers[x - 1].Image[i + j + k + (Width * SHP.Data[x].Header_Image.y)] = FData[i + j + k];
                                        if (j == (Width - 1))
                                        {
                                            k = k + (Width - 1);
                                        }
                                    }
                                }

                            }
                            else
                            {
                                
                                Image_Size = 0;
                                Image_Size = FileSize - SHP.Data[x].Header_Image.offset;
                                Databuffer = new byte[Image_Size];
                                for (int i = 0; i < Image_Size; i++)
                                {
                                    sreader.BaseStream.Position = SHP.Data[x].Header_Image.offset + i;
                                    Databuffer[i] = sreader.ReadByte();
                                }
                                SHP.Data[x].Databuffer = new byte[((SHP.Data[x].Header_Image.cx * SHP.Data[x].Header_Image.cy))];
                                
                                Decode3(Databuffer, ref SHP.Data[x].Databuffer, SHP.Data[x].Header_Image.cx, SHP.Data[x].Header_Image.cy, ref Image_Size);

                                k = 0;
                                l = 0;
                                for (int i = 0; i < Height; i++)
                                {
                                    erri = i;
                                    for (int j = SHP.Data[x].Header_Image.x; j < Width; j++)
                                    {
                                        errj = j;
                                        errl = l;
                                        errk = k;
                                        arroff = i + j + l;

                                        if (((j + 1) > (SHP.Data[x].Header_Image.cx + SHP.Data[x].Header_Image.x)) || ((i + 1) > (SHP.Data[x].Header_Image.cy)))
                                            cp = 0;
                                        else
                                            cp = SHP.Data[x].Databuffer[i + (j - SHP.Data[x].Header_Image.x) + l];

                                        FData[i + j + k] = cp;

                                        if (j == (SHP.Data[x].Header_Image.cx + SHP.Data[x].Header_Image.x - 1))
                                            l = l + (SHP.Data[x].Header_Image.cx - 1);

                                        if (j == (Width - 1))
                                            k = k + (Width - 1);
                                    }
                                }
                                //FData = headers[x - 1].Image;
                                k = 0;
                                for (int i = 0; i < (Height - SHP.Data[x].Header_Image.y); i++)
                                {
                                    for (int j = 0; j < Width; j++)
                                    {
                                        headers[x - 1].Image[i + j + k + (Width * SHP.Data[x].Header_Image.y)] = FData[i + j + k];
                                        if (j == (Width - 1))
                                        {
                                            k = k + (Width - 1);
                                        }
                                    }
                                }



                            }
                        }
                        else if ((SHP.Data[x].Header_Image.compression == 2))
                        {
                            NextOffset = FindNextOffsetFrom(SHP, x + 1, SHP.Header.NumImages);
                            if (NextOffset != 0)
                            {
                                Image_Size = NextOffset - SHP.Data[x].Header_Image.offset;
                                SHP.Data[x].Databuffer = new byte[(SHP.Data[x].Header_Image.cx * SHP.Data[x].Header_Image.cy)];
                                Databuffer = new byte[Image_Size];
                                for (int i = 0; i < Image_Size; i++)
                                {
                                    sreader.BaseStream.Position = SHP.Data[x].Header_Image.offset + i;
                                    Databuffer[i] = sreader.ReadByte();
                                }

                                Decode2(Databuffer, ref SHP.Data[x].Databuffer, SHP.Data[x].Header_Image.cx, SHP.Data[x].Header_Image.cy, ref Image_Size);

                                k = 0;
                                l = 0;
                                for (int i = 0; i < Height; i++)
                                {
                                    erri = i;
                                    for (int j = SHP.Data[x].Header_Image.x; j < Width; j++)
                                    {
                                        errj = j;
                                        errl = l;
                                        errk = k;
                                        arroff = i + j + l;

                                        if (((j + 1) > (SHP.Data[x].Header_Image.cx + SHP.Data[x].Header_Image.x)) || ((i + 1) > (SHP.Data[x].Header_Image.cy)))
                                            cp = 0;
                                        else
                                            cp = SHP.Data[x].Databuffer[i + (j - SHP.Data[x].Header_Image.x) + l];

                                        FData[i + j + k] = cp;

                                        if (j == (SHP.Data[x].Header_Image.cx + SHP.Data[x].Header_Image.x - 1))
                                            l = l + (SHP.Data[x].Header_Image.cx - 1);

                                        if (j == (Width - 1))
                                            k = k + (Width - 1);
                                    }
                                }
                                //FData = headers[x - 1].Image;
                                k = 0;
                                for (int i = 0; i < (Height - SHP.Data[x].Header_Image.y); i++)
                                {
                                    for (int j = 0; j < Width; j++)
                                    {
                                        headers[x - 1].Image[i + j + k + (Width * SHP.Data[x].Header_Image.y)] = FData[i + j + k];
                                        if (j == (Width - 1))
                                        {
                                            k = k + (Width - 1);
                                        }
                                    }
                                }

                                // Compression 2
                            }
                            else
                            {
                                Image_Size = 0;
                                Image_Size = FileSize - SHP.Data[x].Header_Image.offset;
                                Databuffer = new byte[Image_Size];
                                for (int i = 0; i < Image_Size; i++)
                                {
                                    sreader.BaseStream.Position = SHP.Data[x].Header_Image.offset + i;
                                    Databuffer[i] = sreader.ReadByte();
                                }
                                SHP.Data[x].Databuffer = new byte[((SHP.Data[x].Header_Image.cx * SHP.Data[x].Header_Image.cy))];
                                Decode2(Databuffer, ref SHP.Data[x].Databuffer, SHP.Data[x].Header_Image.cx, SHP.Data[x].Header_Image.cy, ref Image_Size);

                                k = 0;
                                l = 0;
                                for (int i = 0; i < Height; i++)
                                {
                                    erri = i;
                                    for (int j = SHP.Data[x].Header_Image.x; j < Width; j++)
                                    {
                                        errj = j;
                                        errl = l;
                                        errk = k;
                                        arroff = i + j + l;

                                        if (((j + 1) > (SHP.Data[x].Header_Image.cx + SHP.Data[x].Header_Image.x)) || ((i + 1) > (SHP.Data[x].Header_Image.cy)))
                                            cp = 0;
                                        else
                                            cp = SHP.Data[x].Databuffer[i + (j - SHP.Data[x].Header_Image.x) + l];

                                        FData[i + j + k] = cp;

                                        if (j == (SHP.Data[x].Header_Image.cx + SHP.Data[x].Header_Image.x - 1))
                                            l = l + (SHP.Data[x].Header_Image.cx - 1);

                                        if (j == (Width - 1))
                                            k = k + (Width - 1);
                                    }
                                }
                                //FData = headers[x - 1].Image;
                                k = 0;
                                for (int i = 0; i < (Height - SHP.Data[x].Header_Image.y); i++)
                                {
                                    for (int j = 0; j < Width; j++)
                                    {
                                        headers[x - 1].Image[i + j + k + (Width * SHP.Data[x].Header_Image.y)] = FData[i + j + k];
                                        if (j == (Width - 1))
                                        {
                                            k = k + (Width - 1);
                                        }
                                    }
                                }

                                // Compression 2
                            }
                        }
                        else
                        {
                            // Compression 1
                            Image_Size = (int)(SHP.Data[x].Header_Image.cx * SHP.Data[x].Header_Image.cy);
                            Databuffer = new byte[Image_Size];
                            for (int i = 0; i < Image_Size; i++)
                            {
                                sreader.BaseStream.Position = SHP.Data[x].Header_Image.offset + i;
                                Databuffer[i] = sreader.ReadByte();
                            }
                            SHP.Data[x].Databuffer = new byte[(SHP.Data[x].Header_Image.cx * SHP.Data[x].Header_Image.cy)];
                            SHP.Data[x].Databuffer = Databuffer;

                            k = 0;
                            l = 0;
                            for (int i = 0; i < Height; i++)
                            {
                                erri = i;
                                for (int j = SHP.Data[x].Header_Image.x; j < Width; j++)
                                {
                                    errj = j;
                                    errl = l;
                                    errk = k;
                                    arroff = i + j + l;

                                    if (((j + 1) > (SHP.Data[x].Header_Image.cx + SHP.Data[x].Header_Image.x)) || ((i + 1) > (SHP.Data[x].Header_Image.cy)))
                                        cp = 0;
                                    else
                                        cp = SHP.Data[x].Databuffer[i + (j - SHP.Data[x].Header_Image.x) + l];

                                    FData[i + j + k] = cp;

                                    if (j == (SHP.Data[x].Header_Image.cx + SHP.Data[x].Header_Image.x - 1))
                                        l = l + (SHP.Data[x].Header_Image.cx - 1);

                                    if (j == (Width - 1))
                                        k = k + (Width - 1);
                                }
                            }
                            //FData = headers[x - 1].Image;
                            k = 0;
                            for (int i = 0; i < (Height - SHP.Data[x].Header_Image.y); i++)
                            {
                                for (int j = 0; j < Width; j++)
                                {
                                    headers[x - 1].Image[i + j + k + (Width * SHP.Data[x].Header_Image.y)] = FData[i + j + k];
                                    if (j == (Width - 1))
                                    {
                                        k = k + (Width - 1);
                                    }
                                }
                            }

                        }
                    }
                    catch (Exception)
                    {
                        //
                    }
                    }
                    // Set the shp's databuffer to the result after decompression
                }

                //Width = Width2;
                //Height = Height2;
           
        }

        public THeader_Image this[int index]
        {
            get { return headers[index]; }
        }
        
        public static void reinterpretwordfrombytes(byte Byte1, byte Byte2, ref ushort FullValue)
        {
            FullValue = (ushort)((Byte2 * 256) + Byte1);
        }

        public static void reinterpretwordfrombytes(byte Byte1, byte Byte2, ref uint FullValue)
        {
            FullValue = (uint)((Byte2 * 256) + Byte1);
        }

        // Compression 3:
        public static void Decode3(byte[] Source, ref byte[] Dest, int cx, int cy, ref int max)
        {
            int SP;
            int DP;
            int x;
            int y;
            int Count;
            int v;
            int maxdp;
            ushort Pos;
            maxdp = cx * cy;
            SP = 0;
            DP = 0;
            Pos = 0;
            try
            {

                for (y = 1; y <= cy; y++)
                {

                    reinterpretwordfrombytes(Source[SP], Source[SP + 1], ref Pos);

                    Count = Pos - 2;


                    SP = SP + 2;

                    x = 0;
                    while (Count > 0)
                    {
                        Count = Count - 1;
                        if ((SP > max) || (DP > maxdp))
                        {
                            break;
                        }
                        else
                        {
                            // SP has reached max value, exit
                            v = Source[SP];
                            SP++;
                            if (v != 0)
                            {
                                if ((SP > max) || (DP > maxdp))
                                {
                                    break;
                                }
                                else
                                {
                                    x++;
                                    Dest[DP] += (byte)v;
                                }
                                DP++;
                            }
                            else
                            {
                                Count -= 1;
                                v = Source[SP];

                                SP++;
                                if ((x + v) > cx)
                                {
                                    v = cx - x;
                                }
                                x = x + v;
                                while (v > 0)
                                {
                                    if ((SP > max) || (DP > maxdp))
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        v -= 1;
                                        Dest[DP] = 0;

                                    }
                                    DP++;
                                    // SP has reached max value, exit
                                }
                            }
                        }
                    }
                    if ((SP >= max) || (DP >= maxdp))
                    {
                        return;
                    }
                    // SP has reached max value, exit
                }
            }


            catch (Exception)
            {
             //
            }

        }

        public static void Decode2(byte[] Source, ref byte[] Dest, int cx, int cy, ref int max)
        {
            int SP;
            int DP;
            int y;
            int Count;
            int maxdp;
            ushort Pos;
            maxdp = cx * cy;
            SP = 0;
            DP = 0;
            Pos = 0;
            try
            {
                for (y = 1; y <= cy; y++)
                {
                    reinterpretwordfrombytes(Source[SP], Source[SP + 1], ref Pos);
                    Count = Pos - 2;
                    SP += 2;
                    while (Count > 0)
                    {
                        Count -= 1;
                        if ((SP > max) || (DP > maxdp))
                        {
                            return;
                        }
                        // SP has reached max value, exit
                        Dest[DP] = Source[SP];
                        SP++;
                        DP++;
                    }
                    if ((SP >= max) || (DP >= maxdp))
                    {
                        return;
                    }
                    // SP has reached max value, exit
                }
            }
            catch (Exception)
            {
                //
            }
        }

        public IEnumerator<THeader_Image> GetEnumerator()
        {
            return headers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Size Size { get { return new Size(Width, Height); } }
   }
}
