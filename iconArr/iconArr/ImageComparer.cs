using System;
using System.Collections;
//using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
//using System.Linq;
//using System.Text;

namespace iconArr
{
    class TenkoCore
    {
        public static string[] GetIconHash(ref Bitmap iconImg)
        {
            //GrayImage(ref iconImg);
            int[,,] iconData = GetARGBData(ref iconImg);

            // init variable
            int width = iconData.GetLength(0);
            int height = iconData.GetLength(1);
            int cx = (int)(width * 0.25), cy = (int)(height * 0.75);

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
            IconMask(ref iconImg);
            string aHash = ImageComparer.GetImageAHashCode(ref iconImg);
            string dHash = ImageComparer.GetImageDHashCode(ref iconImg);
            string pHash = ImageComparer.GetImagePHashCode(ref iconImg);
            return new string[] { aHash, dHash, pHash };
        }

        //高效率用指標讀取影像資料
        public static int[,,] GetARGBData(ref Bitmap bitImg)
        {
            int height = bitImg.Height;
            int width = bitImg.Width;
            //檢查alpha通道
            bool isAlpha = Image.IsAlphaPixelFormat(bitImg.PixelFormat);
            //鎖住Bitmap整個影像內容
            BitmapData bitmapData = bitImg.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, (isAlpha ? PixelFormat.Format32bppRgb : PixelFormat.Format24bppRgb));
            //取得影像資料的起始位置
            IntPtr imgPtr = bitmapData.Scan0;
            //影像scan的寬度
            int stride = bitmapData.Stride;
            //影像陣列的實際寬度
            int widthByte = width * (isAlpha ? 4 : 3);
            //所Padding的Byte數
            int skipByte = stride - widthByte;
            //設定預定存放的rgb三維陣列
            int[,,] rgbData = new int[width, height, 4];

            #region 讀取RGB資料
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
                        rgbData[i, j, 2] = p[0];
                        p++;
                        //G Channel
                        rgbData[i, j, 1] = p[0];
                        p++;
                        //B Channel
                        rgbData[i, j, 0] = p[0];
                        p++;

