using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using OpenRA.FileFormats;
using Gif.Components;

namespace SHPViewer
{
    public partial class frmMain : Form
    {
        public string lastpal;
        public string sver = "0.965";
        public string sname = "SHP Viewer ";
        public int fnum = 1;
        public Color remap;
        public Color remapmain;
        public string initpath;
        public Image Frame;
        public Image[] Frames;
        public Image[] FramesEx;
        public Bitmap[] BitmapEx;
        public ShpTSReader shp;
        public ShpReader shpold;
        public bool oldshp = false;
        public PaletteFormat palf = PaletteFormat.ts;
        public int[] frcx;
        public int[] frcy;
        public bool pbFrameMouseDown = false;
        public int xold = 0;
        public int frexp = 0;
        public Color remapex;
        public int bodyframes = 32;
        public int turretframes = 32;
        public int shadowframes = 32;
        public string[] backgrounds;
        public static Preferences Prefs;
        public string subdir;
        public bool bPNG = false;
        public bool bLoaded = false;
        public APNG png;

        public frmMain(string args)
        {
            InitializeComponent();
            initpath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            initpath = Path.Combine(initpath, "assets");

            DirectoryInfo directory = new DirectoryInfo(initpath);
            DirectoryInfo[] directories = directory.GetDirectories();
            int k = 0;
            foreach (DirectoryInfo subDirectory in directories)
            {
                k = k+1;
                subdir = subDirectory.Name;
                ToolStripMenuItem tsmMain = new ToolStripMenuItem(subdir);
                paletteToolStripMenuItem.DropDownItems.Add(subdir);

                foreach (ToolStripMenuItem c in paletteToolStripMenuItem.DropDownItems) //assuming this is a Form
                {
                    if (c.Text == subdir)
                    {                       
                        string[] palettes = Directory.GetFiles(Path.Combine(initpath, subdir), "*.pal");
                            foreach (string palette in palettes)
                            {

                                ToolStripMenuItem tsmPal = new ToolStripMenuItem(Path.GetFileName(palette));
                                tsmPal.Tag = palette;
                                tsmPal.Click += (_, d) =>
                                {
                                    if (bLoaded)
                                    {
                                        if (!bPNG)
                                            ApplyNewPalette(Convert.ToString(tsmPal.Tag));
                                        else
                                            ApplyNewPalettePNG(Convert.ToString(tsmPal.Tag)); 
                                    }
                                    Prefs.ShpViewer.LastPalette = Convert.ToString(tsmPal.Tag);
                                };
                                c.DropDownItems.Add(tsmPal);
                            }
                                //tsmMain.DropDownItems.Add(tsmPal);
                     }
                }
            }

            
            string strFilter = "*.jpg;*.png;*.gif;*.bmp";
            string[] m_arExt = strFilter.Split(';');
            foreach (string filter in m_arExt)
            {
                backgrounds = Directory.GetFiles(initpath, filter);
                foreach (string background in backgrounds)
                {

                    ToolStripMenuItem tsmBack = new ToolStripMenuItem(Path.GetFileName(background));
                    tsmBack.Tag = background;
                    tsmBack.Click += (_, d) =>
                    {
                        pbFrame.BackgroundImage = Image.FromFile(Convert.ToString(tsmBack.Tag));
                        Prefs.ShpViewer.BackgroundFile = Convert.ToString(tsmBack.Tag);

                    };
                    backgroundToolStripMenuItem.DropDownItems.Add(tsmBack);
                }
            }
            
            //Reading settings
            Prefs = new Preferences(Path.Combine(initpath, "prefs.yaml"));

            lastpal += Prefs.ShpViewer.LastPalette;
            
            if (lastpal == "") { lastpal = Path.Combine(initpath,"unittem.pal"); }
            //lastpal = Path.Combine(initpath, lastpal);

            if (Prefs.ShpViewer.BackgroundFile == "null")
            {
                pbFrame.BackgroundImage = null;
            }
            else
            {
                pbFrame.BackgroundImage = Image.FromFile(Prefs.ShpViewer.BackgroundFile);
            }

            pbFrame.BackgroundImageLayout = Prefs.ShpViewer.BackgroundLayout;
            transparentColorsToolStripMenuItem.Checked = Prefs.ShpViewer.TransparentColors;
            mnuRemap.Checked = Prefs.ShpViewer.RemapableColors;
            shadowToolStripMenuItem.Checked = Prefs.ShpViewer.UseShadow;
            turretToolStripMenuItem.Checked = Prefs.ShpViewer.UseTurret;
            pbColor.BackColor = Prefs.ShpViewer.RemapColor;
            cbLoop.Checked = Prefs.ShpViewer.ContinousPlaying;
            numTurretOffsetX.Value = Prefs.ShpViewer.TurretOffsetX;
            numTurretOffsetY.Value = Prefs.ShpViewer.TurretOffsetY;
            palf = Prefs.ShpViewer.PaletteFormat;
            remapmain = pbColor.BackColor;
            //Finish Reading Settings;
            
            Text = "SHP viewer " + sver;
            if (args.Length != 0)
            {
                OpenSHP(args);
            }
        }

        public void SetBackground(string image)
        {
            pbFrame.BackgroundImage = new Bitmap(image);
        }

        public void WriteLog(string Log)
        {
           txtOut.AppendText(Log+Environment.NewLine);
        }

