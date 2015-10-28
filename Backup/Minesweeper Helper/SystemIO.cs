using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.IO;
using System.Collections;

namespace Minesweeper_Helper
{
    //SystemIO provides in/out between the puzzle logic, and Windows
    //Look to the public functions for how to use it :)
    class SystemIO
    {
        static Bitmap scr = null; // Saves the last screen that we are working with

        private int topHeight = 0;    //The x- and y- coordinates of the 
        private int bottomHeight = 0; //minesweeper game on the screen
        private int leftCol = 0;
        private int rightCol = 0;

        private int width = 0;  //How wide and tall the entire board is 
        private int height = 0; //(in pixels)

        private double cellDimDbl = 0.0; //How wide and tall a cell really is

        private int numWidth = 0; //How many cells in the board
        private int numHeight = 0;

        private bool[,] parsed; //Spaces to not look at anymore

        private OCR myOCR; //Character recognition

        //Game options
        private bool DISTINGUISH_378 = true;

        public SystemIO()
        {
            //The smallest edge of a box is ~18 pixels, and the largest is
            //~100 pixels
            //The smallest games is 9x9 boxes, and the largest is 24 rows by
            //30 cols

            //The general method of finding the game-play box will be to look
            //side-to-side and up-down for rows/columns of pixels
            //The isWhiteUL methods check if a space is "white" (bright enough
            //not to be an edge) on the upper-left edges, while LR is for 
            //the lower-right edges
            //Also, note that "up" means decreasing y

            getScreen();
            myOCR = new OCR();

            //Search left for a vertical edge that is at least 130 pixels tall
            int colHeight = 0;
            Point pixelIndex = getCursor();
            while (colHeight < 130)
            {
                colHeight = 0;
                pixelIndex.X--;
                //Search up from this point
                while (!isWhiteUL(getPixel(pixelIndex)))
                {
                    colHeight++;
                    pixelIndex.Y--;
                }
                //Search down from the point
                pixelIndex.Y = getCursor().Y;
                pixelIndex.Y++;
                while (!isWhiteUL(getPixel(pixelIndex)))
                {
                    colHeight++;
                    pixelIndex.Y++;
                }
            }
            //At this point, the pixelIndex.X to a valid vertical column
            //Keep searching vertically until you hit the end
            //(because of variations, let the end be 3 non-edge in a row)
            Point oneup = new Point(pixelIndex.X, pixelIndex.Y);
            oneup.Y--;
            Point twoup = new Point(oneup.X, oneup.Y);
            oneup.Y--;
            while (!isWhiteUL(getPixel(pixelIndex)) ||
                   !isWhiteUL(getPixel(oneup)) ||
                   !isWhiteUL(getPixel(twoup)))
            {
                pixelIndex.Y--;
                oneup.Y--;
                twoup.Y--;
            }
            pixelIndex.Y++; //we went too far
            topHeight = pixelIndex.Y;

            //Now search left until you reach the top left corner
            while (!isWhiteUL(getPixel(pixelIndex)))
                pixelIndex.X--;
            pixelIndex.X++; //we went too far
            leftCol = pixelIndex.X;

            //Now search right until you reach the top right corner
            while (!isWhiteUL(getPixel(pixelIndex)))
                pixelIndex.X++;
            rightCol = pixelIndex.X; //this value and the next are off-by-one
            pixelIndex.X--; //we went too far
            width = rightCol - leftCol;


            //Now search down until you reach the bottom right corner
            pixelIndex.Y += 3; //avoid a corner if need be
            if (isWhiteLR(getPixel(pixelIndex))) //we have a problem. we need
            {                                    //to be to the left one
                rightCol--;
                width--;
                pixelIndex.X--; //we went too far
            }
            while (!isWhiteLR(getPixel(pixelIndex)))
                pixelIndex.Y++;
            bottomHeight = pixelIndex.Y;
            height = bottomHeight - topHeight;


            Point p = new Point(leftCol + 8, topHeight + 8);
            //Advance through 3 whole squares, but not the next edge
            while (isWhite(getPixel(p), 40))
                p.X++;
            int left = p.X;
            p.X += 9;
            while (isWhite(getPixel(p), 40))
                p.X++;
            p.X += 9;
            while (isWhite(getPixel(p), 40))
                p.X++;
            p.X += 9;
            while (isWhite(getPixel(p), 40))
                p.X++;


            int cellDim3 = p.X - left; //how wide 3 cells are
            numWidth = (int)Math.Round((((double)width) / cellDim3) * 3);
            numHeight = (int)Math.Round((((double)height) / cellDim3) * 3);
            cellDimDbl = ((double)width) / numWidth;

            parsed = new bool[numWidth, numHeight];
        }

        //The functions SystemIO provides
        public void click(Point p) //clicking on a square
        {
            int xLoc = (int)Math.Round((.5 + p.X) * cellDimDbl + leftCol);
            int yLoc = (int)Math.Round((.5 + p.Y) * cellDimDbl + topHeight);
            moveCursor(new Point(xLoc, yLoc));
            leftClick();     //        9 ms is experimentally 
            Thread.Sleep(9); //how long Minesweeper needs to process clicks
        }
        public void clickR(Point p) //right clicking on a square
        {
            int xLoc = (int)Math.Round((.5 + p.X) * cellDimDbl + leftCol);
            int yLoc = (int)Math.Round((.5 + p.Y) * cellDimDbl + topHeight);
            moveCursor(new Point(xLoc, yLoc));
            //Thread.Sleep(1); //this is probably not needed
            rightClick();     //about moving and clicking
            Thread.Sleep(9);
        }
        public void select()//returns the Windows selection to Minesweeper
        {
            click(new Point(numWidth, 0));
        }
        public int getWidth() //getting the width and height of the board
        {
            return numWidth;
        }
        public int getHeight()
        {
            return numHeight;
        }
        public void update() //IMPORTANT--gets a new screenshot
        {
            getScreen();
        }
        public int getNum(Point p) //gets the number at a location
        {
            return pointToNum(p);
        }
        public List<KeyValuePair<Point, int>> getNums() //getNum of all updates
        {
            List<KeyValuePair<Point, int>> ans =
                new List<KeyValuePair<Point, int>>();
            Point p = new Point(0, 0);
            for (; p.Y < numHeight; p.Y++)
                for (p.X = 0; p.X < numWidth; p.X++)
                    if (!parsed[p.X, p.Y])
                    {
                        int num = getNum(p);
                        if (num >= 0 && num < 9)
                        {
                            ans.Add(new KeyValuePair<Point, int>(p, num));
                            parsed[p.X, p.Y] = true;
                        }
                    }
            return ans;
        }
        public void inputMines(List<Point> ps) //don't re-look at mines
        {
            foreach (Point p in ps)
                parsed[p.X, p.Y] = true;
        }
        public int getMinesRemaining()
        {
            return minesRemaining();
        }

        public void setDistinguish378(bool input)
        {
            DISTINGUISH_378 = input;
        }


        //Functions SetCursorPos, GetCursorPos, and mouse_event are magic dll
        //stuff. All I know is that I tried it, and it worked.
        [DllImport("user32.dll")]
        private extern static bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")]
        private static extern int GetCursorPos(ref Point lpPoint);
        [DllImport("user32.dll")]
        private static extern void mouse_event(UInt32 dwFlags, UInt32 dx,
            UInt32 dy, UInt32 dwData, IntPtr dwExtraInfo);

