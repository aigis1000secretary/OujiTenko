using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace OujiTenko
{
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

                if (x[3] != y[3]) return ((new CaseInsensitiveComparer()).Compare(y[3], x[3]));
                if (x[1] != y[1]) return ((new CaseInsensitiveComparer()).Compare(y[1], x[1]));
                if (x[2] != y[2]) return ((new CaseInsensitiveComparer()).Compare(y[2], x[2]));
                return 0;
            }
        }

        /// <summary>
        /// 縮放圖片
        /// </summary>
        private static Bitmap Thumb(ref Bitmap originImage, int width, int height)
        {
            return (Bitmap)originImage.GetThumbnailImage(width, height, () => { return false; }, IntPtr.Zero);
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