        public Bitmap RenderShp(Palette p, int pos)
        {
            int swidth = 0;
            int sheight = 0;
            byte hk;
            byte s;
            byte l;
            
            if (oldshp == false)
                {swidth = shp.Width; sheight = shp.Height;}

            if (oldshp == true)
                {swidth = shpold.Width; sheight = shpold.Height;}

            hk = Convert.ToByte(pbColor.BackColor.GetHue()*0.7+1);
            s = Convert.ToByte(pbColor.BackColor.GetSaturation()*255);
            l = Convert.ToByte(pbColor.BackColor.GetBrightness() * 255);

            ColorRamp CRamp = new ColorRamp(hk, s, l, 25);
            if (mnuRemap.Checked)
            {
                p = new Palette(p, new PlayerColorRemap(CRamp, palf));
            }
            CRamp.GetColor(0).ToArgb();
            var bitmap = new Bitmap(swidth, sheight);
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                int* q = (int*)data.Scan0.ToPointer();
                var stride2 = data.Stride >> 2;
                for (var i = 0; i < swidth; i++)
                    for (var j = 0; j < sheight; j++)
                    {
                        if (oldshp == false)
                        { var frame = shp[pos]; q[j * stride2 + i] = p.GetColor(frame.Image[i + swidth * j]).ToArgb(); }
                        if (oldshp == true)
                        { var frame = shpold[pos]; q[j * stride2 + i] = p.GetColor(frame.Image[i + swidth * j]).ToArgb(); }
                       
                    }
            }
            bitmap.UnlockBits(data);

            return bitmap;
        }

        public Color CalculateHueChange(Color oldColor, float hue, float sat, float lum)
        {
            HLSRGB color = new HLSRGB(oldColor);
            color.Hue = hue;
            color.Saturation = sat;
            //color.Luminance = lum;
            return color.Color;
        }

        public Bitmap RenderPNG(Palette p, int pos)
        {
            Bitmap bmp = png.ToBitmap(pos);         
                        
            if (mnuRemap.Checked)
            {
                byte hk = Convert.ToByte(pbColor.BackColor.GetHue() * 0.7 + 1);
                byte s = Convert.ToByte(pbColor.BackColor.GetSaturation() * 255);
                byte l = Convert.ToByte(pbColor.BackColor.GetBrightness() * 255);
                ColorRamp CRamp = new ColorRamp(hk, s, l, 25);    

                for (int x = 0; x < bmp.Width - 1; x++)
                {
                    for (int y = 0; y < bmp.Height - 1; y++)
                    {
                        Color c = bmp.GetPixel(x, y);
                        c = Color.FromArgb(c.A, CalculateHueChange(c, CRamp.GetColor(0).GetHue(), CRamp.GetColor(0).GetSaturation(), CRamp.GetColor(0).GetBrightness()));
                        bmp.SetPixel(x, y, c);
                    }
                }
            }

            return bmp;
        }

