﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Emgu;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;

namespace APO_Projekt
{
    /**
     * Klasa zawierająca różnego rodzaju metody do wykonywania na obrazach
     */
    public class Operations
    {

        /**************************************************************
         * Metody
         ************************************************************/

        // zwraca informację o tym czy wskazany obraz jest czarno-biały
        public static Boolean isGrayScale(Bitmap img)
        {
            Boolean result = true;
            // iteracja po wszystkich pikselach
            for (Int32 h = 0; h < img.Height; h++)
                for (Int32 w = 0; w < img.Width; w++)
                {
                    Color color = img.GetPixel(w, h);
                    // nie mają tych samych wartości i aktualnie rozpatrywana wartość nie jest czernią
                    if ((color.R != color.G || color.G != color.B || color.R != color.B) && color.A != 0)
                    {
                        result = false;
                        return result;
                    }
                }

            return result;
        }

        // neguje wartości wszystkich pikseli wskazanego obrazu
        public static void negation(Bitmap bitmap, bool isGrey)
        {
            // szary obraz
            if (isGrey)
            {
                for (Int32 h = 0; h < bitmap.Height; h++)
                    for (Int32 w = 0; w < bitmap.Width; w++)
                    {
                        Color color = bitmap.GetPixel(w, h);
                        int negRed = 255 - color.R;
                        bitmap.SetPixel(w, h, Color.FromArgb(color.A, negRed, negRed, negRed));
                    }
            }
            // kolorowy obraz
            else
            {
                for (Int32 h = 0; h < bitmap.Height; h++)
                    for (Int32 w = 0; w < bitmap.Width; w++)
                    {
                        Color color = bitmap.GetPixel(w, h);
                        int negRed = 255 - color.R;
                        int negGreen = 255 - color.G;
                        int negBlue = 255 - color.B;
                        bitmap.SetPixel(w, h, Color.FromArgb(color.A, negRed, negGreen, negBlue));
                        
                    }
            }
        }

        // liniowe rozciąganie histogramu obrazy szaroodcieniowego
        public static void greyLinearStretching(Bitmap bitmap, int[] greyLut)
        {
            // znajdź pierwsze min i ostatnie max dla których wartości są niezerowe
            byte localMin = findMin(greyLut);
            byte localMax = findMax(greyLut);

            // definicja nowego minimum i maksimum
            byte newMin = 0, newMax = 255;

            // zabezpieczenie
            if (localMax < localMin) return;

            for (Int32 h = 0; h < bitmap.Height; ++h)
                for (Int32 w = 0; w < bitmap.Width; ++w)
                {
                    Color color = bitmap.GetPixel(w, h);
                    byte newValue = calcNewLinearIntensity(color.R,localMin, localMax, newMax);
                    bitmap.SetPixel(w, h, Color.FromArgb(color.A, newValue, newValue, newValue));
                }
        }
        // operacja pomocnicza do wyliczania liniowego rozciągania histogramu
        private static byte calcNewLinearIntensity(byte current, byte localMin, byte localMax, byte newMax)
        {
            return (byte) ( ((current - localMin) * newMax) / (localMax - localMin) );
        }
        
        //*******************EQUALIZACJA******************************************

        // wyrównanie histogramu przez equalizację
        public static void greyEqualization(Bitmap bitmap, int[] greyLut)
        {
            // wylicz histogram skumulowany
            int[] cumLut = cumsum(greyLut);
            foreach (int i in cumLut) Console.WriteLine(i);

            // indeksy
            byte localMin = findMin(cumLut);
            byte localMax = findMax(cumLut);

            // zabezpieczenie
            if (localMax < localMin) return;

            // ponownie przelicz indeksy
            localMin = findMin(cumLut);
            localMax = findMax(cumLut);

            // zapisz wartości indexu min i max
            var localMinValue = cumLut[localMin];
            var localMaxValue = cumLut[localMax];

            // normalizujemy wartości w histogramie
            for (int i = 0; i < cumLut.Length; ++i)
            {
                int newValue = ((cumLut[i] - localMinValue) * 255) / (localMaxValue - localMinValue);
                cumLut[i] = newValue >= 0 ? newValue : 0;
            }

            for (Int32 h = 0; h < bitmap.Height; ++h)
                for (Int32 w = 0; w < bitmap.Width; ++w)
                {
                    Color color = bitmap.GetPixel(w, h);
                    byte intensity = color.R;
                    byte result = (byte) cumLut[intensity];
                    bitmap.SetPixel(w, h, Color.FromArgb(color.A, result, result, result));
                }
        }