        //Handles copying PrintScreen into a Bitmap
        private class GDI32
        { //http://www.c-sharpcorner.com/UploadFile/perrylee/ScreenCapture11142005234547PM/ScreenCapture.aspx
            [DllImport("GDI32.dll")]
            public static extern bool BitBlt(int hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, int hdcSrc, int nXSrc, int nYSrc, int dwRop);
            [DllImport("GDI32.dll")]
            public static extern int CreateCompatibleBitmap(int hdc, int nWidth, int nHeight);
            [DllImport("GDI32.dll")]
            public static extern int CreateCompatibleDC(int hdc);
            [DllImport("GDI32.dll")]
            public static extern bool DeleteDC(int hdc);
            [DllImport("GDI32.dll")]
            public static extern bool DeleteObject(int hObject);
            [DllImport("GDI32.dll")]
            public static extern int GetDeviceCaps(int hdc, int nIndex);
            [DllImport("GDI32.dll")]
            public static extern int SelectObject(int hdc, int hgdiobj);
            [DllImport("User32.dll")]
            public static extern int GetDesktopWindow();
            [DllImport("User32.dll")]
            public static extern int GetWindowDC(int hWnd);
            [DllImport("User32.dll")]
            public static extern int ReleaseDC(int hWnd, int hDC);

            static int pictNumber = 0;

            public static Bitmap CaptureScreen()
            {
                int hdcSrc = GetWindowDC(GetDesktopWindow()),
                hdcDest = GDI32.CreateCompatibleDC(hdcSrc),
                hBitmap = GDI32.CreateCompatibleBitmap(
                                hdcSrc,
                                GDI32.GetDeviceCaps(hdcSrc, 8),
                                GDI32.GetDeviceCaps(hdcSrc, 10));
                GDI32.SelectObject(hdcDest, hBitmap);
                GDI32.BitBlt(hdcDest, 0, 0, GDI32.GetDeviceCaps(hdcSrc, 8),
                GDI32.GetDeviceCaps(hdcSrc, 10), hdcSrc, 0, 0, 0x00CC0020);
                Bitmap screen = Image.FromHbitmap(new IntPtr(hBitmap));
                Cleanup(hBitmap, hdcSrc, hdcDest);
                return screen;
            }
            private static void Cleanup(int hBitmap, int hdcSrc, int hdcDest)
            {
                ReleaseDC(GetDesktopWindow(), hdcSrc);
                GDI32.DeleteDC(hdcDest);
                GDI32.DeleteObject(hBitmap);
            }
            public static void SaveImage(Bitmap image)
            {
                // Puts the file into "debugging output"
                pictNumber++;
                image.Save("todelete" + pictNumber + ".bmp", ImageFormat.Bmp);
            }
        }