        public void ExportFrames(string palette, string filename, ImageFormat ExportFormat)
        {
            lastpal = palette;

            if (oldshp == false)
            {
                FramesEx = new Image[shp.ImageCount + 1];
                BitmapEx = new Bitmap[shp.ImageCount + 1];
                FramesEx[0] = new Bitmap(shp.Width, shp.Height);

                for (int i = 1; i <= (int)(shp.ImageCount / 2); i++)
                {
                    FileStream P = new FileStream(palette, FileMode.Open, FileAccess.Read);
                    var p = new Palette(P, true);
                    P.Close();
                    FramesEx[i] = RenderShp(p, i - 1);
                }
                for (int i = (int)(shp.ImageCount / 2); i <= shp.ImageCount; i++)
                {
                    FileStream P = new FileStream(palette, FileMode.Open, FileAccess.Read);
                    var p = new Palette(P, transparentColorsToolStripMenuItem.Checked);
                    P.Close();
                    FramesEx[i] = RenderShp(p, i - 1);
                }
                if (shp.ImageCount > 1)
                {
                    if (shadowToolStripMenuItem.Checked)
                    {
                        if (shp.ImageCount > 1)
                        {
                            var bitmap = new Bitmap(shp.Width, shp.Height);
                            var fbitmap = new Bitmap(shp.Width, shp.Height);
                            var obitmap = new Bitmap(shp.Width, shp.Height);

                            for (int i = 1; i <= (int)(shp.ImageCount / 2); i++)
                            {
                                BitmapEx[i] = new Bitmap(shp.Width, shp.Height);
                                bitmap = (Bitmap)FramesEx[i];
                                obitmap = (Bitmap)FramesEx[i];
                                fbitmap = (Bitmap)FramesEx[i + (int)(shp.ImageCount / 2)];
                                using (var g = System.Drawing.Graphics.FromImage(fbitmap))
                                    g.DrawImage(obitmap, 0, 0);
                                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                                    g.DrawImage(fbitmap, 0, 0);
                                BitmapEx[i] = bitmap;
                            }
                            frexp = (int)(shp.ImageCount / 2);
                        }
                    }
                    if (turretToolStripMenuItem.Checked)
                    {
                        if (shp.ImageCount > 1)
                        {
                            var bitmap = new Bitmap(shp.Width, shp.Height);
                            var fbitmap = new Bitmap(shp.Width, shp.Height);
                            int limit = (int)(shp.ImageCount / 2);
                            if ((shadowToolStripMenuItem.Checked) || (palf == PaletteFormat.ts))
                            {
                                limit = (int)(limit / 2);
                            }


                            for (int i = 1; i <= limit; i++)
                            {
                                BitmapEx[i] = new Bitmap(shp.Width, shp.Height);
                                bitmap = (Bitmap)FramesEx[i];
                                fbitmap = (Bitmap)FramesEx[i + limit];

                                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                                    g.DrawImage(fbitmap, (int)numTurretOffsetX.Value, (int)numTurretOffsetY.Value);
                                BitmapEx[i] = bitmap;
                            }
                            frexp = limit;
                        }
                    }
                }
                else
                {
                    for (int i = 1; i <= shp.ImageCount; i++)
                    {
                        BitmapEx[i] = new Bitmap(shp.Width, shp.Height);
                        BitmapEx[i] = (Bitmap)FramesEx[i];
                    }
                    frexp = shp.ImageCount;
                }
                if ((turretToolStripMenuItem.Checked == false) && (shadowToolStripMenuItem.Checked == false))
                {

                    for (int i = 1; i <= shp.ImageCount; i++)
                    {
                        BitmapEx[i] = new Bitmap(shp.Width, shp.Height);
                        BitmapEx[i] = (Bitmap)FramesEx[i];
                    }
                    frexp = shp.ImageCount;
                }
            }
            if (oldshp == true)
            {
                FramesEx = new Image[shpold.ImageCount + 1];
                BitmapEx = new Bitmap[shpold.ImageCount + 1];
                FramesEx[0] = new Bitmap(shpold.Width, shpold.Height);
                for (int i = 1; i <= (int)(shpold.ImageCount / 2); i++)
                {
                    FileStream P = new FileStream(palette, FileMode.Open, FileAccess.Read);
                    var p = new Palette(P, transparentColorsToolStripMenuItem.Checked);
                    P.Close();
                    FramesEx[i] = RenderShp(p, i - 1);
                }
                for (int i = (int)(shpold.ImageCount / 2); i <= shpold.ImageCount; i++)
                {
                    FileStream P = new FileStream(palette, FileMode.Open, FileAccess.Read);
                    var p = new Palette(P, transparentColorsToolStripMenuItem.Checked);
                    P.Close();
                    FramesEx[i] = RenderShp(p, i - 1);
                }
                if (shpold.ImageCount > 1)
                {
                if (shadowToolStripMenuItem.Checked)
                {
                    if (shpold.ImageCount > 1)
                    {
                        var bitmap = new Bitmap(shpold.Width, shpold.Height);
                        var fbitmap = new Bitmap(shpold.Width, shpold.Height);
                        var obitmap = new Bitmap(shpold.Width, shpold.Height);

                        for (int i = 1; i <= (int)(shpold.ImageCount / 2); i++)
                        {
                            BitmapEx[i] = new Bitmap(shpold.Width, shpold.Height);
                            bitmap = (Bitmap)FramesEx[i];
                            obitmap = (Bitmap)FramesEx[i];
                            fbitmap = (Bitmap)FramesEx[i + (int)(shpold.ImageCount / 2)];
                            using (var g = System.Drawing.Graphics.FromImage(fbitmap))
                                g.DrawImage(obitmap, 0, 0);
                            using (var g = System.Drawing.Graphics.FromImage(bitmap))
                                g.DrawImage(fbitmap, 0, 0);
                            BitmapEx[i] = bitmap;
                        }
                        frexp = (int)(shpold.ImageCount / 2);
                    }
                }
                if (turretToolStripMenuItem.Checked)
                {
                    if (shpold.ImageCount > 1)
                    {
                        var bitmap = new Bitmap(shpold.Width, shpold.Height);
                        var fbitmap = new Bitmap(shpold.Width, shpold.Height);
                        int limit = (int)(shpold.ImageCount / 2);
                        if ((shadowToolStripMenuItem.Checked) || (palf == PaletteFormat.ts))
                        {
                            limit = (int)(limit / 2);
                        }

                        for (int i = 1; i <= limit; i++)
                        {
                            BitmapEx[i] = new Bitmap(shpold.Width, shpold.Height);
                            bitmap = (Bitmap)FramesEx[i];
                            fbitmap = (Bitmap)FramesEx[i + limit];
                            using (var g = System.Drawing.Graphics.FromImage(bitmap))
                                g.DrawImage(fbitmap, (int)numTurretOffsetX.Value, (int)numTurretOffsetY.Value);
                            BitmapEx[i] = bitmap;
                        }
                        frexp = limit;
                    }
                }
                }
                else
                {
                    for (int i = 1; i <= shpold.ImageCount; i++)
                    {
                        BitmapEx[i] = new Bitmap(shpold.Width, shpold.Height);
                        BitmapEx[i] = (Bitmap)FramesEx[i];
                    }
                    frexp = shpold.ImageCount;
                }
                if ((turretToolStripMenuItem.Checked == false) && (shadowToolStripMenuItem.Checked == false))
                {
                    for (int i = 1; i <= shpold.ImageCount; i++)
                    {
                        BitmapEx[i] = new Bitmap(shpold.Width, shpold.Height);
                        BitmapEx[i] = (Bitmap)FramesEx[i];
                    }
                    frexp = shpold.ImageCount;
                }
            }
            remapmain = pbColor.BackColor;
            if (ExportFormat == ImageFormat.Png)
            {
                string changed = Path.ChangeExtension(filename, "");
                changed = changed.Replace('.', ' ');
                for (int i = 0; i < frexp; i++)
                {
                    int num = 0;
                    if (i > 0) { num = GetIntegerDigitCount(i); }
                    string filen = "000";
                    switch (num)
                    {
                        case 1:
                            filen = "000";
                            break;
                        case 2:
                            filen = "00";
                            break;
                        case 3:
                            filen = "00";
                            break;
                        case 4:
                            filen = "";
                            break;
                        default:
                            filen = "000";
                            break;
                    }
                    BitmapEx[i + 1].Save((changed + filen + i.ToString() + ".png"), ImageFormat.Png);
                }   
            }
            if (ExportFormat == ImageFormat.Gif)
            {
                string changed = Path.ChangeExtension(filename, ".gif");
                String outputFilePath = changed;
                AnimatedGifEncoder e = new AnimatedGifEncoder();
                e.Start(outputFilePath);
                e.SetDelay(66);
                //-1:no repeat,0:always repeat
                e.SetRepeat(0);
                for (int i = 0; i < frexp; i++)
                {
                    e.AddFrame((Image)BitmapEx[i + 1]);
                }
                e.Finish();
            }
        }

