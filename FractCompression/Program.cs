using System;
using System.Collections.Generic;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;
using System.Text;
using System.IO;
using System.ComponentModel;

namespace FractCompression
{
    static class Program
    {
       [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
    public class IteratingFunction
    {
        public int iRan, jRan;
        public TransformType type;
        public float q;
        public IteratingFunction(int iRan, int jRan, TransformType type, float q)
        {
            this.iRan = iRan;
            this.jRan = jRan;
            this.type = type;
            this.q = q;
        }
    }
    public enum TransformType
    {
        Deg0, Deg90, Deg180, Deg270,
        Deg0Sim, Deg90Sim, Deg180Sim, Deg270Sim
    }
    public enum ComparisionType
    {
        Cmp2x2, Cmp4x4, Cmp8x8
    }
    public class Compression
    {
        Bitmap image512, image256, image128, image64;
        float[,] br512, br256, br128, br64;
        float[,] avBrRan8x8, avBrDom8x8, avBrRan4x4, avBrDom4x4, avBrRan2x2, avBrDom2x2;
        const int RanBlockLen = 16;
        const int DomBlockLen = 8;
        const int iRanMax = 32, jRanMax = 32, iDomMax = 64, jDomMax = 64; 
        public IteratingFunction[] IterFuncSys;
        Array transfTypes;
        const float epsilon1 = 5;
        const float epsilon2 = 5;
        public bool doCmp2x2, doCmp4x4, doCmp8x8, linearCriterion;
        public BackgroundWorker backgroundWorker;
        
        public void SetImage(Bitmap im)
        {
            image512 = new Bitmap(im, 512, 512);
            image256 = new Bitmap(im, 256, 256);
            image128 = new Bitmap(im, 128, 128);
            image64 = new Bitmap(im, 64, 64);
        }
        public void PreProcess()
        {
            br512 = new float[512, 512];
            br256 = new float[256, 256];
            br128 = new float[128, 128];
            br64 = new float[64, 64];
            avBrRan8x8 = new float[iRanMax, jRanMax];
            avBrDom8x8 = new float[iDomMax, jDomMax];
            avBrRan4x4 = new float[iRanMax, jRanMax];
            avBrDom4x4 = new float[iDomMax, jDomMax];
            avBrRan2x2 = new float[iRanMax, jRanMax];
            avBrDom2x2 = new float[iDomMax, jDomMax];
            transfTypes = Enum.GetValues(typeof(TransformType));

            // Яркость
            for (int x = 0; x < image512.Width; x++)
                for (int y = 0; y < image512.Height; y++)
                    br512[x, y] = image512.GetPixel(x, y).GetBrightness() * 255;
            for (int x = 0; x < image256.Width; x++)
                for (int y = 0; y < image256.Height; y++)
                    br256[x, y] = image256.GetPixel(x, y).GetBrightness() * 255;
            for (int x = 0; x < image128.Width; x++)
                for (int y = 0; y < image128.Height; y++)
                    br128[x, y] = image128.GetPixel(x, y).GetBrightness() * 255;
            for (int x = 0; x < image64.Width; x++)
                for (int y = 0; y < image64.Height; y++)
                    br64[x, y] = image64.GetPixel(x, y).GetBrightness() * 255;

            // Средняя яркость ранговых областей
            for (int i = 0; i < iRanMax; i++)
                for (int j = 0; j < jRanMax; j++)
                {
                    float sum = 0;
                    for (int x = i * 16; x < (i + 1) * 16; x += 2)
                        for (int y = j * 16; y < (j + 1) * 16; y += 2)
                            sum += (br512[x, y] + br512[x + 1, y] +
                                br512[x, y + 1] + br512[x + 1, y + 1]) / 4.0f;
                    avBrRan8x8[i, j] = sum / 64.0f;

                    sum = 0;
                    for (int x = i * 4; x < (i + 1) * 4; x++)
                        for (int y = j * 4; y < (j + 1) * 4; y++)
                            sum += br128[x, y];
                    avBrRan4x4[i, j] = sum / 16.0f;

                    sum = 0;
                    for (int x = i * 2; x < (i + 1) * 2; x++)
                        for (int y = j * 2; y < (j + 1) * 2; y++)
                            sum += br64[x, y];
                    avBrRan2x2[i, j] = sum / 4.0f;
                }

            // Средняя яркость доменных областей
            for (int i = 0; i < iDomMax; i++)
                for (int j = 0; j < jDomMax; j++)
                {
                    float sum = 0;
                    for (int x = i * 8; x < (i + 1) * 8; x++)
                        for (int y = j * 8; y < (j + 1) * 8; y++)
                            sum += br512[x, y];
                    avBrDom8x8[i, j] = sum / 64.0f;

                    sum = 0;
                    for (int x = i * 4; x < (i + 1) * 4; x++)
                        for (int y = j * 4; y < (j + 1) * 4; y++)
                            sum += br256[x, y];
                    avBrDom4x4[i, j] = sum / 16.0f;

                    sum = 0;
                    for (int x = i * 2; x < (i + 1) * 2; x++)
                        for (int y = j * 2; y < (j + 1) * 2; y++)
                            sum += br128[x, y];
                    avBrDom2x2[i, j] = sum / 4.0f;
                }
        }
        public void Compress()
        {
            ArrayList ifs = new ArrayList();
            for(int iDom = 0; iDom < iDomMax; iDom++)
                for (int jDom = 0; jDom < jDomMax; jDom++)
                {
                    float min2x2, min4x4, min8x8, qBest = 0;
                    int iBestRan = 0, jBestRan = 0;
                    TransformType tBest;
                    min2x2 = min4x4 = min8x8 = float.MaxValue;
                    tBest = TransformType.Deg0;
                    for(int iRan = 0; iRan < iRanMax; iRan++)
                        for (int jRan = 0; jRan < jRanMax; jRan++)
                        {
                            float q8x8 = (float)Math.Round(avBrDom8x8[iDom, jDom] -
                                0.75 * avBrRan8x8[iRan, jRan]);
                            float q4x4 = (float)Math.Round(avBrDom4x4[iDom, jDom] -
                                0.75 * avBrRan4x4[iRan, jRan]);
                            float q2x2 = (float)Math.Round(avBrDom2x2[iDom, jDom] -
                                0.75 * avBrRan2x2[iRan, jRan]);
                            foreach (TransformType type in transfTypes)
                            {
                                float cur2x2, cur4x4, cur8x8;
                                cur2x2 = cur4x4 = cur8x8 = float.MaxValue;
                                if (doCmp2x2)
                                {
                                    cur2x2 = d(iDom, jDom, iRan, jRan,
                                    type, ComparisionType.Cmp2x2, q2x2);
                                    if (cur2x2 >= min2x2 + epsilon1)
                                        continue;
                                }
                                if (doCmp4x4)
                                {
                                    cur4x4 = d(iDom, jDom, iRan, jRan,
                                        type, ComparisionType.Cmp4x4, q4x4);
                                    if (cur4x4 >= min4x4 + epsilon2)
                                        continue;
                                }
                                if (doCmp8x8)
                                {
                                    cur8x8 = d(iDom, jDom, iRan, jRan,
                                        type, ComparisionType.Cmp8x8, q8x8);
                                    if (cur8x8 >= min8x8)
                                        continue;
                                }

                                iBestRan = iRan;
                                jBestRan = jRan;
                                tBest = type;
                                if (doCmp8x8) qBest = q8x8;
                                else if (doCmp4x4) qBest = q4x4;
                                else qBest = q2x2;
                                min2x2 = cur2x2;
                                min4x4 = cur4x4;
                                min8x8 = cur8x8;
                            }
                        }
                    ifs.Add(new IteratingFunction(iBestRan, jBestRan, tBest, qBest));
                    backgroundWorker.ReportProgress(iDom * 100 / iDomMax);
                    if (backgroundWorker.CancellationPending) return;
                }
            IterFuncSys = (IteratingFunction[])ifs.ToArray(typeof(IteratingFunction));
            foreach (IteratingFunction f in IterFuncSys)
            {
                if (f.q < -255) f.q = -255;
                if (f.q > 255) f.q = 255;
            }
        }
        float d(int iDom, int jDom, int iRan, int jRan,
            TransformType tType, ComparisionType cType, float q)
        {
            int xRanStart, xRanStep, yRanStart, yRanStep, len;
            float[,] brRan, brDom;
            float res = 0;
            xRanStart = xRanStep = yRanStart = yRanStep = len = 0;
            brRan = brDom = null;

            switch (cType)
            {
                case ComparisionType.Cmp2x2:
                    brRan = br64;
                    brDom = br128;
                    len = 2;
                    break;
                case ComparisionType.Cmp4x4:
                    brRan = br128;
                    brDom = br256;
                    len = 4;
                    break;
                case ComparisionType.Cmp8x8:
                    brRan = br256;
                    brDom = br512;
                    len = 8;
                    break;
            }

            switch (tType)
            {
                case TransformType.Deg0:
                    xRanStart = iRan * len;
                    xRanStep = 1;
                    yRanStart = jRan * len;
                    yRanStep = 1;
                    break;
                case TransformType.Deg90:
                    xRanStart = jRan * len + len - 1;
                    xRanStep = -1;
                    yRanStart = iRan * len;
                    yRanStep = 1;
                    break;
                case TransformType.Deg180:
                    xRanStart = iRan * len + len - 1;
                    xRanStep = -1;
                    yRanStart = jRan * len + len - 1;
                    yRanStep = -1;
                    break;
                case TransformType.Deg270:
                    xRanStart = jRan * len;
                    xRanStep = 1;
                    yRanStart = iRan * len + len - 1;
                    yRanStep = -1;
                    break;
                
                case TransformType.Deg0Sim:
                    xRanStart = iRan * len + len - 1;
                    xRanStep = -1;
                    yRanStart = jRan * len;
                    yRanStep = 1;
                    break;
                case TransformType.Deg90Sim:
                    xRanStart = jRan * len + len - 1;
                    xRanStep = -1;
                    yRanStart = iRan * len + len - 1;
                    yRanStep = -1;
                    break;
                case TransformType.Deg180Sim:
                    xRanStart = iRan * len;
                    xRanStep = 1;
                    yRanStart = jRan * len + len - 1;
                    yRanStep = -1;
                    break;
                case TransformType.Deg270Sim:
                    xRanStart = jRan * len;
                    xRanStep = 1;
                    yRanStart = iRan * len;
                    yRanStep = 1;
                    break;
            }

            int xDomMax = iDom * len + len, yDomMax = jDom * len + len;
            int xRan = xRanStart, yRan;
            for (int xDom = iDom * len; xDom < xDomMax; xDom++)
            {
                yRan = yRanStart;
                for (int yDom = jDom * len; yDom < yDomMax; yDom++)
                {
                    float delta;
                    delta = brRan[xRan, yRan] * 0.75f + q - brDom[xDom, yDom];
                    if (linearCriterion)
                        if (delta >= 0) res += delta;
                        else res -= delta;
                    else res += delta * delta;
                    yRan += yRanStep;
                }
                xRan += xRanStep;
            }
            return res;
        }
        public void WriteToFile(BinaryWriter writer)
        {
            foreach(IteratingFunction f in IterFuncSys)
            {
                uint x = 0;
                x += (uint)f.iRan;
                x = x << 5;
                x += (uint)f.jRan;
                x = x << 3;
                x += (uint)f.type;
                x = x << 9;
                x += (uint)(f.q + 255);
                x = x << 10;
                writer.Write(x);
            }            
        }
    }
    public class Decompression
    {
        float[,] br;
        float[,] tempBr;
        public int repeats;
        public IteratingFunction[] IterFuncSys;
        const int iRanMax = 32, jRanMax = 32, iDomMax = 64, jDomMax = 64; 
        public void Decompress()
        {
            br = new float[512, 512];
            tempBr = new float[512, 512];
            for (int i = 0; i < 512; i++)
                for (int j = 0; j < 512; j++)
                    br[i, j] = 255;
            for (int k = 0; k < repeats; k++)
            {
                for (int iDom = 0; iDom < iDomMax; iDom++)
                    for (int jDom = 0; jDom < jDomMax; jDom++)
                    {
                        int funcIndex = iDom * jDomMax + jDom;
                        IteratingFunction func = IterFuncSys[funcIndex];
                        DrawDomBlock(iDom, jDom, func.iRan, func.jRan, func.type, func.q);
                    }
                float[,] arr = br;
                br = tempBr;
                tempBr = arr;
            }
        }
        void DrawDomBlock(int iDom, int jDom, int iRan, int jRan,
            TransformType type, float q)
        {
            int xRanStart, xRanStep, yRanStart, yRanStep, len = 8;
            xRanStart = xRanStep = yRanStart = yRanStep = 0;
            switch (type)
            {
                case TransformType.Deg0:
                    xRanStart = iRan * len;
                    xRanStep = 1;
                    yRanStart = jRan * len;
                    yRanStep = 1;
                    break;
                case TransformType.Deg90:
                    xRanStart = jRan * len + len - 1;
                    xRanStep = -1;
                    yRanStart = iRan * len;
                    yRanStep = 1;
                    break;
                case TransformType.Deg180:
                    xRanStart = iRan * len + len - 1;
                    xRanStep = -1;
                    yRanStart = jRan * len + len - 1;
                    yRanStep = -1;
                    break;
                case TransformType.Deg270:
                    xRanStart = jRan * len;
                    xRanStep = 1;
                    yRanStart = iRan * len + len - 1;
                    yRanStep = -1;
                    break;

                case TransformType.Deg0Sim:
                    xRanStart = iRan * len + len - 1;
                    xRanStep = -1;
                    yRanStart = jRan * len;
                    yRanStep = 1;
                    break;
                case TransformType.Deg90Sim:
                    xRanStart = jRan * len + len - 1;
                    xRanStep = -1;
                    yRanStart = iRan * len + len - 1;
                    yRanStep = -1;
                    break;
                case TransformType.Deg180Sim:
                    xRanStart = iRan * len;
                    xRanStep = 1;
                    yRanStart = jRan * len + len - 1;
                    yRanStep = -1;
                    break;
                case TransformType.Deg270Sim:
                    xRanStart = jRan * len;
                    xRanStep = 1;
                    yRanStart = iRan * len;
                    yRanStep = 1;
                    break;
            }

            int xDomMax = iDom * len + len, yDomMax = jDom * len + len;
            int xRan = xRanStart * 2, yRan;
            for (int xDom = iDom * len; xDom < xDomMax; xDom++)
            {
                yRan = yRanStart * 2;
                for (int yDom = jDom * len; yDom < yDomMax; yDom++)
                {
                    float ranPixelBr = (br[xRan, yRan] + br[xRan + 1, yRan] +
                        br[xRan, yRan + 1] + br[xRan + 1, yRan + 1]) / 4.0f;
                    tempBr[xDom, yDom] = ranPixelBr * 0.75f + q;
                    yRan += 2 * yRanStep;
                }
                xRan += 2 * xRanStep;
            }
        }
        public Bitmap GetImage()
        {
            Bitmap image = new Bitmap(512, 512);
            for(int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    if (br[x, y] < 0) br[x, y] = 0;
                    if (br[x, y] > 255) br[x, y] = 255;
                    Color c = Color.FromArgb((int)br[x, y], (int)br[x, y], (int)br[x, y]);
                    image.SetPixel(x, y, c);
                }
            return image;
        }
        public void ReadFromFile(BinaryReader reader)
        {
            IterFuncSys = new IteratingFunction[4096];
            for(int i = 0; i < IterFuncSys.Length; i++)
            {
                uint x = reader.ReadUInt32();
                int iRan = (int)(x >> 27);
                int jRan = (int)(x << 5 >> 27);
                TransformType type = (TransformType)(x << 10 >> 29);
                float q = (int)(x << 13 >> 23) - 255;
                IterFuncSys[i] = new IteratingFunction(iRan, jRan, type, q);
            }
        }
    }
}