        //Some Optical Character Recognition (that I made from scratch)
        //Takes a black-and-white bitmap and gives a string from "0" to "9" or
        //an empty string if the image is not the right dimensions to be text
        private class OCR
        {
            string[] nums = new string[10];
            int[] widths = new int[10] { 24, 21, 22, 21, 26, 21, 23, 23,
                                         24, 23 };
            bool[][][] numPixels = new bool[10][][];
            string[] numsBold = new string[3];
            int[] widthsBold = new int[3] { 24, 27, 28 };
            bool[][][] numPixelsBold = new bool[3][][];
            public OCR()
            {
                nums[0] =
                "        *********       " +
                "      *************     " +
                "     ***************    " +
                "    ******     ******   " +
                "   ******       *****   " +
                "   *****         *****  " +
                "   ****          ****** " +
                "  *****           ***** " +
                "  ****            ***** " +
                " *****            ***** " +
                " *****             **** " +
                " *****             *****" +
                " *****             *****" +
                " ****              *****" +
                " ****              *****" +
                "*****              *****" +
                "*****              *****" +
                "*****              *****" +
                "*****              *****" +
                "*****              *****" +
                "*****              *****" +
                "*****              *****" +
                "*****              *****" +
                " ****              *****" +
                " ****              *****" +
                " *****             **** " +
                " *****            ***** " +
                " *****            ***** " +
                "  *****           ***** " +
                "  *****          ****** " +
                "   *****         *****  " +
                "   ******       *****   " +
                "    ******     ******   " +
                "     ***************    " +
                "     **************     " +
                "        *********       " +
                "        ********        ";

                nums[1] =
                "          ***        " +
                "     ********        " +
                "    *********        " +
                "*************        " +
                "*************        " +
                "*************        " +
                "***     *****        " +
                "***     *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                "        *****        " +
                " ********************" +
                "*********************" +
                "*********************" +
                " ******************* ";

                nums[2] =
                "        ********      " +
                "       **********     " +
                "    ***************   " +
                "   *****************  " +
                "  ******       ****** " +
                "  ****          ***** " +
                "  ***            *****" +
                "                 *****" +
                "                 *****" +
                "                  ****" +
                "                  ****" +
                "                 *****" +
                "                 *****" +
                "                 *****" +
                "                 **** " +
                "                ***** " +
                "                ***** " +
                "               *****  " +
                "              *****   " +
                "             ******   " +
                "            ******    " +
                "            *****     " +
                "           *****      " +
                "          *****       " +
                "         *****        " +
                "        *****         " +
                "       *****          " +
                "      *****           " +
                "     ******           " +
                "    *****             " +
                "   *****              " +
                "  ******              " +
                " *****                " +
                " ******************** " +
                "**********************" +
                "**********************" +
                " ******************** ";

                nums[3] =
                "     *********       " +
                "    ***********      " +
                "  ***************    " +
                "  ****************   " +
                "  ***       ******   " +
                "  **         ******  " +
                "              *****  " +
                "              *****  " +
                "               ****  " +
                "               ****  " +
                "               ****  " +
                "              *****  " +
                "              *****  " +
                "              ****   " +
                "            ******   " +
                "           ******    " +
                "    ***********      " +
                "   ************      " +
                "   *************     " +
                "     *************   " +
                "            *******  " +
                "              *****  " +
                "               ***** " +
                "               ***** " +
                "                *****" +
                "                *****" +
                "                *****" +
                "                *****" +
                "                *****" +
                "               ***** " +
                "              ****** " +
                "**            *****  " +
                "*****       *******  " +
                "******************   " +
                "****************     " +
                "  ************       " +
                "   **********        ";
    
                nums[4] =
                "                *****     " +
                "               ******     " +
                "              *******     " +
                "              *******     " +
                "             ********     " +
                "             ********     " +
                "            *********     " +
                "           ****  ****     " +
                "          *****  ****     " +
                "          ****   ****     " +
                "         ****    ****     " +
                "        *****    ****     " +
                "        ****     ****     " +
                "       *****     ****     " +
                "      *****      ****     " +
                "      ****       ****     " +
                "     *****       ****     " +
                "     ****        ****     " +
                "    ****         ****     " +
                "   *****         ****     " +
                "   ****          ****     " +
                "  *****          ****     " +
                " *****           ****     " +
                " ************************ " +
                "**************************" +
                "**************************" +
                "**************************" +
                "                 *****    " +
                "                 ****     " +
                "                 ****     " +
                "                 ****     " +
                "                 *****    " +
                "                 *****    " +
                "                 *****    " +
                "                 *****    " +
                "                 ****     " +
                "                  ***     ";
    
                nums[5] =
                " ******************  " +
                " ******************  " +
                " ******************  " +
                " *****               " +
                " *****               " +
                " *****               " +
                " *****               " +
                " *****               " +
                " *****               " +
                " *****               " +
                " *****               " +
                " *****               " +
                " *****               " +
                " *****               " +
                " *************       " +
                " **************      " +
                " ****************    " +
                "    ***************  " +
                "            *******  " +
                "              *****  " +
                "               ***** " +
                "               ******" +
                "                *****" +
                "                *****" +
                "                *****" +
                "                *****" +
                "                *****" +
                "                *****" +
                "               ******" +
                "               ***** " +
                "              *****  " +
                "             ******  " +
                "***        *******   " +
                "******************   " +
                "****************     " +
                "**************       " +
                " ************        ";

                nums[6] =
                "          **********   " +
                "       *************   " +
                "       **************  " +
                "     *******      ***  " +
                "     *****             " +
                "    *****              " +
                "   *****               " +
                "  *****                " +
                "  *****                " +
                "  ****                 " +
                " *****                 " +
                " *****                 " +
                " ****                  " +
                " ****                  " +
                " ****   *********      " +
                "*****  ***********     " +
                "**** ***************   " +
                "*********     *******  " +
                "********       ******  " +
                "*******         ****** " +
                "******           ***** " +
                "******            *****" +
                "*****             *****" +
                "*****             *****" +
                "*****             *****" +
                "*****             *****" +
                " ****             *****" +
                " *****            *****" +
                " *****            *****" +
                "  ****           ***** " +
                "  *****          ***** " +
                "  ******        *****  " +
                "   ******      *****   " +
                "    ****************   " +
                "     **************    " +
                "       **********      " +
                "        *******        ";

                nums[7] =
                "***********************" +
                "***********************" +
                " **********************" +
                "                 ***** " +
                "                 ***** " +
                "                 ***** " +
                "                 ****  " +
                "                *****  " +
                "                *****  " +
                "                ****   " +
                "               *****   " +
                "               ****    " +
                "              *****    " +
                "              *****    " +
                "              ****     " +
                "             *****     " +
                "             *****     " +
                "            *****      " +
                "            *****      " +
                "           *****       " +
                "           *****       " +
                "           *****       " +
                "           ****        " +
                "          *****        " +
                "         ******        " +
                "         *****         " +
                "         *****         " +
                "        *****          " +
                "        *****          " +
                "        *****          " +
                "       *****           " +
                "       *****           " +
                "      *****            " +
                "      *****            " +
                "      *****            " +
                "     *****             " +
                "      ***              ";

                nums[8] =
                "        **********      " +
                "     **************     " +
                "     ***************    " +
                "    ******     ******   " +
                "   ******       ******  " +
                "   *****         *****  " +
                "  *****           ***** " +
                "  *****           ***** " +
                "  *****           ***** " +
                "  *****           ***** " +
                "  *****           ****  " +
                "   *****         *****  " +
                "   *****        *****   " +
                "   ******       *****   " +
                "    *******  *******    " +
                "     **************     " +
                "       **********       " +
                "       ***********      " +
                "      ************      " +
                "     ******* ********   " +
                "   ********  *********  " +
                "   ******       ******  " +
                "  *****          ****** " +
                " ******           ***** " +
                " *****            ***** " +
                " *****             *****" +
                "*****              *****" +
                "*****              *****" +
                " *****             *****" +
                " *****            ***** " +
                " ******           ***** " +
                "  ******         ****** " +
                "  *******       ******  " +
                "   ******************   " +
                "    *****************   " +
                "      ************      " +
                "       *********        ";

                nums[9] =
                "       *********       " +
                "    **************     " +
                "   ****************    " +
                "  ******      ******   " +
                "  *****        *****   " +
                " *****          *****  " +
                "*****            ****  " +
                "*****            ***** " +
                "****             ***** " +
                "****              **** " +
                "****              **** " +
                "****              **** " +
                "****              *****" +
                "****              *****" +
                "*****            ******" +
                "******           ******" +
                " *****          *******" +
                " *******       ********" +
                "  *******    **********" +
                "   ************** *****" +
                "    ************* *****" +
                "      *********   **** " +
                "                  **** " +
                "                 ***** " +
                "                 ***** " +
                "                 ***** " +
                "                 ***** " +
                "                *****  " +
                "               ******  " +
                "               *****   " +
                "              ******   " +
                "            *******    " +
                "  **        ******     " +
                "  ***************      " +
                "  **************       " +
                "  ************         " +
                "   *********           ";



                numsBold[0] =
                "    *************       " +
                "  *****************     " +
                "  *****************     " +
                "  ******************    " +
                "  *******************   " +
                "  ********************  " +
                "  ********************* " +
                "  *****     *********** " +
                "  ***        ********** " +
                "              ********* " +
                "              ********* " +
                "              ********* " +
                "              ********* " +
                "              ********* " +
                "             *********  " +
                "           ***********  " +
                "     ****************   " +
                "    ****************    " +
                "    ***************     " +
                "    ****************    " +
                "    ******************  " +
                "     ****************** " +
                "           ************ " +
                "              **********" +
                "              **********" +
                "               *********" +
                "                ********" +
                "               *********" +
                "               *********" +
                "***           **********" +
                "*******     *********** " +
                "*********************** " +
                "*********************** " +
                "**********************  " +
                "********************    " +
                " ******************     " +
                "  ***************       ";
                
                numsBold[1] =
                " **************************" +
                " **************************" +
                " **************************" +
                " **************************" +
                "************************** " +
                " ************************* " +
                " ************************* " +
                "                 ********  " +
                "                 ********  " +
                "                ********   " +
                "                ********   " +
                "               ********    " +
                "               ********    " +
                "              *********    " +
                "              *********    " +
                "              ********     " +
                "             *********     " +
                "             ********      " +
                "            *********      " +
                "            *********      " +
                "            ********       " +
                "           *********       " +
                "           ********        " +
                "          *********        " +
                "          *********        " +
                "          *********        " +
                "         *********         " +
                "         ********          " +
                "        *********          " +
                "       **********          " +
                "       *********           " +
                "       *********           " +
                "      **********           " +
                "      *********            " +
                "      *********            " +
                "     *********             " +
                "     *********             ";
                
                numsBold[2] =
                "       *************        " +
                "      ****************      " +
                "     ******************     " +
                "    ********************    " +
                "   **********************   " +
                "  ***********************   " +
                "  **********   ***********  " +
                "  *********      *********  " +
                "  ********       *********  " +
                "  ********       *********  " +
                "  ********       *********  " +
                "  ********       *********  " +
                "  *********     *********   " +
                "  **********   *********    " +
                "   *********************    " +
                "    *******************     " +
                "     ****************       " +
                "       *************        " +
                "      ****************      " +
                "    *******************     " +
                "   *********************    " +
                "  ***********************   " +
                "  *********    ***********  " +
                " *********       *********  " +
                " ********        *********  " +
                "*********         ********* " +
                "*********         ********* " +
                "*********         ********* " +
                "*********         ********* " +
                "**********       ********** " +
                " ***********    **********  " +
                " *************************  " +
                "  ***********************   " +
                "   *********************    " +
                "    *******************     " +
                "     *****************      " +
                "       *************        ";

                for (int i = 0; i < 10; ++i)
                {
                    numPixels[i] = new bool[26][];
                    for (int j = 0; j < numPixels[i].Length; ++j)
                        numPixels[i][j] = new bool[37];
                }

                for (int i = 0; i < 3; ++i)
                {
                    numPixelsBold[i] = new bool[28][];
                    for (int j = 0; j < numPixelsBold[i].Length; ++j)
                        numPixelsBold[i][j] = new bool[37];
                }

                for (int i = 0; i < 10; ++i)
                {
                    int index = 0;
                    for (int y = 0; y < 37; ++y)
                        for (int x = 0; x < widths[i]; ++x)
                        {
                            numPixels[i][x][y] = (nums[i][index] == '*');
                            index++;
                        }
                }

                for (int i = 0; i < 3; ++i)
                {
                    int index = 0;
                    for (int y = 0; y < 37; ++y)
                        for (int x = 0; x < widthsBold[i]; ++x)
                        {
                            numPixelsBold[i][x][y] = (numsBold[i][index] == '*');
                            index++;
                        }
                }
            }//very very messy data storage stuff
            //Converts an image into a string "0" to "9" or ""
            public string imageToText(Bitmap input)
            {
                if (input.Width < 3)
                    return ""; //don't bother checking a trivial image

                double[] prob = new double[10]; //stores how well it matches
                for (int n = 0; n < prob.Length; ++n)
                    prob[n] = 0.0;
                //Each pixel is first translated into "26x37" decimal
                //"gray-scale", and each resulting "partial pixel" is checked
                //against the 26x37 master images/bits
                for (int x = 0; x < input.Width; ++x)
                    for (int y = 0; y < input.Height; ++y)
                    {
                        //These represent the edges of the original pixel
                        double left = ((double)x) * 37 / input.Height,
                               right = ((double)(x + 1)) * 37 / input.Height,
                               top = ((double)(y)) * 37 / input.Height,
                               bottom = ((double)(y + 1)) * 37 / input.Height;

                        //They are checked against the actual to find "gray"
                        for (int i = (int)left; i < (int)right && i < 26; ++i)
                            for (int j = (int)top; j < (int)bottom; ++j)
                            {
                                double proportionW = 1.0;
                                if (i < left)
                                    proportionW -= (left - i);
                                if (i > right)
                                    proportionW += (right - i);
                                double proportionH = 1.0;
                                if (j < top)
                                    proportionH -= (top - j);
                                if (j > bottom)
                                    proportionH += (bottom - j);

                                //proportion is the pixel translated: 1.0 is a 
                                //whole white pixel, while 0.3 is 30% white
                                double proportion = proportionH * proportionW;

                                //each master is compared against the input
                                for (int k = 0; k < 10; ++k)
                                    if (input.GetPixel(x, y).R > 155)
                                        if (numPixels[k][i][j])
                                            prob[k] += 2 * proportion;
                                        else //this is the worst: extra lines!
                                             //some extra penalty will be taken
                                            prob[k] -= proportion * 
                                              distanceAway(i, j, numPixels[k]);
                                    else
                                        if (numPixels[k][i][j])
                                            prob[k] -= proportion;
                                        else
                                            prob[k] += proportion;
                            }
                    }
                //To distinguish zero's from nine's, the center is tested
                bool centerMarked = false;
                for (int y = (input.Height + 1) / 4;
                                                 y < 3 * input.Height / 4; ++y)
                    if (input.GetPixel(input.Width / 2, y).R > 155)
                        centerMarked = true;
                if (!centerMarked)
                    for (int n = 2; n < 10; ++n)
                        prob[n] -= 200;

                //To distinguish eight's from three's, test lower left
                bool lowerLeftMarked = false;
                for (int x = 0; x < input.Width / 3; ++x)
                    if (input.GetPixel(x, 3 * (input.Height + 1) / 4).R > 155)
                        lowerLeftMarked = true;
                if (lowerLeftMarked)//more likely to be eight, but don't
                    prob[3] -= 200; //heighten eight above the rest
                else
                    prob[8] -= 200;
                //Console.Write("   is a eight: {0}   ", lowerLeftMarked);
                
                double maxProb = -9999;
                int likelyNum = -1;
                for(int i = 0; i < 10; ++i)
                    if (prob[i] > maxProb)
                    {
                        maxProb = prob[i];
                        likelyNum = i;
                    }

                //for (int n = 0; n < 10; ++n)
                //    Console.WriteLine("{0} prob is: {1}", n, prob[n]);

                if (maxProb < 0)
                    return ""; //nothing likely was found
                return "" + likelyNum;
            }
            //Given a portion of the screen, returns "3" "7" "8" or ""
            public string imageTo378(Bitmap input)
            {
                double[] prob = new double[3]; //stores how well it matches
                for (int n = 0; n < prob.Length; ++n)
                    prob[n] = 0.0;
                //Each pixel is first translated into "28x37" decimal
                //"gray-scale", and each resulting "partial pixel" is checked
                //against the 26x37 master images/bits
                for (int x = 0; x < input.Width; ++x)
                    for (int y = 0; y < input.Height; ++y)
                    {
                        //These represent the edges of the original pixel
                        double left = ((double)x) * 37 / input.Height,
                               right = ((double)(x + 1)) * 37 / input.Height,
                               top = ((double)(y)) * 37 / input.Height,
                               bottom = ((double)(y + 1)) * 37 / input.Height;

                        //They are checked against the actual to find "gray"
                        for (int i = (int)left; i < (int)right && i < 28; ++i)
                            for (int j = (int)top; j < (int)bottom; ++j)
                            {
                                double proportionW = 1.0;
                                if (i < left)
                                    proportionW -= (left - i);
                                if (i > right)
                                    proportionW += (right - i);
                                double proportionH = 1.0;
                                if (j < top)
                                    proportionH -= (top - j);
                                if (j > bottom)
                                    proportionH += (bottom - j);

                                //proportion is the pixel translated: 1.0 is a 
                                //whole white pixel, while 0.3 is 30% white
                                double proportion = proportionH * proportionW;

                                //each master is compared against the input
                                for (int k = 0; k < 3; ++k)
                                    if (input.GetPixel(x, y).R > 155)
                                        if (numPixelsBold[k][i][j])
                                            prob[k] += 2 * proportion;
                                        else //this is the worst: extra lines!
                                            //some extra penalty will be taken
                                            prob[k] -= proportion *
                                              distanceAway(i, j, numPixels[k]);
                                    else
                                        if (numPixelsBold[k][i][j])
                                            prob[k] -= proportion;
                                        else
                                            prob[k] += proportion;
                            }
                    }
                ////To distinguish eight's from three's, test lower left
                //bool lowerLeftMarked = false;
                //for (int x = 0; x < input.Width / 3; ++x)
                //    if (input.GetPixel(x, 3 * (input.Height + 1) / 4).R > 155)
                //        lowerLeftMarked = true;
                //if (lowerLeftMarked)//more likely to be eight, but don't
                //    prob[3] -= 200; //heighten eight above the rest
                //else
                //    prob[8] -= 200;
                ////Console.Write("   is a eight: {0}   ", lowerLeftMarked);

                double maxProb = -9999;
                int likelyNum = -1;
                for (int i = 0; i < 3; ++i)
                    if (prob[i] > maxProb)
                    {
                        maxProb = prob[i];
                        likelyNum = i;
                    }

                //for (int n = 0; n < 3; ++n)
                //    Console.WriteLine("{0} prob is: {1}", n, prob[n]);

                if (maxProb < 0)
                    return ""; //nothing likely was found

                switch (likelyNum)
                {
                    case(0) :
                        return "3";
                    case(1) :
                        return "7";
                    case(2) :
                        return "8";
                    default :
                        return "";
                }
            }