        static int GetIntegerDigitCount(int valueInt)
        {
            double value = valueInt;
            int sign = 0;
            if (value < 0)
            {value = -value; sign = 1;}
            if (value <= 9)
            {return sign + 1;}
            if (value <= 99)
            {return sign + 2;}
            if (value <= 999)
            {return sign + 3;}
            if (value <= 9999)
            {return sign + 4;}
            return sign + 5;
        }        
        
        public void ApplyNewPalette(string palette)
        {
                lastpal = palette;
                
                FileStream F = new FileStream(palette, FileMode.Open, FileAccess.Read);
                var p = new Palette(F, transparentColorsToolStripMenuItem.Checked);
                F.Close();
                pbFrame.Image = null;
                Frame = null;
                fnum = tbPlay.Value;
                if (oldshp == false)
                {
                    Frame = new Bitmap(shp.Width, shp.Height);
                    
                    if (shp.ImageCount > 1)
                    {
                        if (shadowToolStripMenuItem.Checked)
                        {
                            var bitmap = new Bitmap(shp.Width, shp.Height);
                            var fbitmap = new Bitmap(shp.Width, shp.Height);
                            var obitmap = new Bitmap(shp.Width, shp.Height);
                            int limit = (int)(shp.ImageCount / 2);
                            frcx = new int[1];
                            frcy = new int[1];
                            frcx[0] = shp[fnum - 1].x;
                            frcy[0] = shp[fnum - 1].y;
                            bitmap = RenderShp(p, fnum - 1);
                            obitmap = RenderShp(p, fnum - 1);
                            fbitmap = RenderShp(p, (fnum + limit - 1));
                            using (var g = System.Drawing.Graphics.FromImage(fbitmap))
                                g.DrawImage(obitmap, 0, 0);
                            using (var g = System.Drawing.Graphics.FromImage(bitmap))
                                g.DrawImage(fbitmap, 0, 0);
                            tbPlay.Maximum = limit;
                            Frame = bitmap;
                        }
                        if (turretToolStripMenuItem.Checked)
                        {
                            var bitmap = new Bitmap(shp.Width, shp.Height);
                            var fbitmap = new Bitmap(shp.Width, shp.Height);
                            int limit = (int)(shp.ImageCount / 2);
                            if ((shadowToolStripMenuItem.Checked))
                            {
                                limit = (int)(limit / 2);
                            }
                            frcx = new int[1];
                            frcy = new int[1];
                            frcx[0] = shp[fnum - 1].x;
                            frcy[0] = shp[fnum - 1].y;
                            int tnum = 0;
                            bitmap = RenderShp(p, fnum - 1);
                            tnum = tbTurret.Value + limit;
                            fbitmap = RenderShp(p, (tnum));
                            using (var g = System.Drawing.Graphics.FromImage(bitmap))
                                g.DrawImage(fbitmap, (int)numTurretOffsetX.Value, (int)numTurretOffsetY.Value);
                            Frame = bitmap;
                            tbPlay.Maximum = limit;
                            tbTurret.Maximum = limit - 1;
                        }
                    }
                    else
                    {
                        Frame = RenderShp(p, fnum - 1);
                        frcx = new int[1];
                        frcy = new int[1];
                        frcx[0] = shp[fnum - 1].x;
                        frcy[0] = shp[fnum - 1].y;
                        tbPlay.Maximum = shp.ImageCount - 1;
                    }

                    if ((turretToolStripMenuItem.Checked == false) && (shadowToolStripMenuItem.Checked == false))
                    {
                        Frame = RenderShp(p, fnum - 1);
                        frcx = new int[1];
                        frcy = new int[1];
                        frcx[0] = shp[fnum - 1].x;
                        frcy[0] = shp[fnum - 1].y;
                        tbPlay.Maximum = shp.ImageCount - 1;
                    }
                }
                if (oldshp == true)
                {
                    Frame = new Bitmap(shpold.Width, shpold.Height);

                    if (shpold.ImageCount > 1)
                    {
                        if (shadowToolStripMenuItem.Checked)
                        {
                            var bitmap = new Bitmap(shpold.Width, shpold.Height);
                            var fbitmap = new Bitmap(shpold.Width, shpold.Height);
                            var obitmap = new Bitmap(shpold.Width, shpold.Height);
                            int limit = (int)(shpold.ImageCount / 2);
                            frcx = new int[1];
                            frcy = new int[1];
                            frcx[0] = 0;
                            frcy[0] = 0;
                            bitmap = RenderShp(p, fnum - 1);
                            obitmap = RenderShp(p, fnum - 1);
                            fbitmap = RenderShp(p, (fnum + limit - 1));
                            using (var g = System.Drawing.Graphics.FromImage(fbitmap))
                                g.DrawImage(obitmap, 0, 0);
                            using (var g = System.Drawing.Graphics.FromImage(bitmap))
                                g.DrawImage(fbitmap, 0, 0);
                            tbPlay.Maximum = limit;
                            Frame = bitmap;
                        }
                        if (turretToolStripMenuItem.Checked)
                        {
                            var bitmap = new Bitmap(shpold.Width, shpold.Height);
                            var fbitmap = new Bitmap(shpold.Width, shpold.Height);
                            int limit = (int)(shpold.ImageCount / 2);
                            if ((shadowToolStripMenuItem.Checked))
                            {
                                limit = (int)(limit / 2);
                            }
                            frcx = new int[1];
                            frcy = new int[1];
                            frcx[0] = 0;
                            frcy[0] = 0;
                            int tnum = 0;
                            bitmap = RenderShp(p, fnum - 1);
                            tnum = tbTurret.Value + limit;
                            fbitmap = RenderShp(p, (tnum));
                            using (var g = System.Drawing.Graphics.FromImage(bitmap))
                                g.DrawImage(fbitmap, (int)numTurretOffsetX.Value, (int)numTurretOffsetY.Value);
                            Frame = bitmap;
                            tbPlay.Maximum = limit;
                            tbTurret.Maximum = limit - 1;
                        }
                    }
                    else
                    {
                        Frame = RenderShp(p, fnum - 1);
                        frcx = new int[1];
                        frcy = new int[1];
                        frcx[0] = 0;
                        frcy[0] = 0;
                        tbPlay.Maximum = shpold.ImageCount - 1;
                    }

                    if ((turretToolStripMenuItem.Checked == false) && (shadowToolStripMenuItem.Checked == false))
                    {
                        Frame = RenderShp(p, fnum - 1);
                        frcx = new int[1];
                        frcy = new int[1];
                        frcx[0] = 0;
                        frcy[0] = 0;
                        tbPlay.Maximum = shpold.ImageCount - 1;
                    }
                }

                bLoaded = true;

                if (bLoaded)
                {
                    if (turretToolStripMenuItem.Checked)
                    {
                        gpTurretOffset.Enabled = true;
                    }
                    else
                    {
                        gpTurretOffset.Enabled = false;
                    }
                }
                pbFrame.Image = Frame;
                
                txtX.Text = Convert.ToString(Frame.Width);
                txtY.Text = Convert.ToString(Frame.Height);
                txtCX.Text = Convert.ToString(frcx[0]);
                txtCY.Text = Convert.ToString(frcy[0]);
        }

