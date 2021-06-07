using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using Tesseract;

namespace UmaUmaTeio
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var handle = WindowsAPI.GetHandle("UnityWndClass", "umamusume");
            var bitmap = WindowsAPI.GetCaptureImage(handle);

            bitmap?.Save("save.png", System.Drawing.Imaging.ImageFormat.Png);

            // 切り出しテスト
            var snipBitmap = bitmap?.Clone(new Rectangle(50, 603, 363, 20), bitmap.PixelFormat);

            var exePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            using (var tesseract = new Tesseract.TesseractEngine(System.IO.Path.Combine(exePath, "tessdata"), "jpn"))
            {
                tesseract.SetVariable("tessedit_char_whitelist", "0123456789");

                var pix = PixConverter.ToPix(bitmap);

                var p = tesseract.Process(pix, PageSegMode.SingleLine);

                Console.WriteLine(p.GetText());
            }
        }
    }
}