            //Finds how far a misplaced pixel is from it is supposed to be
            private int distanceAway(int badX, int badY, bool[][] master)
            {
                int distance = 0;
                int x = badX,
                    y = badY;

                //check ever-increasing loops around (badX, badY)
                while (distance < 20)
                {
                    --x; //start looking on a bigger square
                    --y;
                    ++distance;
                    for (int n = 0; n < distance * 2; ++n)
                    {
                        ++x;
                        if (x > 0 && x < master.Length &&
                            y > 0 && y < master[x].Length && master[x][y])
                            return distance;
                    }
                    for (int n = 0; n < distance * 2; ++n)
                    {
                        ++y;
                        if (x > 0 && x < master.Length &&
                            y > 0 && y < master[x].Length && master[x][y])
                            return distance;
                    }
                    for (int n = 0; n < distance * 2; ++n)
                    {
                        --x;
                        if (x > 0 && x < master.Length &&
                            y > 0 && y < master[x].Length && master[x][y])
                            return distance;
                    }
                    for (int n = 0; n < distance * 2; ++n)
                    {
                        --y;
                        if (x > 0 && x < master.Length &&
                            y > 0 && y < master[x].Length && master[x][y])
                            return distance;
                    }
                }
                return distance;
            }
        }

        //Provides helpful mouse funtions
        private void moveCursor(Point p)
        {
            SetCursorPos(p.X, p.Y);
        }
        private Point getCursor()
        {
            Point p = new Point();
            GetCursorPos(ref p);
            return p;
        }
        private void leftClick()
        {
            mouse_event(0x0002, 0, 0, 0, new System.IntPtr());
            mouse_event(0x0004, 0, 0, 0, new System.IntPtr());
        }
        private void rightClick()
        {
            mouse_event(0x0008, 0, 0, 0, new System.IntPtr());
            mouse_event(0x00010, 0, 0, 0, new System.IntPtr());
        }