        public void OpenSHP(string filename)
        {
            tmPlay.Enabled = false;
            tbPlay.Enabled = false;
            cbLoop.Enabled = false;
            cbLoop.Checked = false;
            bPNG = false;
            tbPlay.Value = 1;
            tbPlay.Maximum = 1;
            fnum = 1;

            FileStream F = new FileStream(filename, FileMode.Open, FileAccess.Read);
            FileStream S = new FileStream(filename, FileMode.Open, FileAccess.Read);
            BinaryReader rdr = new BinaryReader(S);

            int ImageCount = rdr.ReadUInt16();
            rdr.Close();
            S.Close();

            if (ImageCount == 0)
            {
                shp = new ShpTSReader(F);
                oldshp = false;
            }
            else
            {
                shpold = new ShpReader(F);
                oldshp = true;
            }
            Text = sname + sver + " - " + filename;

            ApplyNewPalette(lastpal);
            
            tbPlay.Enabled = true;
            cbLoop.Enabled = true;
        }

        public void ApplyNewPalettePNG(string palette)
        {
            lastpal = palette;

            FileStream F = new FileStream(palette, FileMode.Open, FileAccess.Read);
            var p = new Palette(F, transparentColorsToolStripMenuItem.Checked);
            F.Close();

            fnum = tbPlay.Value;
            Frame = new Bitmap(png.ToBitmap(fnum).Width, png.ToBitmap(fnum).Height);				
			if (png.NumEmbeddedPNG > 1)
			{
				if (shadowToolStripMenuItem.Checked)
				{
					var bitmap = new Bitmap(png.ToBitmap(fnum).Width, png.ToBitmap(fnum).Height);
					var fbitmap = new Bitmap(png.ToBitmap(fnum).Width, png.ToBitmap(fnum).Height);
					var obitmap = new Bitmap(png.ToBitmap(fnum).Width, png.ToBitmap(fnum).Height);
					int limit = (int)(png.NumEmbeddedPNG / 2);
					frcx = new int[1];
					frcy = new int[1];
					frcx[0] = 0;
					frcy[0] = 0;
					bitmap = RenderPNG(p, fnum - 1);
                    obitmap = RenderPNG(p, fnum - 1);
					fbitmap = RenderPNG(p, (fnum + limit - 1));
					using (var g = System.Drawing.Graphics.FromImage(fbitmap))
						g.DrawImage(obitmap, 0, 0);
					using (var g = System.Drawing.Graphics.FromImage(bitmap))
						g.DrawImage(fbitmap, 0, 0);
					tbPlay.Maximum = limit;
					Frame = bitmap;
				}
				if (turretToolStripMenuItem.Checked)
				{
					var bitmap = new Bitmap(png.ToBitmap(fnum).Width, png.ToBitmap(fnum).Height);
					var fbitmap = new Bitmap(png.ToBitmap(fnum).Width, png.ToBitmap(fnum).Height);
                    int limit = (int)(png.NumEmbeddedPNG / 2);
					if ((shadowToolStripMenuItem.Checked))
					{
						limit = (int)(limit / 2);
					}
					frcx = new int[1];
					frcy = new int[1];
					frcx[0] = 0;
					frcy[0] = 0;
					int tnum = 0;
					bitmap = RenderPNG(p, fnum - 1);
					tnum = tbTurret.Value + limit;
					fbitmap = RenderPNG(p, (tnum));
					using (var g = System.Drawing.Graphics.FromImage(bitmap))
						g.DrawImage(fbitmap, (int)numTurretOffsetX.Value, (int)numTurretOffsetY.Value);
					Frame = bitmap;
					tbPlay.Maximum = limit;
					tbTurret.Maximum = limit - 1;
				}
			}
			else
			{
				Frame = RenderPNG(p, fnum - 1);
				frcx = new int[1];
				frcy = new int[1];
				frcx[0] = 0;
				frcy[0] = 0;
                tbPlay.Maximum = png.NumEmbeddedPNG - 1;
			}

			if ((turretToolStripMenuItem.Checked == false) && (shadowToolStripMenuItem.Checked == false))
			{
				Frame = RenderPNG(p, fnum - 1);
				frcx = new int[1];
				frcy = new int[1];
				frcx[0] = 0;
				frcy[0] = 0;
                tbPlay.Maximum = png.NumEmbeddedPNG - 1;
			}

            bLoaded = true;

            if (bLoaded)
            {
                if (turretToolStripMenuItem.Checked)
                {
                    gpTurretOffset.Enabled = true;
                }
                else
                {
                    gpTurretOffset.Enabled = false;
                }
            }
            pbFrame.Image = Frame;
            txtX.Text = Convert.ToString(Frame.Width);
            txtY.Text = Convert.ToString(Frame.Height);
            txtCX.Text = Convert.ToString(frcx[0]);
            txtCY.Text = Convert.ToString(frcy[0]);
        }