                        //A Channel
                        if (isAlpha)
                        {
                            rgbData[i, j, 3] = p[0];
                            if (p[0] == 0)
                            {
                                rgbData[i, j, 2] = 255;
                                rgbData[i, j, 1] = 255;
                                rgbData[i, j, 0] = 255;
                            }
                            p++;
                        }
                        else
                        {
                            rgbData[i, j, 3] = 255;
                        }
                    }
                    p += skipByte;
                }
            }
            //解開記憶體鎖
            bitImg.UnlockBits(bitmapData);
            bitmapData = null;
            #endregion

            return rgbData;
        }

        //高效率圖形轉換工具--由陣列設定新的Bitmap
        public static void SetRGBData(int[,,] rgbData, ref Bitmap bitImg)
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
            bitmapData = null;
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

    class ImageComparer
    {
        /// <summary>
        /// 平均雜湊演算法
        /// </summary>
        public static string GetImageAHashCode(string imageName)
        {
            Bitmap bmp = new Bitmap(imageName);
            string hash = GetImageAHashCode(ref bmp);
            bmp.Dispose();
            return hash;
        }
        public static string GetImageAHashCode(ref Bitmap bmp)
        {
            //	第一步
            //	將圖片縮小到8x8的尺寸，總共64個畫素。這一步的作用是去除圖片的細節，
            //	只保留結構、明暗等基本資訊，摒棄不同尺寸、比例帶來的圖片差異。
            Bitmap thumb = Thumb(ref bmp, 8, 8);

            //	第二步
            //	將縮小後的圖片，轉為64級灰度。也就是說，所有畫素點總共只有64種顏色。
            int[] pixels = RGBToGray(ref thumb);

            //	第三步
            //	計算所有64個畫素的灰度平均值。
            int avgPixel = Average(pixels);

            //	第四步
            //	將每個畫素的灰度，與平均值進行比較。大於或等於平均值，記為1；小於平均值，記為0。
            int[] comps = new int[8 * 8];
            for (int i = 0; i < comps.Length; ++i)
            {
                if (pixels[i] >= avgPixel)
                {
                    comps[i] = 1;
                }
                else
                {
                    comps[i] = 0;
                }
            }

            //	第五步
            //	將上一步的比較結果，組合在一起，就構成了一個64位的整數，這就是這張圖片的指紋。組合的次序並不重要，只要保證所有圖片都採用同樣次序就行了。
            thumb.Dispose();
            return BinaryToHex(comps);
        }

        /// <summary>
        /// 差異雜湊演算法
        /// </summary>
        public static string GetImageDHashCode(string imageName)
        {
            Bitmap bmp = new Bitmap(imageName);
            string hash = GetImageDHashCode(ref bmp);
            bmp.Dispose();
            return hash;
        }
        public static string GetImageDHashCode(ref Bitmap bmp)
        {
            //	第一步
            //	將圖片縮小到8x9的尺寸，總共72個畫素
            Bitmap thumb = Thumb(ref bmp, 8, 9);

            //	第二步
            //	將縮小後的圖片，轉為64級灰度。也就是說，所有畫素點總共只有64種顏色。
            int[] pixels = RGBToGray(ref thumb);

            //	第三步
            //	算差異值，當前行畫素值-前一行畫素值，從第二到第九行共8行，又因為矩陣有8列，所以得到一個8x8差分矩陣G
            int[] difference = new int[8 * 8];
            for (int j = 0; j < 8; ++j)
            {
                for (int i = 0; i < 8; ++i)
                {
                    difference[i * 8 + j] = pixels[i * 8 + (j + 1)] - pixels[i * 8 + j];
                }
            }

            //	第四步
            //	將每個畫素的灰度，大於或等於0，記為1；小於0，記為0。
            int[] comps = new int[8 * 8];
            for (int i = 0; i < comps.Length; ++i)
            {
                if (difference[i] >= 0)
                {
                    comps[i] = 1;
                }
                else
                {
                    comps[i] = 0;
                }
            }

            //	第五步
            //	將上一步的比較結果，組合在一起，就構成了一個64位的整數，這就是這張圖片的指紋。組合的次序並不重要，只要保證所有圖片都採用同樣次序就行了。
            thumb.Dispose();
            return BinaryToHex(comps);
        }

        /// <summary>
        /// 感知雜湊演算法
        /// </summary>
        public static string GetImagePHashCode(string imageName)
        {
            Bitmap bmp = new Bitmap(imageName);
            string hash = GetImagePHashCode(ref bmp);
            bmp.Dispose();
            return hash;
        }
        public static string GetImagePHashCode(ref Bitmap bmp)
        {
            //	第一步
            //	將圖片縮小到8x8的尺寸，總共64個畫素。這一步的作用是去除圖片的細節，
            //	只保留結構、明暗等基本資訊，摒棄不同尺寸、比例帶來的圖片差異。
            Bitmap thumb = Thumb(ref bmp, 32, 32);

            //	第二步
            //	將縮小後的圖片，轉為64級灰度。也就是說，所有畫素點總共只有64種顏色。
            int[] grays = RGBToGray(ref thumb);

            //	第三步
            //	計算DCT，計算 32x32 資料矩陣的離散餘弦變換後對應的32x32資料矩陣
            //  並取出左上角 8x8 資料矩陣
            double[,] dctConst = GetDctConst();
            double dctSum = 0;
            double[,] dct = new double[8, 8];
            for (int dctY = 0; dctY < 8; ++dctY)
            {
                for (int dctX = 0; dctX < 8; ++dctX)
                {
                    double sum = 0.0;
                    for (int i = 0; i < 32; ++i)
                    {
                        for (int j = 0; j < 32; ++j)
                        {
                            sum += dctConst[i, dctY] * grays[i * 32 + j] * dctConst[j, dctX];
                        }
                    }
                    dct[dctX, dctY] = sum;
                    dctSum += sum;
                }
            }

            //	第四步
            //	計算平均值
            double avgPixel = dctSum / 64.0;

            //	第五步
            //	將每個畫素的灰度，與平均值進行比較。大於或等於平均值，記為1；小於平均值，記為0。
            int[] comps = new int[8 * 8];
            for (int j = 0; j < 8; ++j)
            {
                for (int i = 0; i < 8; ++i)
                {
                    if (dct[i, j] >= avgPixel)
                    {
                        comps[j * 8 + i] = 1;
                    }
                    else
                    {
                        comps[j * 8 + i] = 0;
                    }
                }
            }

            //	第六步
            //	將上一步的比較結果，組合在一起，就構成了一個64位的整數，這就是這張圖片的指紋。組合的次序並不重要，只要保證所有圖片都採用同樣次序就行了。
            thumb.Dispose();
            return BinaryToHex(comps);
        }

        /// <summary>
        /// 計算"漢明距離"（Hamming distance）。
        /// 如果不相同的資料位不超過5，就說明兩張圖片很相似；如果大於10，就說明這是兩張不同的圖片。
        /// </summary>
        public static int HammingDistance(String sourceHashCode, String hashCode)
        {
            int difference = 0;
            int len = sourceHashCode.Length;

            for (int i = 0; i < len; ++i)
            {
                if (sourceHashCode[i] != hashCode[i])
                {
                    ++difference;
                }
            }
            return difference;
        }

        /// <summary>
        /// 排序"漢明距離"（Hamming distance）。
        /// </summary>
        public class HammingDistanceSort : IComparer
        {
            int IComparer.Compare(Object x0, Object y0)
            {
                Object[] x = (Object[])x0;
                Object[] y = (Object[])y0;
                //resFile, aHashHD, dHashHD, pHashHD

                if (x[3] != y[3]) return ((new CaseInsensitiveComparer()).Compare(x[3], y[3]));
                if (x[1] != y[1]) return ((new CaseInsensitiveComparer()).Compare(x[1], y[1]));
                if (x[2] != y[2]) return ((new CaseInsensitiveComparer()).Compare(x[2], y[2]));
                return 0;
            }
        }

        /// <summary>
        /// 縮放圖片
        /// </summary>
        private static Bitmap Thumb(ref Bitmap originImage, int width, int height)
        {
            //return (Bitmap)originImage.GetThumbnailImage(width, height, () => { return false; }, IntPtr.Zero);

            int oriwidth = originImage.Width;
            int oriheight = originImage.Height;

            Bitmap resizedbitmap = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(resizedbitmap);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.Clear(Color.Transparent);
            g.DrawImage(originImage, new Rectangle(0, 0, width, height), new Rectangle(0, 0, oriwidth, oriheight), GraphicsUnit.Pixel);
            return resizedbitmap;
        }

        /// <summary>
        /// 轉為64級灰度
        /// </summary>
        private static int[] RGBToGray(ref Bitmap bmp)
        {
            int width = bmp.Width;
            int height = bmp.Height;
            int[] pixels = new int[width * height];
            for (int j = 0; j < height; ++j)
            {
                for (int i = 0; i < width; ++i)
                {
                    Color color = bmp.GetPixel(i, j);
                    pixels[i * height + j] = RGBToGray(color.ToArgb());
                }
            }
            return pixels;
        }
        private static int RGBToGray(int pixels)
        {
            int _red = (pixels >> 16) & 0xFF;
            int _green = (pixels >> 8) & 0xFF;
            int _blue = (pixels) & 0xFF;
            return (int)(0.3 * _red + 0.59 * _green + 0.11 * _blue);
        }

        /// <summary>
        /// 計算平均值
        /// </summary>
        private static int Average(int[] pixels)
        {
            float m = 0;
            for (int i = 0; i < pixels.Length; ++i)
            {
                m += pixels[i];
            }
            m = m / pixels.Length;
            return (int)m;
        }

        /// <summary>
        /// 取得 DCT 常數
        /// </summary>
        private static double[,] dctConst = null;
        private static double[,] GetDctConst()
        {
            if (dctConst != null) { return dctConst; }

            int n = 32;
            dctConst = new double[n, n];
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    //if (j >= 8) continue; // skip useless element
                    double a = 0.0;
                    if (i == 0) { a = Math.Sqrt(1.0 / n); }
                    else { a = Math.Sqrt(2.0 / n); }
                    dctConst[i, j] = a * Math.Cos(Math.PI * (double)i * (2.0 * (double)j + 1.0) / (2.0 * (double)n));
                }
            }

            return dctConst;
        }

        /// <summary>
        /// BinaryToHex
        /// </summary>
        private static string BinaryToHex(int[] comps)
        {
            int len = comps.Length + 4 - comps.Length % 4;
            string binary = String.Join("", comps).PadLeft(len, '0');
            string hex = "";
            while (binary.Length > 0)
            {
                hex += Convert.ToString(Convert.ToInt32(binary.Substring(0, 4), 2), 16);
                binary = binary.Remove(0, 4);
            }
            return hex;
        }
    }
}