        // formuła do wyliczenia skumulowanego histogramu
        // zwraca tablicę na podstawie, której taki histogram można stworzyć
        private static int[] cumsum(int[] greyLut)
        {
            int[] cumLut = new int[256];
            cumLut[0] = greyLut[0];
            for (int i = 1; i < 256; ++i)
            {
                cumLut[i] = cumLut[i - 1] + greyLut[i];
            }

            return cumLut;

        }

        // znajdź pierwszą niezerową wartość
        private static byte findMin(int[] lutTable)
        {
            byte localMin = 0;
            while (lutTable[localMin] == 0) ++localMin;
            return localMin;
        }

        // znajdź ostatnią niezerową wartość
        private static byte findMax(int[] lutTable)
        {
            byte localMax = 0;
            while (lutTable[localMax] == 0) --localMax;
            return localMax;
        }

        //*******************THRESHOLDING******************************************
        public static void thresholding(Bitmap bitmap, byte threshold)
        {
            for (Int32 h = 0; h < bitmap.Height; ++h)
                for (Int32 w = 0; w < bitmap.Width; ++w)
                {
                    Color color = bitmap.GetPixel(w, h);
                    byte intensity = color.R;
                    byte newValue = (intensity > threshold) ? (byte) 255 : (byte) 0;
                    bitmap.SetPixel(w, h, Color.FromArgb(color.A, newValue, newValue, newValue));
                }
        }


        //*******************THRESHOLDING WITH GRAY LEVELS************************
        // progowanie z zachowaniem poziomów szarości

        public static void grayLevelsThresholding(Bitmap bitmap, byte threshold)
        {
            for (Int32 h = 0; h < bitmap.Height; ++h)
                for (Int32 w = 0; w < bitmap.Width; ++w)
                {
                    Color color = bitmap.GetPixel(w, h);
                    byte intensity = color.R;
                    byte newValue = (intensity > threshold) ? (byte)intensity : (byte)0;
                    bitmap.SetPixel(w, h, Color.FromArgb(color.A, newValue, newValue, newValue));
                }
        }

        //*******************POSTERIZE********************************************

        public static void posterize(Bitmap bitmap, byte binsAmount)
        {
            // dla np. 8 zwraca 32
            byte binSize = (byte) Math.Round(255.0 / binsAmount);

            // inicjalizcja tablicy odpowiedniej długości
            byte[] myBins = new byte[binsAmount - 1];

            // np. 32,64,96,128,160,192,224
            for(byte i = 0; i < binsAmount-1; ++i)
            {
                myBins[i] = (byte) (binSize *(i+1));
            }

            // ustawienie odpowiednich wartości obrazu
            for (Int32 h = 0; h < bitmap.Height; ++h)
                for (Int32 w = 0; w < bitmap.Width; ++w)
                {
                    Color color = bitmap.GetPixel(w, h);
                    byte intensity = color.R;
                    byte newValue = getNewValueFromBins(myBins, binsAmount, intensity, binSize);
                    bitmap.SetPixel(w, h, Color.FromArgb(color.A, newValue, newValue, newValue));
                }

        }

        private static byte getNewValueFromBins(byte[] myBins, byte binAmount, byte colorValue, byte binSize) 
        {
            for (byte i = 0; i < binAmount-1; ++i)
            {
                if (colorValue <= myBins[i]) return (byte) ((i) * binSize); 
            }
            return 255;
        }