        //Copies the screen info to the scr bitmap
        private void getScreen()
        {
            if (scr != null)
                scr.Dispose();
            scr = GDI32.CaptureScreen();
        }
        private Color getPixel(Point p)
        {
            return scr.GetPixel(p.X, p.Y);
        }
        private Color getPixel(int x, int y)
        {
            return scr.GetPixel(x,y);
        }
        bool validPixel(Point p)
        {
            return (0 <= p.X && p.X < scr.Width) &&
                   (0 <= p.Y && p.Y < scr.Height);
        }

        // An edge-checker for the upper-left corner
        bool isWhiteUL(Color input)
        {
            return isWhite(input, 70);
        }
        // An edge-checker for the lower-right corner
        bool isWhiteLR(Color input)
        {
            return isWhite(input, 73);
        }
        bool isWhite(Color input, int whiteValue)
        {
            return input.R > whiteValue &&
                   input.G > whiteValue &&
                   input.B > whiteValue;
        }
        //(these values are experimentally the best, but could be changed)
        bool isBackground(Color c)
        {
            //check if the piece is blue-gray
            return (c.R < c.G + 10 && c.G < c.B + 10 &&
                c.G - c.R < 20 && c.B - c.G < 45);
        }

        //Gives the location at, and dimensions of, a slice of a number in
        //the game, to analyze the color of the number
        private void numFineImageSelect(Point pnum,                 //input
                     ref Point pixel, ref int sizex, ref int sizey) //output
        {
            pixel = new Point((int)Math.Round((pnum.X * cellDimDbl +
                                               leftCol + cellDimDbl / 2.7)),
                              (int)Math.Round((pnum.Y * cellDimDbl +
                                               topHeight + cellDimDbl / 2.5)));
            sizex = (int)Math.Round(cellDimDbl / 2.2);
            sizey = (int)Math.Round(cellDimDbl / 5);
        }
        //Gives the average colors (use for the 5 darkest pixels)
        private Color averageColor(List<Color> colors)
        {
            colors = darkestFive(colors);
            List<int> r = new List<int>(),
                      g = new List<int>(),
                      b = new List<int>();
            foreach (Color c in colors)
            {
                r.Add(c.R);
                g.Add(c.G);
                b.Add(c.B);
            }
            return Color.FromArgb(average(r), average(g), average(b));
        }
        //The five pixels that are the most rich color--the least averaged
        //with the background
        private List<Color> darkestFive(List<Color> colors)
        {
            List<Color> ans = new List<Color>();
            for (int i = 0; i < 5 && colors.Count > 0; i++)
            {
                int min = bright(colors.ElementAt(0));
                int minIndex = 0;
                for (int j = 1; j < colors.Count; j++)
                {
                    Color c = colors.ElementAt(j);
                    if (bright(c) < min)
                    {
                        min = bright(c);
                        minIndex = j;
                    }
                }
                ans.Add(colors.ElementAt(minIndex));
                colors.RemoveAt(minIndex);
            }
            return ans;
        }
        private int bright(Color c)
        {
            return c.R + c.G + c.B;
        }
        private int average(List<int> list)
        {
            int sum = 0;
            foreach (int i in list)
                sum += i;
            return sum / list.Count;
        }
        //Given a space, throws out the background, and returns the number, 
        //from 0-8 or 0-6 depending on DISTINGUISH_378 (because 3's, 7's and
        //8's are all the same color, they are distinguished by looking at the
        //shape of the pixels, which can be avoided by assuming them all to be
        //3's)
        //Unclicked spaces return a -1, while flags currently return -1 or 3
        //A future update would distinguish flags to be -2
        private int pointToNum(Point pnum)
        {
            //Find the location of the pixels for this number
            Point ploc = new Point();
            int dx = 0, dy = 0;
            numFineImageSelect(pnum, ref ploc, ref dx, ref dy);

            int pixelTotal = dx * dy;
            int foregroundPixels = 0;
            List<Color> colors = new List<Color>();

            //Copy colors, and also check how much is colored "unclicked"
            for (int y = ploc.Y; y < dy + ploc.Y; ++y)
                for (int x = ploc.X; x < dx + ploc.X; ++x)
                    if (!isBackground(getPixel(x,y)))
                    {
                        foregroundPixels++;
                        colors.Add(getPixel(x,y));
                    }

            if (foregroundPixels > 0.9 * pixelTotal)
                return -1; //all foreground means it is unclicked
            if (foregroundPixels < 0.2 * pixelTotal)
                return 0; //all background means it is blank

            int num = colorToNum(averageColor(colors));

            //3's, 7's and 8's have the same color, so potentially analyze
            if (num == 3 && DISTINGUISH_378) 
            {
                //Create a black-background/white-text image to analyze
                cellNumberSelect(ref ploc, ref dx, ref dy);
                Bitmap numberPic = new Bitmap(dx, dy);
                for (int y = ploc.Y; y < dy + ploc.Y; ++y)
                    for (int x = ploc.X; x < dx + ploc.X; ++x)
                        if (!isBackground(getPixel(x, y)))
                            numberPic.SetPixel(x - ploc.X, y - ploc.Y,
                                               Color.White);

                num = System.Convert.ToInt32(myOCR.imageTo378(numberPic), 10);
            }
            return num;
        }
        private int colorToNum(Color c)
        {
            if (60 < c.R && c.R < 70 &&
                 75 < c.G && c.G < 85 &&
                180 < c.B && c.B < 200)
                return 1;
            if (20 < c.R && c.R < 34 &&
                100 < c.G && c.G < 110 &&
                             c.B < 10)
                return 2;
            if (150 < c.R && c.R < 180 &&
                             c.G < 15 &&
                             c.B < 15)
                return 3;
            if (c.R < 10 &&
                             c.G < 10 &&
                110 < c.B && c.B < 130)
                return 4;
            if (110 < c.R && c.R < 125 &&
                             c.G < 10 &&
                             c.B < 10)
                return 5;
            if (c.R < 20 &&
                100 < c.G && c.G < 125 &&
                110 < c.B && c.B < 140)
                return 6;
            //(maybe TODO) if it's a flag it might go here
            return -1; //it's a unclicked (unknown/flag)
        }
        //Given a numFineImageSelect, changes the selection to cell's 3/7/8
        private void cellNumberSelect(ref Point ploc, ref int dx, ref int dy)
        {
            //Shrink the box horizontally so not horizontally beyond number
            bool leftMarked = false;
            while (!leftMarked)
            {
                for (int y = ploc.Y; y < dy + ploc.Y; ++y)
                    if (!isBackground(getPixel(ploc.X, y)))
                        leftMarked = true;
                    else ++ploc.X;
            }
            bool rightMarked = false;
            while (!rightMarked)
            {
                for (int y = ploc.Y; y < dy + ploc.Y; ++y)
                    if (!isBackground(getPixel(ploc.X + dx - 1, y)))
                        rightMarked = true;
                    else --dx;
            }

            bool keepGoing = true;
            //Expand the box in each direction
            while (keepGoing)
            {
                keepGoing = false;
                //left
                for (int y = ploc.Y; y < dy + ploc.Y; ++y)
                    if (!isBackground(getPixel(ploc.X - 1, y)))
                    {
                        keepGoing = true;
                        --ploc.X;
                    }
                //right
                for (int y = ploc.Y; y < dy + ploc.Y; ++y)
                    if (!isBackground(getPixel(ploc.X + dx, y)))
                    {
                        keepGoing = true;
                        ++dx;
                    }
                //top
                for (int x = ploc.X; x < dx + ploc.X; ++x)
                    if (!isBackground(getPixel(x, ploc.Y - 1)))
                    {
                        keepGoing = true;
                        --ploc.Y;
                    }
                //bottom
                for (int x = ploc.X; x < dx + ploc.X; ++x)
                    if (!isBackground(getPixel(x, ploc.Y +dy)))
                    {
                        keepGoing = true;
                        ++dy;
                    }
            }
        }

