using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace UmaUmaTeio
{
    /// <summary>
    /// WindowsAPI クラスは、Win32 API の利用をサポートするためのクラスです。
    /// </summary>
    public class WindowsAPI
    {
        #region Struct

        public const int SRCCOPY = 13369376;

        public const int DIB_RGB_COLORS = 0;
        public const int DIB_PAL_COLORS = 1;

        /*
        enum DWMWINDOWATTRIBUTE : uint
        {
            DWMWA_NCRENDERING_ENABLED = 1,
            DWMWA_NCRENDERING_POLICY,
            DWMWA_TRANSITIONS_FORCEDISABLED,
            DWMWA_ALLOW_NCPAINT,
            DWMWA_CAPTION_BUTTON_BOUNDS,
            DWMWA_NONCLIENT_RTL_LAYOUT,
            DWMWA_FORCE_ICONIC_REPRESENTATION,
            DWMWA_FLIP3D_POLICY,
            DWMWA_EXTENDED_FRAME_BOUNDS,
            DWMWA_HAS_ICONIC_BITMAP,
            DWMWA_DISALLOW_PEEK,
            DWMWA_EXCLUDED_FROM_PEEK,
            DWMWA_CLOAK,
            DWMWA_CLOAKED,
            DWMWA_FREEZE_REPRESENTATION,
            DWMWA_LAST
        }
        */
        enum BitmapCompressionMode : uint
        {
            BI_RGB = 0,
            BI_RLE8 = 1,
            BI_RLE4 = 2,
            BI_BITFIELDS = 3,
            BI_JPEG = 4,
            BI_PNG = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public BitmapCompressionMode biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;

            public void Init()
            {
                biSize = (uint)Marshal.SizeOf(this);
            }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
            public byte[] bmiColors;
        }

        /*
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RGBQUAD
        {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            public byte rgbReserved;
        }
        */

        #endregion

        #region Dll Import

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // [DllImport("user32.dll")]
        // private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern int GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, [In, Out] ref RECT rect, [MarshalAs(UnmanagedType.U4)] int cPoints);

        // [DllImport("User32.dll")]
        // private extern static bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

        // [DllImport("dwmapi.dll")]
        // private static extern long DwmGetWindowAttribute(IntPtr hWnd, DWMWINDOWATTRIBUTE dwAttribute, out RECT rect, int cbAttribute);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFO pbmi, uint pila, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport("gdi32.dll")]
        static extern int BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        #endregion

        #region Public Methods

        /// <summary>
        /// 指定した名前のウィンドウのハンドルを取得します。
        /// </summary>
        /// <param name="className">ウィンドウのクラス名。</param>
        /// <param name="windowName">ウィンドウの名前。</param>
        /// <returns>ウィンドウのハンドル。</returns>
        public static IntPtr GetHandle(string className, string windowName)
        {
            var handle = FindWindow(className, windowName);

            return handle;
        }

        /// <summary>
        /// 指定したウィンドウハンドルをキャプチャーした <see cref="Bitmap"/> データを取得します。
        /// </summary>
        /// <param name="handle">ウィンドウのハンドル。</param>
        /// <returns>キャプチャーした <see cref="Bitmap"/> データ。失敗したとき <c>null</c>。</returns>
        public static Bitmap GetCaptureImage(IntPtr handle)
        {
            int result = 0;

            IntPtr desktopDC = IntPtr.Zero;
            IntPtr memoryDC = IntPtr.Zero;
            Bitmap bitmap = null;

            try
            {
                result = GetClientRect(handle, out RECT rect);

                if (result == 0) throw new NullReferenceException($"指定したハンドルのウィンドウ({handle})を発見することができませんでした。");

                result = MapWindowPoints(handle, IntPtr.Zero, ref rect, 2);

                if (result == 0) throw new NullReferenceException($"指定したハンドルのウィンドウ({handle})の座標空間の変換に失敗しました。");
                
                var tempRect = rect;

                desktopDC = GetWindowDC(IntPtr.Zero); // デスクトップの HDC を取得
                
                var header = new BITMAPINFOHEADER()
                {
                    biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER)),
                    biWidth = tempRect.right - rect.left,
                    biHeight = tempRect.bottom - rect.top,
                    biPlanes = 1,
                    biCompression = BitmapCompressionMode.BI_RGB,
                    biBitCount = 24,
                };

                var info = new BITMAPINFO
                {
                    bmiHeader = header,
                };

                var hBitmap = CreateDIBSection(desktopDC, ref info, DIB_RGB_COLORS, out _, IntPtr.Zero, 0);
                
                memoryDC = CreateCompatibleDC(desktopDC);

                var phBitmap = SelectObject(memoryDC, hBitmap);

                BitBlt(memoryDC, 0, 0, header.biWidth, header.biHeight, desktopDC, rect.left, rect.top, SRCCOPY);

                SelectObject(memoryDC, phBitmap);

                bitmap = Bitmap.FromHbitmap(hBitmap, IntPtr.Zero);
            }
            finally
            {
                if (desktopDC != IntPtr.Zero)
                {
                    ReleaseDC(IntPtr.Zero, desktopDC);
                }

                if (memoryDC != IntPtr.Zero)
                {
                    ReleaseDC(handle, memoryDC);
                }
            }

            if (bitmap == null) return null;

            return bitmap;
        }

    }
}
