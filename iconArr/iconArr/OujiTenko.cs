using OujiTenko;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace iconArr
{
    class OujiTenko
    {
        public static string[] GetIconHash(string filePath)
        {
            //if (resfile.IndexOf("エリザベス_王女【七つの大罪】") == -1) continue;
            //Console.WriteLine(filePath);
            string extension = Path.GetExtension(filePath);

            // analysisw raw icon image
            // read icon file data
            Bitmap iconImg = new Bitmap(filePath);
            if (extension.ToLower() == ".png") PngToBmpImage(ref iconImg);
            //GrayImage(ref iconImg);
            int[,,] iconData = GetRGBData(ref iconImg);

            // init variable
            int width = iconData.GetLength(0);
            int height = iconData.GetLength(1);
            int cx = (int)(width * 0.8), cy = (int)(height * 0.5);

            // find icon main range
            int edgeT = 0, edgeB = 0, edgeL = 0, edgeR = 0;
            for (int i = 0000000000; i < cx; ++i) { edgeL = i; if (!(iconData[i, cy, 0] == 255 && iconData[i, cy, 1] == 255 && iconData[i, cy, 2] == 255)) { break; } }
            for (int i = width - 01; i > cx; --i) { edgeR = i; if (!(iconData[i, cy, 0] == 255 && iconData[i, cy, 1] == 255 && iconData[i, cy, 2] == 255)) { break; } }
            for (int j = 0000000000; j < cy; ++j) { edgeT = j; if (!(iconData[cx, j, 0] == 255 && iconData[cx, j, 1] == 255 && iconData[cx, j, 2] == 255)) { break; } }
            for (int j = height - 1; j > cy; --j) { edgeB = j; if (!(iconData[cx, j, 0] == 255 && iconData[cx, j, 1] == 255 && iconData[cx, j, 2] == 255)) { break; } }
            //Console.WriteLine(iconData[cx, 0, 0] + "\t" + iconData[cx, 0, 1] + "\t" + iconData[cx, 0, 2]);
            //Console.WriteLine(edgeT + "\t" + edgeB + "\t" + edgeL + "\t" + edgeR + "\t" + (edgeR - edgeL) + "\t" + (edgeB - edgeT) + "\t" + ((float)(edgeR - edgeL) / (float)(edgeB - edgeT)));

            // split main icon
            if (edgeT != 0 || edgeB != height - 1 || edgeL != 0 || edgeR != width - 1)
            {
                int iconWidth = edgeR - edgeL, iconHeight = edgeB - edgeT;
                int[,,] mIconData = new int[iconWidth, iconHeight, 3];
                for (int y = 0; y < iconHeight; ++y)
                {
                    for (int x = 0; x < iconWidth; ++x)
                    {
                        mIconData[x, y, 0] = iconData[edgeL + x, edgeT + y, 0];
                        mIconData[x, y, 1] = iconData[edgeL + x, edgeT + y, 1];
                        mIconData[x, y, 2] = iconData[edgeL + x, edgeT + y, 2];
                    }
                }
                iconData = mIconData;
                mIconData = null;
            }
            //ShowImage(SetRGBData(iconData), filename);

            //// 
            SetRGBData(iconData, ref iconImg);
            //ScaleImage(ref iconImg);
            IconMask(ref iconImg);
            string aHash = ImageComparer.GetImageAHashCode(ref iconImg);
            string dHash = ImageComparer.GetImageDHashCode(ref iconImg);
            string pHash = ImageComparer.GetImagePHashCode(ref iconImg);
            iconImg.Dispose();
            return new string[] { aHash, dHash, pHash };
        }

        private static void PngToBmpImage(ref Bitmap bitImg)
        {
            // get RGBA
            int height = bitImg.Height;
            int width = bitImg.Width;
            //鎖住Bitmap整個影像內容
            BitmapData bitmapData = bitImg.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            //取得影像資料的起始位置
            IntPtr imgPtr = bitmapData.Scan0;
            //影像scan的寬度
            int stride = bitmapData.Stride;
            //影像陣列的實際寬度
            int widthByte = width * 4;
            //所Padding的Byte數
            int skipByte = stride - widthByte;
            //設定預定存放的rgb三維陣列
            int[,,] rgbaData = new int[width, height, 4];

            #region 讀取RGBA資料
            //注意C#的GDI+內的影像資料順序為BGRA, 非一般熟悉的順序RGBA
            //因此我們把順序調回原來的陣列順序排放BGRA->RGBA
            unsafe
            {
                byte* p = (byte*)(void*)imgPtr;
                for (int j = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        //R Channel
                        rgbaData[i, j, 2] = p[0];
                        p++;
                        //G Channel
                        rgbaData[i, j, 1] = p[0];
                        p++;
                        //B Channel
                        rgbaData[i, j, 0] = p[0];
                        p++;
                        //A Channel
                        rgbaData[i, j, 3] = p[0];
                        p++;
                    }
                    p += skipByte;
                }
            }

            //解開記憶體鎖
            bitImg.UnlockBits(bitmapData);
            #endregion

            //// set RGB
            ////依陣列長寬設定Bitmap新的物件
            //bitImg.Dispose();
            //bitImg = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            //鎖住Bitmap整個影像內容
            bitmapData = bitImg.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            //取得影像資料的起始位置
            imgPtr = bitmapData.Scan0;
            //影像scan的寬度
            stride = bitmapData.Stride;
            //影像陣列的實際寬度
            widthByte = width * 3;
            //所Padding的Byte數
            skipByte = stride - widthByte;

            #region 設定RGB資料
            //注意C#的GDI+內的影像資料順序為BGR, 非一般熟悉的順序RGB
            //因此我們把順序調回GDI+的設定值, RGB->BGR
            unsafe
            {
                byte* p = (byte*)(void*)imgPtr;
                for (int j = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        if (rgbaData[i, j, 3] != 255)
                        {
                            //R Channel
                            p[0] = (byte)255;
                            p++;
                            //G Channel
                            p[0] = (byte)255;
                            p++;
                            //B Channel
                            p[0] = (byte)255;
                            p++;
                        }
                        else
                        {
                            //R Channel
                            p[0] = (byte)rgbaData[i, j, 2];
                            p++;
                            //G Channel
                            p[0] = (byte)rgbaData[i, j, 1];
                            p++;
                            //B Channel
                            p[0] = (byte)rgbaData[i, j, 0];
                            p++;
                        }
                    }
                    p += skipByte;
                }
            }

            //解開記憶體鎖
            bitImg.UnlockBits(bitmapData);
            rgbaData = null;
            #endregion

            return;
        }

        //高效率用指標讀取影像資料
        private static int[,,] GetRGBData(ref Bitmap bitImg)
        {
            int height = bitImg.Height;
            int width = bitImg.Width;
            //鎖住Bitmap整個影像內容
            BitmapData bitmapData = bitImg.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            //取得影像資料的起始位置
            IntPtr imgPtr = bitmapData.Scan0;
            //影像scan的寬度
            int stride = bitmapData.Stride;
            //影像陣列的實際寬度
            int widthByte = width * 3;
            //所Padding的Byte數
            int skipByte = stride - widthByte;
            //設定預定存放的rgb三維陣列
            int[,,] rgbData = new int[width, height, 3];

            #region 讀取RGB資料
            //注意C#的GDI+內的影像資料順序為BGR, 非一般熟悉的順序RGB
            //因此我們把順序調回原來的陣列順序排放BGR->RGB
            unsafe
            {
                byte* p = (byte*)(void*)imgPtr;
                for (int j = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        //R Channel
                        rgbData[i, j, 2] = p[0];
                        p++;
                        //G Channel
                        rgbData[i, j, 1] = p[0];
                        p++;
                        //B Channel
                        rgbData[i, j, 0] = p[0];
                        p++;
                    }
                    p += skipByte;
                }
            }

            //解開記憶體鎖
            bitImg.UnlockBits(bitmapData);

            #endregion

            return rgbData;
        }

        //高效率圖形轉換工具--由陣列設定新的Bitmap
        private static void SetRGBData(int[,,] rgbData, ref Bitmap bitImg)
        {
            //宣告Bitmap變數
            int width = rgbData.GetLength(0);
            int height = rgbData.GetLength(1);

            //依陣列長寬設定Bitmap新的物件
            if (bitImg != null) bitImg.Dispose();
            bitImg = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            //鎖住Bitmap整個影像內容
            BitmapData bitmapData = bitImg.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            //取得影像資料的起始位置
            IntPtr imgPtr = bitmapData.Scan0;
            //影像scan的寬度
            int stride = bitmapData.Stride;
            //影像陣列的實際寬度
            int widthByte = width * 3;
            //所Padding的Byte數
            int skipByte = stride - widthByte;

            #region 設定RGB資料
            //注意C#的GDI+內的影像資料順序為BGR, 非一般熟悉的順序RGB
            //因此我們把順序調回GDI+的設定值, RGB->BGR
            unsafe
            {
                byte* p = (byte*)(void*)imgPtr;
                for (int j = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        //R Channel
                        p[0] = (byte)rgbData[i, j, 2];
                        p++;
                        //G Channel
                        p[0] = (byte)rgbData[i, j, 1];
                        p++;
                        //B Channel
                        p[0] = (byte)rgbData[i, j, 0];
                        p++;
                    }
                    p += skipByte;
                }
            }

            //解開記憶體鎖
            bitImg.UnlockBits(bitmapData);

            #endregion

            return;
        }

        private static void IconMask(ref Bitmap originImage)
        {
            int srcW = originImage.Width;
            int srcH = originImage.Height;
            double rx = srcW / 90.0;
            double ry = srcH / 90.0;

            Rectangle[] masks = new Rectangle[3];
            {
                int x = (int)Math.Round(26 * rx);
                int y = 0;
                int w = srcW - x;
                int h = (int)Math.Round(15 * ry);
                masks[0] = new Rectangle(x, y, w, h);
            }
            {
                int x = 0;
                int y = (int)Math.Round(70 * ry);
                int w = (int)Math.Round(59 * rx);
                int h = srcH - y;
                masks[1] = new Rectangle(x, y, w, h);
            }
            {
                int x = (int)Math.Round(68 * rx);
                int y = (int)Math.Round(51 * ry);
                int w = srcW - x;
                int h = srcH - y;
                masks[2] = new Rectangle(x, y, w, h);
            }
            Graphics g = Graphics.FromImage(originImage);
            SolidBrush sb = new SolidBrush(Color.Black);
            g.FillRectangle(sb, masks[0]);
            g.FillRectangle(sb, masks[1]);
            g.FillRectangle(sb, masks[2]);
            return;
        }
        
    }
}