        //Looks for the box with mines remaining
        private void mineBox(ref Point p, ref int dx, ref int dy)
        {
            //The mineBox is always 81% of the way across the grid
            p.X = (int)(0.81 * (width)) + leftCol;
            p.Y = bottomHeight + 5;

            //Find the top left corner
            while (getPixel(p).R > 150)
                p.Y++;
            p.Y += Math.Max((int)(cellDimDbl / 6), 3);
            while (getPixel(p).R < 150)
                p.X--;
            p.X += 6;

            //Find the bottom right corner
            Point br = new Point(p.X, p.Y);
            br.X++;
            br.Y++;
            while (getPixel(br).R < 150)
                br.X++;
            br.X--;

            br.X--;
            while (getPixel(br).R < 150)
                br.Y++;
            br.Y--;
            br.X++;

            br.X -= 4;

            dx = br.X - p.X;
            dy = br.Y - p.Y;
        }
        //Gets two to three numbers from mineBox
        private void mineBoxNumbers(ref Point p1, ref int dx1, ref int dy1,
                                    ref Point p2, ref int dx2,
                                    ref Point p3, ref int dx3)
        {
            Point p = new Point();
            int dx = 0, dy = 0;
            mineBox(ref p, ref dx, ref dy);

            //Find the height of the numbers in general by going across rows
            int[] rows = new int[dy];
            for (int y = 0; y < dy; ++y)
                for (int x = 0; x < 7 * dx / 8; ++x)
                    if (getPixel(p.X + x, p.Y + y).R > 65)
                        rows[y]++;
            int bottom = dy - 1;
            while (rows[bottom] == 0)
                bottom--;
            int top = bottom;
            bottom++;
            while (rows[top] > 0)
                top--;
            top++;
            dy1 = bottom - top;
            p1.Y = top + p.Y;
            p2.Y = top + p.Y;
            p3.Y = top + p.Y;

            //Find the heights of each column to allow for number seperation
            int[] heights = new int[dx];
            for (int x = 0; x < dx; ++x)
                for (int y = top; y < bottom; ++y)
                    if (getPixel(p.X + x, p.Y + y).R > 65)
                        heights[x]++;

            //Seperate out each number into blocks, splitting when there is 
            //one or less pixels between numbers
            int X = 0;
            while (heights[X] < 2)
            {
                X++;
            }
            p1.X = X;
            while (heights[X] >= 2)
            {
                X++;
            }
            dx1 = X - p1.X;
            while (heights[X] < 2)
            {
                X++;
            }
            p2.X = X;
            while (heights[X] >= 2)
            {
                X++;
            }
            dx2 = X - p2.X;
            while (X < heights.Length && heights[X] < 2)
                X++;
            p3.X = X;
            while (X < heights.Length && heights[X] >= 2)
                X++;
            dx3 = X - p3.X;

            p1.X += p.X;
            p2.X += p.X;
            p3.X += p.X;
        }
        //Finds out how many mines are left in the game
        private int minesRemaining()
        {
            //Find the locations of numbers 1, 2 and 3--the third may be a stub
            Point p1 = new Point(), p2 = new Point(), p3 = new Point();
            int dx1 = 0, dx2 = 0, dx3 = 0, dy1 = 0;
            mineBoxNumbers(ref p1, ref dx1, ref dy1,
                           ref p2, ref dx2,
                           ref p3, ref dx3);

            //Make images of the numbers, to analyze
            Bitmap num1 = new Bitmap(dx1, dy1);
            for (int y = p1.Y; y < p1.Y + dy1; ++y)
                for (int x = p1.X; x < p1.X + dx1; ++x)
                    if (getPixel(x, y).R > 65)
                        num1.SetPixel(x - p1.X, y - p1.Y, Color.White);

            Bitmap num2 = new Bitmap(dx2, dy1);
            for (int y = p2.Y; y < p2.Y + dy1; ++y)
                for (int x = p2.X; x < p2.X + dx2; ++x)
                    if (getPixel(x, y).R > 65)
                        num2.SetPixel(x - p2.X, y - p2.Y, Color.White);

            if (dx3 < 1) //if the image is too small, make a stubby line
                dx3 = 1;
            Bitmap num3 = new Bitmap(dx3, dy1);
            for (int y = p3.Y; y < p3.Y + dy1; ++y)
                for (int x = p3.X; x < p3.X + dx3; ++x)
                    if (getPixel(x, y).R > 65)
                        num3.SetPixel(x - p3.X, y - p3.Y, Color.White);

            string mines = myOCR.imageToText(num1) +
                            myOCR.imageToText(num2) +
                            myOCR.imageToText(num3);
            return System.Convert.ToInt32(mines, 10);
        }

