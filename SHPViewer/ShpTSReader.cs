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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace SHPViewer.FileFormats
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


    public class ShpTSReader
    {
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

        public ShpTSReader(string Filename, ref TSHP SHP, frmMain sender)
        {
                                              
                int FileSize;
                int x;
                int CP;
                int result;
                string hex;
                byte[] Cbyte;
                int Image_Size;
                int NextOffset;
                CP = 0;
                
                byte[] PCurrentData;
                byte CData;
                byte[] Databuffer;
                FileStream F = new FileStream(Filename, FileMode.Open, FileAccess.Read);
                BinaryReader reader = new BinaryReader(F);
  
                // Store the whole file in the memory
                FileSize = (int)F.Length;

                PCurrentData = new byte[FileSize];
                F.Read(PCurrentData, 0, (int)FileSize);
                F.Close();

                // Get Header
                SHP.Header.A = ((ushort)((ushort)(PCurrentData[CP])));
                CP += 2;
                
                Cbyte = new byte[2];
                Cbyte[1] = ((byte)(PCurrentData[CP]));
                CP += 1;
                Cbyte[0] = ((byte)(PCurrentData[CP]));
                hex = BitConverter.ToString(Cbyte);
                hex = hex.Replace("-", "");
                result = System.UInt16.Parse(hex, System.Globalization.NumberStyles.AllowHexSpecifier);
                SHP.Header.Width = (ushort)result;
                CP += 1;
                Cbyte = new byte[2];
                Cbyte[1] = ((byte)(PCurrentData[CP]));
                CP += 1;
                Cbyte[0] = ((byte)(PCurrentData[CP]));
                hex = BitConverter.ToString(Cbyte);
                hex = hex.Replace("-", "");
                result = System.UInt16.Parse(hex, System.Globalization.NumberStyles.AllowHexSpecifier);
                SHP.Header.Height = (ushort)result;
                CP += 1; 
                SHP.Header.NumImages = ((ushort)((ushort)(PCurrentData[CP])));
 
                CP += 2;                

                SHP.Data = new TSHPData[SHP.Header.NumImages + 1];

                for (x = 1; x <= SHP.Header.NumImages; x++)
                {
                    SHP.Data[x].Header_Image = new THeader_Image();
                    // Load Image Headers

                    SHP.Data[x].Header_Image.x = ((ushort)((ushort)(PCurrentData[CP])));
                    CP += 2;
                    SHP.Data[x].Header_Image.y = ((ushort)((ushort)(PCurrentData[CP])));
                    CP += 2;
                    SHP.Data[x].Header_Image.cx = ((ushort)((ushort)(PCurrentData[CP])));
                    CP += 2;
                    SHP.Data[x].Header_Image.cy = ((ushort)((ushort)(PCurrentData[CP])));
                    CP += 2;
                    SHP.Data[x].Header_Image.compression = ((byte)(PCurrentData[CP]));
                    CP += 1;
                    SHP.Data[x].Header_Image.align = new byte[3];
                    SHP.Data[x].Header_Image.align[0] = ((byte)(PCurrentData[CP]));
                    CP += 1;
                    SHP.Data[x].Header_Image.align[1] = ((byte)(PCurrentData[CP]));
                    CP += 1;
                    SHP.Data[x].Header_Image.align[2] = ((byte)(PCurrentData[CP]));
                    CP += 5;
                    SHP.Data[x].Header_Image.zero = ((int)(PCurrentData[CP]));
                    CP += 1;
                    SHP.Data[x].Header_Image.transparent = new byte[3];
                    SHP.Data[x].Header_Image.transparent[0] = ((byte)(PCurrentData[CP]));
                    CP += 1;
                    SHP.Data[x].Header_Image.transparent[1] = ((byte)(PCurrentData[CP]));
                    CP += 1;
                    SHP.Data[x].Header_Image.transparent[2] = ((byte)(PCurrentData[CP]));
                    CP += 1;

                    sender.WriteLog("R: " + Convert.ToString(SHP.Data[x].Header_Image.transparent[0]) + " G: " + Convert.ToString(SHP.Data[x].Header_Image.transparent[1]) + " B: " + Convert.ToString(SHP.Data[x].Header_Image.transparent[2]));

                    Cbyte = new byte[4];
                    Cbyte[3] = ((byte)(PCurrentData[CP]));
                    CP += 1;
                    Cbyte[2] = ((byte)(PCurrentData[CP]));
                    CP += 1;
                    Cbyte[1] = ((byte)(PCurrentData[CP]));
                    CP += 1;
                    Cbyte[0] = ((byte)(PCurrentData[CP]));
                    hex = BitConverter.ToString(Cbyte);
                    hex = hex.Replace("-", "");
                    result = System.Int32.Parse(hex, System.Globalization.NumberStyles.AllowHexSpecifier);

                    SHP.Data[x].Header_Image.offset = result;
                    CP += 1;    
                }

                // Read and decode each image from the file
                for (x = 1; x <= SHP.Header.NumImages; x++)
                {
                    // Does it really reads the frame?
                    if (SHP.Data[x].Header_Image.offset != 0)
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
                                    CData = ((byte)(PCurrentData[SHP.Data[x].Header_Image.offset+i]));
                                    Databuffer[i] = CData;
                                }
                                SHP.Data[x].Databuffer = new byte[(SHP.Data[x].Header_Image.cx * SHP.Data[x].Header_Image.cy)];
                                Decode3(Databuffer, ref SHP.Data[x].Databuffer, SHP.Data[x].Header_Image.cx, SHP.Data[x].Header_Image.cy, ref FileSize, sender);
                            
                            }
                            else
                            {
                                Image_Size = 0;
                                Image_Size = FileSize - SHP.Data[x].Header_Image.offset;
                                Databuffer = new byte[Image_Size];
                                for (int i = 0; i < Image_Size; i++)
                                {
                                    CData = ((byte)(PCurrentData[SHP.Data[x].Header_Image.offset + i]));
                                    Databuffer[i] = CData;
                                }
                                SHP.Data[x].Databuffer = new byte[((SHP.Data[x].Header_Image.cx * SHP.Data[x].Header_Image.cy))];
                                Decode3(Databuffer, ref SHP.Data[x].Databuffer, SHP.Data[x].Header_Image.cx, SHP.Data[x].Header_Image.cy, ref Image_Size, sender);
                            
                            }
                        }
                        else if ((SHP.Data[x].Header_Image.compression == 2))
                        {
                            sender.WriteLog("Decoding compression 2");
                            NextOffset = FindNextOffsetFrom(SHP, x + 1, SHP.Header.NumImages);
                            if (NextOffset != 0)
                            {
                                Image_Size = NextOffset - SHP.Data[x].Header_Image.offset;
                                SHP.Data[x].Databuffer = new byte[(SHP.Data[x].Header_Image.cx * SHP.Data[x].Header_Image.cy)];
                                Databuffer = new byte[Image_Size];
                                for (int i = 0; i < Image_Size; i++)
                                {
                                    CData = ((byte)(PCurrentData[SHP.Data[x].Header_Image.offset + i]));
                                    Databuffer[i] = CData;
                                }

                                Decode2(Databuffer, ref SHP.Data[x].Databuffer, SHP.Data[x].Header_Image.cx, SHP.Data[x].Header_Image.cy, ref Image_Size, sender);
                                // Compression 2
                            }
                            else
                            {
                                Image_Size = 0;
                                Image_Size = FileSize - SHP.Data[x].Header_Image.offset;
                                Databuffer = new byte[Image_Size];
                                for (int i = 0; i < Image_Size; i++)
                                {
                                    CData = ((byte)(PCurrentData[SHP.Data[x].Header_Image.offset + i]));
                                    Databuffer[i] = CData;
                                }
                                SHP.Data[x].Databuffer = new byte[((SHP.Data[x].Header_Image.cx * SHP.Data[x].Header_Image.cy))];
                                Decode2(Databuffer, ref SHP.Data[x].Databuffer, SHP.Data[x].Header_Image.cx, SHP.Data[x].Header_Image.cy, ref Image_Size, sender);
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
                                CData = ((byte)(PCurrentData[SHP.Data[x].Header_Image.offset + i]));
                                Databuffer[i] = CData;
                            }
                            SHP.Data[x].Databuffer = new byte[(SHP.Data[x].Header_Image.cx * SHP.Data[x].Header_Image.cy)];
                            SHP.Data[x].Databuffer = Databuffer;
                        }
                    }
                    // Set the shp's databuffer to the result after decompression
                }
            
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
        public static void Decode3(byte[] Source, ref byte[] Dest, int cx, int cy, ref int max, frmMain sender)
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


            catch (Exception ex)
            {
                sender.WriteLog("Error: " + ex.ToString());
            }

        }

        public static void Decode2(byte[] Source, ref byte[] Dest, int cx, int cy, ref int max, frmMain sender)
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
            catch (Exception ex)
            {
                sender.WriteLog("Error: " + ex.ToString());
            }
        }
   }
}