        //*******************Adjustable Stretching********************************
        public static void adjustableStretching(Bitmap bitmap, byte p1, byte p2, byte q3, byte q4)
        {
            if (p2 < p1 && q4 < q3) return;
            byte pSize = (byte) (p2 - p1);
            byte qSize = (byte) (q4 - q3);
            Console.WriteLine(pSize + " " + qSize);

            for(Int32 h = 0; h < bitmap.Height; ++h)
                for(Int32 w = 0; w < bitmap.Width; ++w)
                {
                    Color color = bitmap.GetPixel(w, h);
                    byte redValue = color.R;
                    byte newValue;
                    if (redValue < p1 || redValue > p2) newValue = 0;
                    else newValue = (byte)(q3 + (((redValue - p1) / (double)pSize) * qSize));

                    bitmap.SetPixel(w, h, Color.FromArgb(color.A, newValue, newValue, newValue));
                }
        }

        //-----------------------------------------OPEN_CV------------------------------------
        // Blurowanie
        
        public static void blur(PictureWindow pw, Size kernel, BorderType borderType)
        {
            // rozmiar filtra
            Size kernelSize = kernel;
            // bitmapa źródłowa
            Bitmap source = pw.Bitmap;
            // przekonwertuj bitmapa na format emgu
            Image<Gray, byte> emguImage = source.ToImage<Gray, byte>();
            // wykonaj operację blur
            CvInvoke.Blur(emguImage, emguImage, kernelSize , new Point(-1, -1), borderType);
            // ponownie zmień typ na bitmap
            Bitmap result = emguImage.ToBitmap();
            // zapisz rezulat w oknie wynikowym
            pw.Bitmap = result;
        }

        public static void gaussianBlur(PictureWindow pw, Size size, BorderType borderType, double sX, double sY)
        {
            // rozmiar filtra
            Size kernelSize = new Size(5, 5);
            // odch. std. w kierunku X;
            double sigmaX = 0;
            // odch. std. w kierunku Y
            double sigmaY = sigmaX;

            // bitmapa źródłowa
            Bitmap source = pw.Bitmap;
            // przekonwertuj bitmapa na format emgu
            Image<Gray, byte> emguImage = source.ToImage<Gray, byte>();
            // wykonaj operację gaussianBlur
            CvInvoke.GaussianBlur(emguImage, emguImage, kernelSize, sigmaX, sigmaY, borderType);
            // ponownie zmień typ na bitmap
            Bitmap result = emguImage.ToBitmap();
            // zapisz rezulat w oknie wynikowym
            pw.Bitmap = result;
        }

        // Detekcja krawędzi
        // do wywalenia
        public static void laplacian(PictureWindow pw)
        {
            // format obrazu wyjściowego
            var ddepth = DepthType.Cv8U;
            // rozmiar filtra
            int ksize = 3;

            // bitmapa źródłowa
            Bitmap source = pw.Bitmap;
            // przekonwertuj bitmapa na format emgu
            Image<Gray, byte> emguImage = source.ToImage<Gray, byte>();
            // wykonaj operację detekcji krawędzi
            CvInvoke.Laplacian(emguImage, emguImage, ddepth, ksize);
            // ponownie zmień typ na bitmap
            Bitmap result = emguImage.ToBitmap();
            // zapisz rezulat w oknie wynikowym
            pw.Bitmap = result;
        }

        public static void laplacian(PictureWindow pw, int kernel, BorderType border)
        {
            // format obrazu wyjściowego
            var ddepth = DepthType.Cv8U;

            // bitmapa źródłowa
            Bitmap source = pw.Bitmap;
            // przekonwertuj bitmapa na format emgu
            Image<Gray, byte> emguImage = source.ToImage<Gray, byte>();
            // wykonaj operację detekcji krawędzi
            CvInvoke.Laplacian(emguImage, emguImage, ddepth, kernel, 1, 0, border);
            // ponownie zmień typ na bitmap
            Bitmap result = emguImage.ToBitmap();
            // zapisz rezulat w oknie wynikowym
            pw.Bitmap = result;
        }

        // do wywalenia
        public static void sobel(PictureWindow pw)
        {
            // format obrazu wyjściowego
            var ddepth = DepthType.Cv8U;
            // rozmiar filtra
            int ksize = 5;

            // bitmapa źródłowa
            Bitmap source = pw.Bitmap;
            // przekonwertuj bitmapa na format emgu
            Image<Gray, byte> emguImage = source.ToImage<Gray, byte>();
            // wykonaj operację detekcji krawędzi
            CvInvoke.Sobel(emguImage, emguImage, ddepth, 0, 1, ksize); // 0,1; 1,0
            
            // ponownie zmień typ na bitmap
            Bitmap result = emguImage.ToBitmap();
            // zapisz rezulat w oknie wynikowym
            pw.Bitmap = result;
        }