        //public functions that are useful for debugging
        public void printBoard()
        {
            Point p = new Point(0, 0);
            for (; p.Y < getHeight(); ++p.Y)
            {
                for (p.X = 0; p.X < getWidth(); ++p.X)
                {
                    int num = getNum(p);
                    if (num > 0 && num < 9)
                        Console.Write("" + num + " ");
                    else if (num == -1)
                        Console.Write("- ");
                    else
                        Console.Write("  ");
                }
                Console.WriteLine();
            }
        }
        public void printParsed()
        {
            Point p = new Point(0, 0);
            for (; p.Y < getHeight(); ++p.Y)
            {
                for (p.X = 0; p.X < getWidth(); ++p.X)
                {
                    if (parsed[p.X, p.Y])
                        Console.Write("  ");
                    else
                        Console.Write("? ");
                }
                Console.WriteLine();
            }
        }
        //Lets the user see clicks are performed correctly
        //(right clicks are used to check every cell)
        public void clickCheck()
        {
            Point p = new Point(0, 0);
            for (; p.Y < getHeight(); ++p.Y)
            {
                for (p.X = 0; p.X < getWidth(); ++p.X)
                {
                    clickR(p);
                }
            }
        }

        public void debugger()//has miscellanious functions
        {
            testImageRecognition();
        }