        public void OpenPNG(string filename)
        {
            tmPlay.Enabled = false;
            tbPlay.Enabled = false;
            cbLoop.Enabled = false;
            cbLoop.Checked = false;

            bPNG = true;

            tbPlay.Value = 1;
            tbPlay.Maximum = 1;
            fnum = 1;
            png = new APNG();
            png.Load(filename);
            
            Text = sname + sver + " - " + filename;

            ApplyNewPalettePNG(lastpal);

            tbPlay.Enabled = true;
            cbLoop.Enabled = true;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = "SHP (*.shp)|*.shp|PNG (*.png)|*.png" })
                    if (DialogResult.OK == ofd.ShowDialog())
                    {
                        if (Path.GetExtension(ofd.FileName) == ".shp")
                        {
                            OpenSHP(ofd.FileName);
                        }
                        if (Path.GetExtension(ofd.FileName) == ".png")
                        {
                            OpenPNG(ofd.FileName);
                        }
                    }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void transparentColorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Prefs.ShpViewer.TransparentColors = transparentColorsToolStripMenuItem.Checked;

            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal); 
            }
        }

        private void spPlayer_SizeChanged(object sender, EventArgs e)
        {
            if (spPlayer.Height > 45)
                spPlayer.SplitterDistance = spPlayer.Height - 44;
        }

        private void spPlayer_SplitterMoved(object sender, SplitterEventArgs e)
        {
            if (spPlayer.Height > 62)
                spPlayer.SplitterDistance = spPlayer.Height - 62;

        }

        private void tbPlay_Scroll(object sender, EventArgs e)
        {
            fnum = tbPlay.Value;
            if (!bPNG)
                ApplyNewPalette(lastpal);
            else
                ApplyNewPalettePNG(lastpal);

            //pbFrame.Image = Frames[tbPlay.Value];
        }

        private void tbPlay_ValueChanged(object sender, EventArgs e)
        {
            fnum = tbPlay.Value;
            lblFrame.Text = "Frame #: " + Convert.ToString(tbPlay.Value);
            if (!bPNG)
                ApplyNewPalette(lastpal);
            else
                ApplyNewPalettePNG(lastpal);
        }

        private void tmPlay_Tick(object sender, EventArgs e)
        {
            tbPlay.Value = fnum;
            if (fnum == tbPlay.Maximum)
            {
                fnum = 1;
            }
            else
            {
                fnum++;
            }
        }

        private void cbLoop_CheckedChanged(object sender, EventArgs e)
        {

            Prefs.ShpViewer.ContinousPlaying = cbLoop.Checked;

            
                if (cbLoop.Checked)
                {
                    if (bLoaded) { tmPlay.Enabled = true; }
                }
                else
                {
                    tmPlay.Enabled = false;
                }

        }

        private void pbColor_Click(object sender, EventArgs e)
        {
            colSel.Color = pbColor.BackColor;
            
            if (DialogResult.OK == colSel.ShowDialog())
            {
                pbColor.BackColor = colSel.Color;
                remapmain = pbColor.BackColor;
                if (bLoaded) {
                    if (!bPNG)
                        ApplyNewPalette(lastpal);
                    else
                        ApplyNewPalettePNG(lastpal); 
                }
            }
            Prefs.ShpViewer.RemapColor = pbColor.BackColor;
            
        }

        private void mnuRemap_Click(object sender, EventArgs e)
        {
            Prefs.ShpViewer.RemapableColors = mnuRemap.Checked;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
        }

        private void noneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Prefs.ShpViewer.BackgroundFile = "null";
            
            pbFrame.BackgroundImage = null;
        }

        private void tileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pbFrame.BackgroundImageLayout = ImageLayout.Tile;
            Prefs.ShpViewer.BackgroundLayout = (ImageLayout)1;
            
        }

        private void centerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pbFrame.BackgroundImageLayout = ImageLayout.Center;
            Prefs.ShpViewer.BackgroundLayout = (ImageLayout)2;
            
        }

        private void stretchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pbFrame.BackgroundImageLayout = ImageLayout.Stretch;
            Prefs.ShpViewer.BackgroundLayout = (ImageLayout)3;
            
        }

        private void zoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pbFrame.BackgroundImageLayout = ImageLayout.Zoom;
            Prefs.ShpViewer.BackgroundLayout = (ImageLayout)4;
            
        }

        private void shadowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Prefs.ShpViewer.UseShadow = shadowToolStripMenuItem.Checked;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
        }

        private void turretToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Prefs.ShpViewer.UseTurret = turretToolStripMenuItem.Checked;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal); 

                if (turretToolStripMenuItem.Checked)
                {
                    gpTurretOffset.Enabled = true;
                }
                else
                {
                    gpTurretOffset.Enabled = false;
                }
            }
        }

        private void numX_ValueChanged(object sender, EventArgs e)
        {
            Prefs.ShpViewer.TurretOffsetX = (int)numTurretOffsetX.Value;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
        }

        private void numY_ValueChanged(object sender, EventArgs e)
        {
            Prefs.ShpViewer.TurretOffsetY = (int)numTurretOffsetY.Value;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {

            Prefs.ShpViewer.TurretOffsetX = (int)numTurretOffsetX.Value;
            Prefs.ShpViewer.TurretOffsetY = (int)numTurretOffsetY.Value;
            
            Prefs.Save();
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            pbColor.BackColor = pictureBox10.BackColor;
            remapmain = pbColor.BackColor;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }

            Prefs.ShpViewer.RemapColor = pbColor.BackColor;
        }

        private void pictureBox12_Click(object sender, EventArgs e)
        {
            pbColor.BackColor = pictureBox12.BackColor;
            remapmain = pbColor.BackColor;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
            Prefs.ShpViewer.RemapColor = pbColor.BackColor;
        }

        private void pictureBox11_Click(object sender, EventArgs e)
        {
            pbColor.BackColor = pictureBox11.BackColor;
            remapmain = pbColor.BackColor;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
            Prefs.ShpViewer.RemapColor = pbColor.BackColor;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            pbColor.BackColor = pictureBox1.BackColor;
            remapmain = pbColor.BackColor;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
            Prefs.ShpViewer.RemapColor = pbColor.BackColor;
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            pbColor.BackColor = pictureBox5.BackColor;
            remapmain = pbColor.BackColor;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
            Prefs.ShpViewer.RemapColor = pbColor.BackColor;
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            pbColor.BackColor = pictureBox6.BackColor;
            remapmain = pbColor.BackColor;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
            Prefs.ShpViewer.RemapColor = pbColor.BackColor;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            pbColor.BackColor = pictureBox2.BackColor;
            remapmain = pbColor.BackColor;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
            Prefs.ShpViewer.RemapColor = pbColor.BackColor;
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {
            pbColor.BackColor = pictureBox7.BackColor;
            remapmain = pbColor.BackColor;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
            Prefs.ShpViewer.RemapColor = pbColor.BackColor;
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            pbColor.BackColor = pictureBox4.BackColor;
            remapmain = pbColor.BackColor;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
            Prefs.ShpViewer.RemapColor = pbColor.BackColor;
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            pbColor.BackColor = pictureBox3.BackColor;
            remapmain = pbColor.BackColor;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
            Prefs.ShpViewer.RemapColor = pbColor.BackColor;
        }

        private void pictureBox8_Click(object sender, EventArgs e)
        {
            pbColor.BackColor = pictureBox8.BackColor;
            remapmain = pbColor.BackColor;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
            Prefs.ShpViewer.RemapColor = pbColor.BackColor;
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            pbColor.BackColor = pictureBox9.BackColor;
            remapmain = pbColor.BackColor;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
            Prefs.ShpViewer.RemapColor = pbColor.BackColor;
        }

        private void gpTurret_Enter(object sender, EventArgs e)
        {

        }

        private void tiberianDawnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            palf = PaletteFormat.cnc;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
            Prefs.ShpViewer.PaletteFormat = palf;
        }

        private void redAlertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            palf = PaletteFormat.ra;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
            Prefs.ShpViewer.PaletteFormat = palf;
        }

        private void tiberianSunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            palf = PaletteFormat.ts;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
            Prefs.ShpViewer.PaletteFormat = palf;
        }

        private void dune2000ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            palf = PaletteFormat.d2k;
            if (bLoaded)
            {
                if (!bPNG)
                    ApplyNewPalette(lastpal);
                else
                    ApplyNewPalettePNG(lastpal);
            }
            Prefs.ShpViewer.PaletteFormat = palf;
        }

        private void framesToPNGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog { Filter = "PNG (*.png)|*.png" })
                if (DialogResult.OK == sfd.ShowDialog())
                {
                    ExportFrames(lastpal, sfd.FileName, ImageFormat.Png);
                    MessageBox.Show("Frames exported as " + Path.GetFileName(sfd.FileName));
                }
            
        }

        private void aboutToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show(sname + sver + " by KatzSmile, 2011\n" +
            "SHP(TS) format parser by Banshee, Olaf van der Spek & Stucuk\n" +
            "Remap code and SHP(TD/RA) format parser by OpenRA team\n" +
            "GIF engine by gOODiDEA.NET\n\n"+
            "E-mail: info@lead-games.com\n\n" +
            "Thanks to DerDmitry(aka Morpheuz) for help\nE-mail: dmitry@yottos.com\n\n" +
            "This tool is open source under GPLv3\n" +
            "Source code available at GIT");
        }

        private void pbFrame_Click(object sender, EventArgs e)
        {

        }

        private void openRAWebsiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.open-ra.org/");
        }

        private void leadGamesWebsiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.lead-games.com/");
        }

        private void pbFrame_MouseDown(object sender, MouseEventArgs e)
        {
            pbFrameMouseDown = true;
            xold = e.X;
        }

        private void pbFrame_MouseUp(object sender, MouseEventArgs e)
        {
            pbFrameMouseDown = false;
        }

        private void pbFrame_MouseMove(object sender, MouseEventArgs e)
        {
            if (pbFrameMouseDown)
            {                
                if (bLoaded)
                {
                    if (xold < e.X)
                    {
                        tbPlay.Value = fnum;
                        if (fnum == tbPlay.Maximum)
                        {
                            fnum = 1;
                        }
                        else
                        {
                            fnum++;
                        }
                        xold = e.X;
                    }
                    else
                    {
                        tbPlay.Value = fnum;
                        if (fnum == 1)
                        {
                            fnum = tbPlay.Maximum;
                        }
                        else
                        {
                            fnum--;
                        }
                        xold = e.X;
                    }
                }
            }
        }


        private void allFramesToGIFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog { Filter = "GIF (*.gif)|*.gif" })
                if (DialogResult.OK == sfd.ShowDialog())
                {
                    ExportFrames(lastpal, sfd.FileName, ImageFormat.Gif);
                    MessageBox.Show("Frames exported as " + Path.GetFileName(sfd.FileName));
                }
        }

        private void tbTurret_Scroll(object sender, EventArgs e)
        {
            tt.SetToolTip(tbTurret, Convert.ToString(tbTurret.Value));
            if (!bPNG)
                ApplyNewPalette(lastpal);
            else
                ApplyNewPalettePNG(lastpal);
        }

        private void captureViewportToImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog { Filter = "PNG (*.png)|*.png" })
                if (DialogResult.OK == sfd.ShowDialog())
                {
                    System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(pbFrame.Width, pbFrame.Height);
                    pbFrame.DrawToBitmap(bmp, pbFrame.ClientRectangle);

                    bmp.Save(sfd.FileName, ImageFormat.Png);
                    MessageBox.Show("Viewport saved as " + Path.GetFileName(sfd.FileName));
                }
        }

        private void asSequenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog { Filter = "PNG (*.png)|*.png" })
                if (DialogResult.OK == sfd.ShowDialog())
                {
                    tbPlay.Value = 1;
                    string changed = Path.ChangeExtension(sfd.FileName, "");
                    changed = changed.Replace('.', ' ');
                    for (int i = 0; i < tbPlay.Maximum; i++)
                    {
                        tbPlay.Value = i+1;
                        int num = 0;
                        if (i > 0) { num = GetIntegerDigitCount(i); }
                        string filen = "000";
                        switch (num)
                        {
                            case 1:
                                filen = "000";
                                break;
                            case 2:
                                filen = "00";
                                break;
                            case 3:
                                filen = "00";
                                break;
                            case 4:
                                filen = "";
                                break;
                            default:
                                filen = "000";
                                break;
                        }
                        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(pbFrame.Width, pbFrame.Height);
                        pbFrame.DrawToBitmap(bmp, pbFrame.ClientRectangle);
                        bmp.Save((changed + filen + i.ToString() + ".png"), ImageFormat.Png);                        
                    }
                    tbPlay.Value = 1;
                    MessageBox.Show("Viewport saved as " + Path.GetFileName(sfd.FileName));
                }
        }

        private void asGifToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog { Filter = "GIF (*.gif)|*.gif" })
                if (DialogResult.OK == sfd.ShowDialog())
                {
                    tbPlay.Value = 1;
                    Bitmap[] vgif;
                    vgif = new Bitmap[tbPlay.Maximum];
                    for (int i = 0; i < tbPlay.Maximum; i++)
                    {
                        tbPlay.Value = i + 1;
                        vgif[i] = new Bitmap(pbFrame.Width, pbFrame.Height);
                        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(pbFrame.Width, pbFrame.Height);
                        pbFrame.DrawToBitmap(bmp, pbFrame.ClientRectangle);
                        vgif[i] = bmp;
                    }
                    tbPlay.Value = 1;
                    String outputFilePath = sfd.FileName;
                    AnimatedGifEncoder ev = new AnimatedGifEncoder();
                    ev.Start(outputFilePath);
                    ev.SetDelay(66);
                    //-1:no repeat,0:always repeat
                    ev.SetRepeat(0);
                    for (int i = 0; i < tbPlay.Maximum; i++)
                    {
                        ev.AddFrame((Image)vgif[i]);
                    }
                    ev.Finish();
                    MessageBox.Show("Viewport saved as " + Path.GetFileName(sfd.FileName));
                }
        }

        private void projectPerfectModToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.ppmsite.com/");
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            spViewer.SplitterDistance = spViewer.Width - spViewer.Panel2MinSize - 1;
        }

        private void tbTurret_ValueChanged(object sender, EventArgs e)
        {
            if (!bPNG)
                ApplyNewPalette(lastpal);
            else
                ApplyNewPalettePNG(lastpal);
        }

    }
}