        public static void sobel(PictureWindow pw, int kernel, int xorder, int yorder, BorderType border)
        {
            // format obrazu wyjściowego
            var ddepth = DepthType.Cv8U;

            // bitmapa źródłowa
            Bitmap source = pw.Bitmap;
            // przekonwertuj bitmapa na format emgu
            Image<Gray, byte> emguImage = source.ToImage<Gray, byte>();
            // wykonaj operację detekcji krawędzi
            CvInvoke.Sobel(emguImage, emguImage, ddepth, xorder, yorder, kernel, 1, 0, border); // 0,1; 1,0
            // ponownie zmień typ na bitmap
            Bitmap result = emguImage.ToBitmap();
            // zapisz rezulat w oknie wynikowym
            pw.Bitmap = result;
        }

        // do wywalenia
        public static void canny(PictureWindow pw)
        {
            // progi
            double threshold1 = 100;
            double threshold2 = 200;

            // bitmapa źródłowa
            Bitmap source = pw.Bitmap;
            // przekonwertuj bitmapa na format emgu
            Image<Gray, byte> emguImage = source.ToImage<Gray, byte>();
            // wykonaj operację detekcji krawędzi
            CvInvoke.Canny(emguImage, emguImage, threshold1, threshold2);
            // ponownie zmień typ na bitmap
            Bitmap result = emguImage.ToBitmap();
            // zapisz rezulat w oknie wynikowym
            pw.Bitmap = result;
        }

        public static void canny(PictureWindow pw, double th1, double th2)
        {
            // bitmapa źródłowa
            Bitmap source = pw.Bitmap;
            // przekonwertuj bitmapa na format emgu
            Image<Gray, byte> emguImage = source.ToImage<Gray, byte>();
            // wykonaj operację detekcji krawędzi
            CvInvoke.Canny(emguImage, emguImage, th1, th2);
            // ponownie zmień typ na bitmap
            Bitmap result = emguImage.ToBitmap();
            // zapisz rezulat w oknie wynikowym
            pw.Bitmap = result;
        }

        // Wyostrzanie liniowe
        public static void linearSharpening(PictureWindow pw, Matrix<double> matrix, BorderType border)
        {
            // przypisanie maski
            var maskSharp = matrix;
            // przypisanie operacji brzegowych
            var borderKind = border;

            // bitmapa źródłowa
            Bitmap source = pw.Bitmap;
            // przekonwertuj bitmapa na format emgu
            Image<Gray, byte> emguImage = source.ToImage<Gray, byte>();
            // wykonaj operację detekcji krawędzi
            CvInvoke.Filter2D(emguImage, emguImage, maskSharp, new Point(-1, -1), 0, borderKind);
            // ponownie zmień typ na bitmap
            Bitmap result = emguImage.ToBitmap();
            // zapisz rezulat w oknie wynikowym
            pw.Bitmap = result;
        }

        // Kierunkowa detekcja krawędzi
        public static void directionalEdgeDetection(PictureWindow pw, Matrix<double> matrix, BorderType border)
        {
            // przypisanie maski
            var maskSharp = matrix;
            // przypisanie operacji brzegowych
            var borderKind = border;

            // bitmapa źródłowa
            Bitmap source = pw.Bitmap;
            // przekonwertuj bitmapa na format emgu
            Image<Gray, byte> emguImage = source.ToImage<Gray, byte>();
            // wykonaj operację detekcji krawędzi
            CvInvoke.Filter2D(emguImage, emguImage, maskSharp, new Point(-1, -1), 0, borderKind);
            // ponownie zmień typ na bitmap
            Bitmap result = emguImage.ToBitmap();
            // zapisz rezulat w oknie wynikowym
            pw.Bitmap = result;
        }

        /*
         * Nie można wywoływać operacji używających setPixel po operacjach EmguCv
         */
    }
}