        private void testImageRecognition()
        {
            Bitmap[] big = new Bitmap[10], med = new Bitmap[10], sma = new Bitmap[10];
            Bitmap[] biB = new Bitmap[10], meB = new Bitmap[10], smB = new Bitmap[10];
            for (int n = 0; n < 10; ++n)
            {
                big[n] = new Bitmap(n + ".bmp");
                med[n] = new Bitmap(n + "M.bmp");
                sma[n] = new Bitmap(n + "S.bmp");
            }
            for (int z = 0; z < 3; ++z)
            {
                int n;
                if (z == 0) n = 3;
                else if (z == 1) n = 7;
                else n = 8;
                biB[z] = new Bitmap("Bold" + n + ".bmp");
                meB[z] = new Bitmap("Bold" + n + "M.bmp");
                smB[z] = new Bitmap("Bold" + n + "S.bmp");
            }
            for (int n = 0; n < 10; ++n)
            {
                Console.WriteLine("Big {0} is " + myOCR.imageToText(big[n]), n);
                Console.WriteLine("Med {0} is " + myOCR.imageToText(med[n]), n);
                Console.WriteLine("Sma {0} is " + myOCR.imageToText(sma[n]), n);
                Console.ReadLine();
            }
            for (int z = 0; z < 3; ++z)
            {
                int n;
                if (z == 0) n = 3;
                else if (z == 1) n = 7;
                else n = 8;
                Console.WriteLine("BigBold {0} is " + myOCR.imageTo378(biB[z]), n);
                Console.WriteLine("MedBold {0} is " + myOCR.imageTo378(meB[z]), n);
                Console.WriteLine("SmaBold {0} is " + myOCR.imageTo378(smB[z]), n);
                Console.ReadLine();
            }
        }
        private void makeImageAt(Point p, int sizex, int sizey)
        {
            Bitmap ans = new Bitmap(sizex, sizey);
            for (int y = p.Y; y < sizey + p.Y; y++)
                for (int x = p.X; x < sizex + p.X; x++)
                    ans.SetPixel(x - p.X, y - p.Y, getPixel(x, y));
            GDI32.SaveImage(ans);
        }
    }

    
    //  TODO eventually get rid of old code 
     /* getScreen();
      int SIZE = 650;
      Point mousePos = getCursor();
      Bitmap ans = new Bitmap(SIZE, SIZE), ans2 = new Bitmap(SIZE, SIZE),
           ans3 = new Bitmap(SIZE, SIZE), ans4 = new Bitmap(SIZE, SIZE);
      for (int y = mousePos.Y; y < SIZE + mousePos.Y; y++)
      {
          for (int x = mousePos.X; x < SIZE + mousePos.X; x++)
              if (scr.GetPixel(x, y).B > 170)
                  ans.SetPixel(x - mousePos.X, y - mousePos.Y, Color.DarkBlue);
              else if (scr.GetPixel(x, y).B > 160)
                  ans.SetPixel(x - mousePos.X, y - mousePos.Y, Color.Blue);
              else if (scr.GetPixel(x, y).B > 150)
                  ans.SetPixel(x - mousePos.X, y - mousePos.Y, Color.LightBlue);
              else
                  ans.SetPixel(x - mousePos.X, y - mousePos.Y, Color.White);
              //ans.SetPixel(x - mousePos.X, y - mousePos.Y, scr.GetPixel(x, y));
              //Console.Write(color(scr.GetPixel(x, y)) + ", ");
          Console.WriteLine();
      }
      for (int y = mousePos.Y; y < SIZE + mousePos.Y; y++)
      {
          for (int x = mousePos.X; x < SIZE + mousePos.X; x++)
              if (scr.GetPixel(x, y).R < 110)
                  ans2.SetPixel(x - mousePos.X, y - mousePos.Y, Color.DarkBlue);
              else if (scr.GetPixel(x, y).R < 120)
                  ans2.SetPixel(x - mousePos.X, y - mousePos.Y, Color.Blue);
              else if (scr.GetPixel(x, y).R < 130)
                  ans2.SetPixel(x - mousePos.X, y - mousePos.Y, Color.LightBlue);
              else
                  ans2.SetPixel(x - mousePos.X, y - mousePos.Y, Color.White);
          //ans.SetPixel(x - mousePos.X, y - mousePos.Y, scr.GetPixel(x, y));
          //Console.Write(color(scr.GetPixel(x, y)) + ", ");
          Console.WriteLine();
      }

      for (int y = mousePos.Y; y < SIZE + mousePos.Y; y++)
      {
          for (int x = mousePos.X; x < SIZE + mousePos.X; x++)
              if (scr.GetPixel(x, y).B > 160 && scr.GetPixel(x, y).R >= 120)
                  ans3.SetPixel(x - mousePos.X, y - mousePos.Y, Color.Blue);
              else if (scr.GetPixel(x, y).B <= 160 && scr.GetPixel(x, y).R < 120)
                  ans3.SetPixel(x - mousePos.X, y - mousePos.Y, Color.Red);
              else if (scr.GetPixel(x, y).B > 160 && scr.GetPixel(x, y).R < 120)
                  ans3.SetPixel(x - mousePos.X, y - mousePos.Y, Color.Purple);
              else
                  ans3.SetPixel(x - mousePos.X, y - mousePos.Y, Color.White);
      }
      for (int y = mousePos.Y; y < SIZE + mousePos.Y; y++)
      {
          for (int x = mousePos.X; x < SIZE + mousePos.X; x++)
              if (scr.GetPixel(x, y).R > 73 && scr.GetPixel(x, y).G > 73
                  && scr.GetPixel(x, y).B > 73)
                  ans4.SetPixel(x - mousePos.X, y - mousePos.Y, Color.White);
              else if (scr.GetPixel(x, y).R > 70 && scr.GetPixel(x, y).G > 70
              && scr.GetPixel(x, y).B > 70)
                  ans4.SetPixel(x - mousePos.X, y - mousePos.Y, Color.LightGray);
              else if (scr.GetPixel(x, y).R > 69 && scr.GetPixel(x, y).G > 69
              && scr.GetPixel(x, y).B > 69)
                  ans4.SetPixel(x - mousePos.X, y - mousePos.Y, Color.Gray);
              else if (scr.GetPixel(x, y).R > 68 && scr.GetPixel(x, y).G > 68
              && scr.GetPixel(x, y).B > 68)
                  ans4.SetPixel(x - mousePos.X, y - mousePos.Y, Color.DimGray);
      }
      for (int y = mousePos.Y; y < SIZE + mousePos.Y; y++)
      {
          for (int x = mousePos.X; x < SIZE + mousePos.X; x++)
              if (isWhiteLR(scr.GetPixel(x, y)))
                  ans3.SetPixel(x - mousePos.X, y - mousePos.Y, Color.White);
      }
      for (int y = mousePos.Y; y < SIZE + mousePos.Y; y++)

          for (int x = mousePos.X; x < SIZE + mousePos.X; x++)
              if (isWhiteUL(scr.GetPixel(x, y)))
                  ans4.SetPixel(x - mousePos.X, y - mousePos.Y, Color.White);

      GDI32.SaveImage(ans);
      GDI32.SaveImage(ans2);
      GDI32.SaveImage(ans3);
      GDI32.SaveImage(ans4);

      int WIDTH = width;
      int HEIGHT = height;
      Bitmap ans4 = new Bitmap(WIDTH, HEIGHT);
      for (int y = topHeight; y < HEIGHT + topHeight; y++)
          for (int x = leftCol; x < WIDTH + leftCol; x++)
              ans4.SetPixel(x - leftCol, y - topHeight, getPixel(new Point(x,y)));
      GDI32.SaveImage(ans4);

      int SIZE = Math.Max(WIDTH, HEIGHT) + 60;
      Bitmap ans5 = new Bitmap(SIZE, SIZE);
      for (int y = topHeight - 30; y < SIZE + topHeight - 30; y++)
          for (int x = leftCol - 30; x < SIZE + leftCol - 30; x++)
              if (isWhite(scr.GetPixel(x, y), 40))
                  ans5.SetPixel(x - leftCol + 30, y - topHeight + 30, Color.White);
      GDI32.SaveImage(ans5);
    

      int mode(List<int> input)
      {
          int max = 0;
          Dictionary<int, int> values = new Dictionary<int, int>();

          foreach(int i in input)
          {
              int temp = 0;
              if (values.TryGetValue(i, out temp))
                  temp++;
              else
                  values.Add(i, 0);
          }
          max = values.Max().Key;
          Console.WriteLine("Max value is " + max);

          return max;
      }
     for (int y = 0; y < getHeight(); ++y)
                  for (int x = 0; x < getWidth(); ++x)
                  {
                      Point loc = new Point(x, y);
                      Point pixel = new Point();
                      int dx = 0, dy = 0;
                      numFineImageSelect(loc, ref pixel, ref dx, ref dy);
                      if (getNum(loc) == 3)
                          cellNumberSelect(ref pixel, ref dx, ref dy);
                      Bitmap b = new Bitmap(dx, dy);
                      for (int Y = pixel.Y; Y < dy + pixel.Y; ++Y)
                          for (int X = pixel.X; X < dx + pixel.X; ++X)
                              if (!isBackground(scr.GetPixel(X, Y)))
                                  b.SetPixel(X - pixel.X, Y - pixel.Y, //getPixel(new Point(X, Y)));
                                                                       Color.White);
                      GDI32.SaveImage(b);
                  }
            
              Bitmap ans2 = new Bitmap(width, height);
              for (int y = topHeight; y < height + topHeight; y++)
                  for (int x = leftCol; x < width + leftCol; x++)
                      if (isBackground(scr.GetPixel(x, y)))
                          ans2.SetPixel(x - leftCol, y - topHeight, Color.White);
              GDI32.SaveImage(ans2);

              Point p = new Point(0, 0);
              // /*
              Bitmap ans3 = new Bitmap(getWidth(), getHeight());
              for (; p.Y < getHeight(); p.Y++)
                  for (p.X = 0; p.X < getWidth(); p.X++)
                      ans3.SetPixel(p.X, p.Y, pointToColor(p));
              GDI32.SaveImage(ans3);
            
              Point p = new Point();
              int dx = 0, dy = 0;
              mineBox(ref p, ref dx, ref dy);
              Bitmap ans4 = new Bitmap(dx, dy);
              for (int y = p.Y; y < p.Y + dy; y++)
                  for (int x = p.X; x < p.X + dx; x++)
                      ans4.SetPixel(x - p.X, y - p.Y, getPixel(new Point(x, y)));
              GDI32.SaveImage(ans4);

              Bitmap ans5 = new Bitmap(dx, dy);
              for (int y = p.Y; y < p.Y + dy; y++)
                  for (int x = p.X; x < p.X + dx; x++)
                      if (getPixel(new Point(x, y)).R > 65)
                          ans5.SetPixel(x - p.X, y - p.Y, Color.White);
              GDI32.SaveImage(ans5);
            
              Point p1 = new Point(), p2 = new Point(), p3 = new Point();
              int dx1 = 0, dx2 = 0, dx3 = 0, dy1 = 0;
              mineBoxNumbers(ref p1, ref dx1, ref dy1, ref p2, ref dx2, ref p3, ref dx3);


              Bitmap ans6 = new Bitmap(dx1, dy1);
              for (int y = p1.Y; y < p1.Y + dy1; y++)
                  for (int x = p1.X; x < p1.X + dx1; x++)
                      if (getPixel(new Point(x, y)).R > 65)
                          ans6.SetPixel(x - p1.X, y - p1.Y, Color.White);
              GDI32.SaveImage(ans6);

              Bitmap ans7 = new Bitmap(dx2, dy1);
              for (int y = p2.Y; y < p2.Y + dy1; y++)
                  for (int x = p2.X; x < p2.X + dx2; x++)
                      if (getPixel(new Point(x, y)).R > 65)
                          ans7.SetPixel(x - p2.X, y - p2.Y, Color.White);
              GDI32.SaveImage(ans7);

              if (dx3 > 2)
              {
                  Bitmap ans8 = new Bitmap(dx3, dy1);
                  for (int y = p3.Y; y < p3.Y + dy1; y++)
                      for (int x = p3.X; x < p3.X + dx3; x++)
                          if (getPixel(new Point(x, y)).R > 65)
                              ans8.SetPixel(x - p3.X, y - p3.Y, Color.White);
                  GDI32.SaveImage(ans8);
              }

            
              Console.WriteLine("First number is" + myOCR.imageToText(ans6));
              Console.WriteLine("Second number is" + myOCR.imageToText(ans7));

            
              Bitmap[] bolds = new Bitmap[3];
              bolds[0] = new Bitmap("Bold3.bmp");
              bolds[1] = new Bitmap("Bold7.bmp");
              bolds[2] = new Bitmap("Bold8.bmp");

              for(int n = 0; n < 3; ++n)
              {
                  Console.WriteLine("numsBold[{0}] = ", n);
                  for(int y = 0; y < bolds[n].Height; ++y)
                  {
                      Console.Write("\"");
                      for(int x = 0; x < bolds[n].Width; ++x)
                          if(bolds[n].GetPixel(x,y).R>155)
                              Console.Write(" ");
                          else
                              Console.Write("*");
                      Console.Write("\" +\n");
                  }
              }
     */
}
