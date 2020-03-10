using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace OujiTenko
{
    public partial class OujiTenko : Form
    {
        public OujiTenko()
        {
            InitializeComponent();
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            // get hash data
            GetHashListFromFile();


            // check screen shot folder
            string ssPath = @".\data1";
            if (!Directory.Exists(ssPath)) return;
            // read screen shot folder
            ArrayList ssList = new ArrayList();
            foreach (string file in Directory.GetFiles(ssPath)) { ssList.Add(file); }

            // check bg image
            int bgi = ssList.IndexOf(ssPath + @"\bg.png");
            if (bgi == -1) { return; }


            // start analysis
            // read bg image
            string bgBmpPath = (string)ssList[bgi];
            Bitmap bgBmp = new Bitmap(bgBmpPath);
            bgBmp = (Bitmap)bgBmp.GetThumbnailImage(bgBmp.Width / 2, bgBmp.Height / 2, () => { return false; }, IntPtr.Zero);
            int[,,] bgRgbData = TenkoCore.GetARGBData(ref bgBmp);
            bgBmp.Dispose();
            ssList.RemoveAt(bgi);
            //ShowImage(bgRgbData, "bgRgbData");

            foreach (string pagefile in ssList)
            {
                if (Path.GetExtension(pagefile).ToLower() != ".png") continue;
                Console.WriteLine(pagefile);

                //// timer
                //int startTime = System.Environment.TickCount;
                //int lastTime = System.Environment.TickCount;
                //Console.Write(pagefile + "\t");

                // read screen shot image & init data array
                Bitmap ssImg = new Bitmap(pagefile);
                ssImg = (Bitmap)ssImg.GetThumbnailImage(ssImg.Width / 2, ssImg.Height / 2, () => { return false; }, IntPtr.Zero);
                int[,,] ssImgData = TenkoCore.GetARGBData(ref ssImg);
                //ssImg.Dispose();

                // get size data
                int width = ssImg.Width;
                int height = ssImg.Height;

                // clear alpha data
                for (int j = 0; j < height; ++j)
                {
                    for (int i = 0; i < width; ++i)
                    {
                        ssImgData[i, j, 3] = -1;
                    }
                }

                // set mask
                for (int j = 0; j < height; ++j)
                {
                    for (int i = 0; i < width; ++i)
                    {
                        for (int k = 0; k <= 1; ++k)
                        {
                            int px = (k == 0 ? i : width - 1 - i);
                            int py = j;

                            if (ssImgData[px, py, 3] != -1) continue;

                            // same color
                            if (Math.Abs(ssImgData[px, py, 0] - bgRgbData[px, py, 0]) < 15 &&
                                Math.Abs(ssImgData[px, py, 1] - bgRgbData[px, py, 1]) < 15 &&
                                Math.Abs(ssImgData[px, py, 2] - bgRgbData[px, py, 2]) < 15)
                            {
                                // outside of icon range
                                // Blocking icon
                                if ((px == 0 || px == width - 1 || py == 0) ||
                                    ssImgData[px, py - 1, 3] == 0 ||
                                    ssImgData[px - 1, py, 3] == 0 ||
                                    ssImgData[px + 1, py, 3] == 0)
                                {
                                    ssImgData[px, py, 3] = 0;
                                }
                            }
                            else
                            {
                                int iconStride = (int)(ssImgData.GetLength(1) / 5.6);
                                //int spy = (int)(py / iconStride + 0.5) * iconStride;
                                ssImgData[px, py, 3] = py * width / iconStride + px;
                            }
                        }
                    }
                }

                // Blocking mask
                for (int j = height - 1; j > 0; --j)
                {
                    for (int i = 0; i < width; ++i)
                    {
                        for (int k = 0; k <= 1; ++k)
                        {
                            int px = (k == 0 ? i : width - 1 - i);
                            int py = j;

                            if (ssImgData[px, py, 3] == 0) continue;
                            // maskid ==-1 or >0
                            if (px != 0000000000) ssImgData[px, py, 3] = Math.Max(ssImgData[px, py, 3], ssImgData[px - 1, py, 3]);
                            if (px != width - 01) ssImgData[px, py, 3] = Math.Max(ssImgData[px, py, 3], ssImgData[px + 1, py, 3]);
                            if (py != height - 1) ssImgData[px, py, 3] = Math.Max(ssImgData[px, py, 3], ssImgData[px, py + 1, 3]);
                        }
                    }
                }

                // get maskid list/area/size
                int maxMaskArea = 0;
                Dictionary<int, int[]> maskList = new Dictionary<int, int[]>(); // list[id] = [area, left, top, right, bottom]
                for (int j = 0; j < height; ++j)
                {
                    for (int i = 0; i < width; ++i)
                    {
                        int maskId = ssImgData[i, j, 3];
                        if (maskId > 0)
                        {
                            if (maskList.ContainsKey(maskId))
                            {
                                ++maskList[maskId][0];
                                maskList[maskId][1] = Math.Min(maskList[maskId][1], i);
                                maskList[maskId][2] = Math.Min(maskList[maskId][2], j);
                                maskList[maskId][3] = Math.Max(maskList[maskId][3], i);
                                maskList[maskId][4] = Math.Max(maskList[maskId][4], j);
                                maxMaskArea = Math.Max(maskList[maskId][0], maxMaskArea);
                            }
                            else
                            {
                                maskList.Add(maskId, new int[5] { 1, i, j, i, j });
                            }
                        }
                    }
                }
                //Console.WriteLine("maxMaskArea = " + maxMaskArea);

                // delete invalid maskid from list
                foreach (int key in maskList.Keys.ToArray())
                {
                    int area = maskList[key][0];
                    int maskWidth = maskList[key][3] - maskList[key][1];
                    int maskHeight = maskList[key][4] - maskList[key][2];
                    if (area < maxMaskArea * .9 || maskWidth < maskHeight / 10)
                    {
                        maskList.Remove(key);
                    }
                }
                // sort mask list
                maskList = maskList.OrderBy(o => o.Key).ToDictionary(o => o.Key, p => p.Value);

                // clear pixel by mask
                // get maskid list
                for (int j = 0; j < height; ++j)
                {
                    for (int i = 0; i < width; ++i)
                    {
                        int maskId = ssImgData[i, j, 3];
                        if (maskId == 0 || !maskList.ContainsKey(maskId))
                        {
                            ssImgData[i, j, 0] = 255;
                            ssImgData[i, j, 1] = 255;
                            ssImgData[i, j, 2] = 255;
                            continue;
                        }
                        //else
                        //{
                        //    ssImgData[i, j, 0] = (maskId == 11603 ? 255 : 0);
                        //    ssImgData[i, j, 1] = (maskId == 11792 ? 255 : 0);
                        //    ssImgData[i, j, 2] = (maskId == 11981 ? 255 : 0);
                        //}
                    }
                }
                //TenkoCore.SetRGBData(ssImgData, ref ssImg);
                //((Image)ssImg).Save(Path.GetFileNameWithoutExtension(pagefile) + "mask.png");

                ////show masklist
                //foreach (KeyValuePair<int, int[]> item in maskList)
                //{
                //    int maskId = item.Key;
                //    int area = item.Value[0];
                //    int left = item.Value[1];
                //    int top = item.Value[2];
                //    int right = item.Value[3];
                //    int bottom = item.Value[4];
                //    Console.WriteLine(maskId + "\t" + area + "\t" + left + "\t" + top + "\t" + right + "\t" + bottom);

                //    int maskWidth = right - left;
                //    int maskHeight = bottom - top;
                //    //Console.WriteLine(maskId + "\t" + area + "\t" + left + "\t" + top + "\t" + maskWidth + "\t" + maskHeight);
                //}

                // split icon img data
                foreach (KeyValuePair<int, int[]> item in maskList)
                {

                    // get mask data
                    int maskId = item.Key;
                    int area = item.Value[0];
                    int left = item.Value[1];
                    int top = item.Value[2];
                    int right = item.Value[3];
                    int bottom = item.Value[4];

                    int maskWidth = right - left;
                    int maskHeight = bottom - top;

                    //Console.WriteLine(maskId + "\t" + area + "\t" + left + "\t" + top);
                    //Console.WriteLine(maskId + "\t" + area + "\t" + left + "\t" + top + "\t" + right + "\t" + bottom);

                    // get icon color
                    int[,,] iconData = new int[maskWidth, maskHeight, 3];
                    for (int j = 0; j < maskHeight; ++j)
                    {
                        for (int i = 0; i < maskWidth; ++i)
                        {
                            iconData[i, j, 0] = ssImgData[left + i, top + j, 0];
                            iconData[i, j, 1] = ssImgData[left + i, top + j, 1];
                            iconData[i, j, 2] = ssImgData[left + i, top + j, 2];
                        }
                    }
                    TenkoCore.SetRGBData(iconData, ref ssImg);
                    //((Image)ssImg).Save(Path.GetFileNameWithoutExtension(pagefile) + "_" + maskId + ".png");
                    //Console.WriteLine(Path.GetFileNameWithoutExtension(pagefile) + "_" + maskId + ".png");

                    // get icon hash
                    string[] iconHash = TenkoCore.GetIconHash(ref ssImg);
                    //((Image)ssImg).Save(Path.GetFileNameWithoutExtension(pagefile) + "_" + maskId + "m.png");

                    ArrayList compareResult = new ArrayList();
                    foreach (string[] hashData in hashList)
                    {
                        string _id = hashData[0];
                        string _name = hashData[1];
                        string _class = hashData[2];
                        string aHash = hashData[3];
                        string dHash = hashData[4];
                        string pHash = hashData[5];
                        string _path = hashData[6];

                        int ahd = ImageComparer.HammingDistance(aHash, iconHash[0]);
                        int dhd = ImageComparer.HammingDistance(dHash, iconHash[1]);
                        int phd = ImageComparer.HammingDistance(pHash, iconHash[2]);

                        if (ahd <= 5 && dhd <= 6 && phd <= 6)
                        {
                            //compareResult.Add(new Object[] { _name, ahd, dhd, phd });
                            compareResult.Add(new Object[] { _name, ahd, dhd, phd, aHash, dHash, pHash });
                        }
                    }
                    compareResult.Sort(new ImageComparer.HammingDistanceSort());


                    //foreach (Object[] hashData in compareResult)
                    for (int i = 0; i < 10 && i < compareResult.Count; ++i)
                    {
                        Object[] hashData = (Object[])compareResult[i];

                        //Console.ForegroundColor = (((int)hashData[1] >= 5 || (int)hashData[2] >= 5 || (int)hashData[3] >= 5) ? ConsoleColor.DarkCyan : ConsoleColor.Gray);
                        Console.WriteLine(@"{0,3}/{1,3}, {2}, {3}, {4}, {5}", maskId, compareResult.Count, ((string)hashData[0]).PadRight(15, '　'), hashData[1], hashData[2], hashData[3]);

                        if (maskId == 7284)
                        {
                            //Console.WriteLine(@"{0}, {1}, {2}, {3}", maskId.ToString().PadRight(32), iconHash[0], iconHash[1], iconHash[2]);
                            //Console.WriteLine(@"{0}, {1}, {2}, {3}", ((string)hashData[0]).PadRight(16, '　'), hashData[4], hashData[5], hashData[6]);
                            continue;
                        }
                        break;
                    }
                    //if (compareResult.Count == 0)
                    //{
                    //    Console.WriteLine(@"{0,3}/{1,3}, {2}, {3}, {4}, {5}", -1, -1, "UNKNOWN".PadRight(15, '　'), -1, -1, -1);
                    //}

                    //Console.WriteLine(iconHash[0] + ", " + iconHash[1] + ", " + iconHash[2]);
                    //Console.WriteLine("");
                    //Console.ReadLine();
                }//*/









                //foreach (KeyValuePair<int, int> item in maskList)
                //{
                //    Console.WriteLine(item.Key + "\t" + item.Value);
                //}

                //Console.Write(System.Environment.TickCount - lastTime + "ms\t"); lastTime = System.Environment.TickCount;
                //Console.Write(System.Environment.TickCount - startTime + "ms\n");

                TenkoCore.SetRGBData(ssImgData, ref ssImg);
                ((Image)ssImg).Save(Path.GetFileName(pagefile));
                //ShowImage(ssImgData, Path.GetFileName(pagefile));
                ssImg.Dispose();

                //Console.WriteLine("");
                Console.ReadLine();
                //break;
            }





            return;

            {
                /*
                // read all icon image files
                string resPath = @".\Resources";
                ArrayList resList = new ArrayList();
                // checked folder
                if (!Directory.Exists(resPath)) { Directory.CreateDirectory(resPath); }
                if (!Directory.Exists(resPath + @"\bak")) { Directory.CreateDirectory(resPath + @"\bak"); }
                // get file list
                foreach (string file in Directory.GetFiles(resPath)) { resList.Add(file); }
                foreach (string resfile in resList)
                {
                    // get file string
                    string filename = Path.GetFileNameWithoutExtension(resfile);
                    string extension = Path.GetExtension(resfile);
                    string[] data = filename.Split('_');

                    if (data.Length < 3)
                    {
                        //if (resfile.IndexOf("エリザベス_王女【七つの大罪】") == -1) continue;
                        Console.WriteLine(resfile);

                        // analysisw raw icon image
                        // read icon file data
                        Bitmap iconImg = new Bitmap(resfile);
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

                        ((Image)iconImg).Save(resPath + @"\" + filename + "_" + aHash + "@" + dHash + "@" + pHash + ".png");
                        iconImg.Dispose();
                    }
                }
                // memory: 25.5mb

                // move raw file
                resList.Clear();
                foreach (string resfile in Directory.GetFiles(resPath))
                {
                    string filename = Path.GetFileNameWithoutExtension(resfile);
                    string extension = Path.GetExtension(resfile);
                    string[] data = filename.Split('_');
                    if (data.Length < 3)
                    {
                        // move files
                        File.Move(resfile, resfile.Replace(filename, @"bak\" + filename));
                    }
                    else
                    {
                        // read data
                        //Bitmap iconImg = new Bitmap(resfile);
                        //int[,,] iconData = GetRGBData(ref iconImg);
                        //iconImg.Dispose();
                        string[] hash = data[2].Split('@');

                        if (hash.Length < 3) continue;

                        // filename, data
                        //resList.Add(new Object[] { filename, iconData });
                        resList.Add(new string[] { filename, hash[0], hash[1], hash[2] });
                    }
                }
                // memory: 33.0mb
                //*/
            }


            {
                /*
            // pick icon row from page
            ArrayList iconResult = new ArrayList();
            ArrayList rowInPage = new ArrayList();
            foreach (string pagefile in ssList)
            {
                // read screen shot image & init data array
                Bitmap ssImg = new Bitmap(pagefile);
                int width = ssImg.Width;
                int height = ssImg.Height;
                int[,,] ssImgData = GetRGBData(ref ssImg);
                int[,,] maskData = new int[width, height, 3];
                //ssImg.Dispose();

                // delete same pixel, set icon [0,0,255]
                for (int i = 0; i < width; ++i)
                {
                    for (int j = 0; j < height; ++j)
                    {
                        if (Math.Abs(ssImgData[i, j, 0] - bgRgbData[i, j, 0]) < 15 &&
                            Math.Abs(ssImgData[i, j, 1] - bgRgbData[i, j, 1]) < 15 &&
                            Math.Abs(ssImgData[i, j, 2] - bgRgbData[i, j, 2]) < 15)
                        {
                            // same color set blue
                            maskData[i, j, 0] = 0; ssImgData[i, j, 0] = 0;
                            maskData[i, j, 1] = 0; ssImgData[i, j, 1] = 0;
                            maskData[i, j, 2] = 0; ssImgData[i, j, 2] = 0;
                        }
                        else
                        {
                            // icon pixel set blue
                            maskData[i, j, 0] = 0;
                            maskData[i, j, 1] = 0;
                            maskData[i, j, 2] = 255;
                        }
                    }
                }
                SetRGBData(ssImgData, ref ssImg);
                //GrayImage(ref ssImg);
                //ShowImage(ssImgData, "ssImgData");
                //ShowImage(maskData, "maskData1");

                int iconSize = height / 5;
                // find area edge, set icon range [255,0,0]/[0,255,0]
                for (int i = 0; i < width; ++i)
                {
                    int count = 0;
                    for (int j = 0; j < height; ++j)
                    { if (maskData[i, j, 2] == 255) { count++; } }
                    if (count > iconSize)
                    {
                        for (int j = 0; j < height; ++j)
                        { maskData[i, j, 0] = 255; }
                    }
                }
                //ShowImage(maskData, "maskData2");
                for (int j = 0; j < height; ++j)
                {
                    int count = 0;
                    for (int i = 0; i < width; ++i)
                    { if (maskData[i, j, 2] == 255) { count++; } }
                    if (count > iconSize)
                    {
                        for (int i = 0; i < width; ++i)
                        { maskData[i, j, 1] = 255; }
                    }
                }
                //ShowImage(maskData, "maskData3");

                // find icon center Y             
                int maxLength = 0; int length = 0; ;
                ArrayList rangeYList = new ArrayList();
                for (int i = 0, j = 0; j < height; ++j)
                {
                    // check [0, 255, 0] area
                    int c = maskData[i, j, 1];
                    if (c == 0 && length != 0)
                    {
                        // check count
                        int centerY = j - (length / 2);
                        rangeYList.Add(new int[] { centerY, length });
                        maxLength = Math.Max(maxLength, length);
                        length = 0;
                    }
                    else if (c == 255)
                    {
                        ++length;
                    }
                }
                for (int i = 0; i < rangeYList.Count; ++i)
                {
                    int[] rangeY = (int[])rangeYList[i];
                    if (rangeY[1] < maxLength * 0.95) { rangeYList.RemoveAt(i); --i; }
                }
                //foreach (int[] rangeY in rangeYList) { Console.Write(rangeY[0] + "\t"); }; Console.Write("\n");

                // find icon center X
                maxLength = 0; length = 0; ;
                ArrayList rangeXList = new ArrayList();
                for (int i = 0, j = 0; i < width; ++i)
                {
                    // check [255, 0, 0] area
                    int c = maskData[i, j, 0];
                    if (c == 0 && length != 0)
                    {
                        // check count
                        int centerX = i - (length / 2);
                        rangeXList.Add(new int[] { centerX, length });
                        maxLength = Math.Max(maxLength, length);
                        length = 0;
                    }
                    else if (c == 255)
                    {
                        ++length;
                    }
                }
                for (int i = 0; i < rangeXList.Count; ++i)
                {
                    int[] rangeX = (int[])rangeXList[i];
                    if (rangeX[1] < maxLength * 0.95) { rangeXList.RemoveAt(i); --i; }
                }
                //foreach (int[] rangeX in rangeXList) { Console.Write(rangeX[0] + "\t"); }; Console.Write("\n");

                // get distance between two row
                int strideY = 0;
                for (int j = 1; j < rangeYList.Count; ++j)
                {
                    int centerY = ((int[])rangeYList[j])[0];
                    int centerYL = ((int[])rangeYList[j - 1])[0];
                    if (centerY != -1 && centerYL != -1)
                    { strideY = centerY - centerYL; break; }
                }
                // get distance between two column
                int strideX = 0;
                for (int j = 1; j < rangeXList.Count; ++j)
                {
                    int centerX = ((int[])rangeXList[j])[0];
                    int centerXL = ((int[])rangeXList[j - 1])[0];
                    if (centerX != -1 && centerXL != -1)
                    { strideX = centerX - centerXL; break; }
                }
                //Console.WriteLine("strideY " + strideY);

                // pick row from page
                foreach (int[] rangeY in rangeYList)
                {
                    int end = rangeXList.Count - 1;
                    int srcX = ((int[])rangeXList[0])[0] - strideX / 2;
                    int srcY = rangeY[0] - strideY / 2;
                    int srcW = strideX + ((int[])rangeXList[end])[0] - ((int[])rangeXList[0])[0];
                    int srcH = strideY;
                    Bitmap rowBmp = SplitBitmap(ref ssImg, srcX, srcY, srcW, srcH);

                    ArrayList iconRange = new ArrayList();
                    foreach (int[] rangeX in rangeXList) { iconRange.Add(new int[] { rangeX[0] - srcX, strideX }); }

                    string aHash = ImageComparer.GetImageAHashCode(ref rowBmp);
                    string dHash = ImageComparer.GetImageDHashCode(ref rowBmp);
                    string pHash = ImageComparer.GetImagePHashCode(ref rowBmp);
                    Object[] row = { rowBmp, aHash, dHash, pHash, iconRange };
                    rowInPage.Add(row);
                    //Console.WriteLine("Hash: " + aHash + ", " + dHash + ", " + pHash);
                }

                // Dispose
                ssImgData = null;
                maskData = null;
                ssImg.Dispose();
                GC.Collect();
            }
            // Dispose
            bgRgbData = null;
            // memory: 325.1mb

            // merge same row image
            for (int i = 1; i < rowInPage.Count; ++i)
            {
                Object[] rowA = (Object[])rowInPage[i - 1];
                Object[] rowB = (Object[])rowInPage[i];
                string[] aHash = { (string)rowA[1], (string)rowB[1] };
                string[] dHash = { (string)rowA[2], (string)rowB[2] };
                string[] pHash = { (string)rowA[3], (string)rowB[3] };

                if (ImageComparer.HammingDistance(aHash[0], aHash[1]) < 7 &&
                    ImageComparer.HammingDistance(dHash[0], dHash[1]) < 7 &&
                    ImageComparer.HammingDistance(pHash[0], pHash[1]) < 14)
                {
                    rowInPage.RemoveAt(i);
                    --i;
                }
            }
            //for (int i = 0; i < rowInPage.Count; ++i) { Object[] row = (Object[])rowInPage[i]; ((Bitmap)row[0]).Save(@".\rowInPage_" + i + ".png"); }
            // memory: 325.1mb

            // pick icon form row
            foreach (Object[] rowObj in rowInPage)
            {
                Bitmap rowBmp = (Bitmap)rowObj[0];
                ArrayList rangeXList = (ArrayList)rowObj[4];
                int[,,] rowRgbData = GetRGBData(ref rowBmp);

                // pick icon
                foreach (int[] rangeX in rangeXList)
                {
                    // get main icon range
                    int centerX = rangeX[0];
                    int centerY = rowBmp.Height / 2;
                    int edgeL = centerX - rangeX[1] / 2;
                    int edgeR = centerX + rangeX[1] / 2;
                    int edgeT = 0;
                    int edgeB = rowBmp.Height - 1;
                    for (; edgeL < centerX; ++edgeL) { if (rowRgbData[edgeL, centerY, 0] != 0) { break; } }
                    for (; edgeR > centerX; --edgeR) { if (rowRgbData[edgeR, centerY, 0] != 0) { break; } }
                    for (; edgeT < centerY; ++edgeT) { if (rowRgbData[centerX, edgeT, 0] != 0) { break; } }
                    for (; edgeB > centerY; --edgeB) { if (rowRgbData[centerX, edgeB, 0] != 0) { break; } }

                    // pick range
                    int srcX = edgeL;
                    int srcW = edgeR - edgeL + 1;
                    int srcY = edgeT;
                    int srcH = edgeB - edgeT;

                    // split main icon
                    Bitmap iconBmp = SplitBitmap(ref rowBmp, srcX, srcY, srcW, srcH);
                    IconMask(ref iconBmp);
                    iconResult.Add(iconBmp);
                    //ShowImage(iconBmp, "iconBmp");
                }
                rowBmp.Dispose();
            }
            // memory: 355.8mb

            //// get icon hash
            //foreach (Object[] rowObj in rowInPage)
            //{
            //    Bitmap iconBmp = (Bitmap)rowObj[0];
            //    int[,,] iconRgbData = GetRGBData(ref iconBmp);

            //    // find icon main range

            //}


            /*

            // gray
            SetRGBData(ssImgData, ref ssImg);
            //GrayImage(ref ssImg);
            ssImgData = GetRGBData(ref ssImg);
            // Dispose
            maskData = null;

            // split icon
            foreach (int[] square in squareList)
            {
                int centerX = square[0];
                int centerY = square[1];

                // find icon main range
                int edgeL = 0, edgeR = 0, edgeT = 0, edgeB = 0;
                int dx = strideX / 2; int dy = strideY / 2;
                for (edgeL = centerX - dx; edgeL < centerX; ++edgeL) { if (ssImgData[edgeL, centerY, 0] != 0) { break; } }
                for (edgeR = centerX + dx; edgeR > centerX; --edgeR) { if (ssImgData[edgeR, centerY, 0] != 0) { break; } }
                for (edgeT = centerY - dy; edgeT < centerY; ++edgeT) { if (ssImgData[centerX, edgeT, 0] != 0) { break; } }
                for (edgeB = centerY + dy; edgeB > centerY; --edgeB) { if (ssImgData[centerX, edgeB, 0] != 0) { break; } }

                // split icon data
                int iconWidth = edgeR - edgeL, iconHeight = edgeB - edgeT;
                int[,,] iconData = new int[iconWidth, iconHeight, 3];
                for (int y = 0; y < iconHeight; ++y)
                {
                    for (int x = 0; x < iconWidth; ++x)
                    {
                        iconData[x, y, 0] = ssImgData[edgeL + x, edgeT + y, 0];
                        iconData[x, y, 1] = ssImgData[edgeL + x, edgeT + y, 1];
                        iconData[x, y, 2] = ssImgData[edgeL + x, edgeT + y, 2];
                    }
                }
                if (iconWidth <= 0 || iconHeight <= 0) continue;
                //ShowImage(iconData, "iconFromSS");

                // scale icon data
                Bitmap iconImg = null;
                SetRGBData(iconData, ref iconImg);
                ScaleImage(ref iconImg);
                Mask9090(ref iconImg);
                iconData = GetRGBData(ref iconImg);
                iconImg.Dispose();
                //ShowImage(iconData, "iconScaled_" + centerX + "_" + centerY);

                // compare
                ArrayList resultInIcon = new ArrayList();
                foreach (Object[] resObj in resList)
                {
                    int[,,] resData = (int[,,])resObj[1];
                    int deltaArea = GetDeltaArea(ref iconData, ref resData);
                    resData = null;
                    Object[] resultObj = { deltaArea, Path.GetFileNameWithoutExtension((string)resObj[0]) };
                    resultInIcon.Add(resultObj);
                    //Console.WriteLine(deltaArea + ", " + Path.GetFileNameWithoutExtension((string)resObj[0]));
                }
                resultInIcon.Sort(new DeltaAreaSort());
                Object[] result = (Object[])resultInIcon[0];
                // result[1] = charaName_charaClass
                resultInPage.Add(result[1]);
                //ShowImage(SetRGBData(iconData), file + " (" + iconX + ",\t" + iconY + ")");
            }
            GC.Collect();

            }
            // log all result:
            for (int i = 0; i < resultInPage.Count; ++i)
            {
                if (i % 5 == 0) Console.Write("\n");
                string[] data = ((string)resultInPage[i]).Split('_');
                Console.Write(data[0].PadLeft(16, '　'));
            }

            //*/


            }
        }

        //public class DeltaAreaSort : IComparer
        //{
        //    int IComparer.Compare(Object x, Object y) { return ((new CaseInsensitiveComparer()).Compare(((object[])y)[0], ((object[])x)[0])); }
        //}



        private static ArrayList hashList = new ArrayList();
        private static void GetHashListFromFile()
        {
            string hashFile = @".\HashList.txt";
            try
            {
                StreamReader sr = new StreamReader(hashFile, System.Text.Encoding.UTF8);
                string line;
                while ((line = sr.ReadLine()) != null && line != "")
                {
                    line = line.Trim();
                    // format data
                    string[] data = line.Split(',');
                    if (data.Length != 7 || data[0] == "") continue;

                    // put data to array
                    //Console.WriteLine(data);
                    hashList.Add(data);
                }
                sr.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }




        private static void ShowImage(int[,,] imgData, string title)
        {
            Bitmap img = null;
            TenkoCore.SetRGBData(imgData, ref img);
            ShowImage(img, title);
        }
        private static void ShowImage(Image img, string title)
        {
            Console.WriteLine("ShowImage(" + title + ")");
            ((Image)img).Save(title + ".png");

            Form bgFrom = new Form();
            if (img.Width > 400) bgFrom.ClientSize = new Size(img.Width / 5, img.Height / 5);
            else bgFrom.ClientSize = new Size(img.Width / 1, img.Height / 1);
            bgFrom.Text = title + ",\t" + img.Width + ",\t" + img.Height;
            PictureBox bgBox = new PictureBox();
            bgBox.Dock = DockStyle.Fill;
            bgBox.Image = img;
            bgBox.SizeMode = PictureBoxSizeMode.StretchImage;
            bgFrom.Controls.Add(bgBox);
            bgFrom.Show();
        }

        /*
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
        private static void GrayImage(ref Bitmap originImage)
        {
            int width = originImage.Width;
            int height = originImage.Height;
            // scale
            Bitmap cloneImg = (Bitmap)originImage.Clone();
            originImage.Dispose();
            originImage = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(originImage);
            g.Clear(Color.Transparent);
            // gray
            ColorMatrix cm = new ColorMatrix(new float[][]
            {
                new float[] { 0.30f, 0.30f, 0.30f, 0.00f, 0.00f } ,
                new float[] { 0.59f, 0.59f, 0.59f, 0.00f, 0.00f } ,
                new float[] { 0.11f, 0.11f, 0.11f, 0.00f, 0.00f } ,
                new float[] { 0.00f, 0.00f, 0.00f, 1.00f, 0.00f } ,
                new float[] { 0.00f, 0.00f, 0.00f, 0.00f, 1.00f }
            });
            ImageAttributes ia = new ImageAttributes();
            ia.SetColorMatrix(cm);

            g.DrawImage(cloneImg, new Rectangle(0, 0, width, height), 0, 0, width, height, GraphicsUnit.Pixel, ia);
            cloneImg.Dispose();
            return;
        }
        private static void ScaleImage(ref Bitmap originImage, int width = 90, int height = 90)
        {
            //int width = 90;
            //int height = 90;
            int oriwidth = originImage.Width;
            int oriheight = originImage.Height;

            Bitmap cloneImg = (Bitmap)originImage.Clone();
            // scale
            originImage.Dispose();
            originImage = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(originImage);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.Clear(Color.Transparent);
            g.DrawImage(cloneImg, new Rectangle(0, 0, width, height), 0, 0, oriwidth, oriheight, GraphicsUnit.Pixel);
            cloneImg.Dispose();
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
        private static Bitmap SplitBitmap(ref Bitmap originImage, int srcX, int srxY, int srcW, int srcH)
        {
            Bitmap outImage = new Bitmap(srcW, srcH);
            Graphics g = Graphics.FromImage(outImage);
            g.DrawImage(originImage, new Rectangle(0, 0, srcW, srcH), srcX, srxY, srcW, srcH, GraphicsUnit.Pixel);
            return outImage;
        }





        private static int GetDeltaArea(ref Bitmap imgA, ref Bitmap imgB)
        {
            int height = imgA.Height;
            int width = imgA.Width;
            //鎖住Bitmap整個影像內容
            BitmapData bitmapDataA = imgA.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData bitmapDataB = imgB.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            //取得影像資料的起始位置
            IntPtr imgPtrA = bitmapDataA.Scan0;
            IntPtr imgPtrB = bitmapDataB.Scan0;
            //影像scan的寬度
            int stride = bitmapDataA.Stride;
            //影像陣列的實際寬度
            int widthByte = width * 3;
            //所Padding的Byte數
            int skipByte = stride - widthByte;
            //設定預定存放的rgb三維陣列
            int da = 0;

            #region 讀取灰階資料
            unsafe
            {
                byte* pA = (byte*)(void*)imgPtrA;
                byte* pB = (byte*)(void*)imgPtrB;
                for (int j = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        //R Channel
                        if (Math.Abs(pA[0] - pB[0]) < 15) { ++da; }
                        pA++; pB++;
                        pA++; pB++;
                        pA++; pB++;
                    }
                    pA += skipByte;
                    pB += skipByte;
                }
            }

            //解開記憶體鎖
            imgA.UnlockBits(bitmapDataA);
            imgB.UnlockBits(bitmapDataA);

            #endregion

            return da;
        }
        private static int GetDeltaArea(ref int[,,] imgA, ref int[,,] imgB)
        {
            int width = imgA.GetLength(0);
            int height = imgA.GetLength(1);
            int da = 0;

            #region 讀取灰階資料
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    int d = (imgA[i, j, 0] - imgB[i, j, 0]);
                    if (-32 < d && d < 32) { ++da; }
                }
            }
            #endregion

            return da;
        }
        //*/



    }
}
