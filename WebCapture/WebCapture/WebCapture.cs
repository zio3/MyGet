using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebCapture
{
    public class WebCapture
    {
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern Int32 SendMessage(IntPtr hWnd, Int32 Msg, IntPtr wParam, int lParam);
        public static Bitmap Capture(string url, int width, int height)
        {
            WebBrowser browser = new WebBrowser();
            browser.Width = width;
            browser.Height = height;
            browser.ScrollBarsEnabled = false;

            browser.Navigate(url);

            // ↓これをしとかないと WebBrowser.IsBusy が true のままになり、ドキュメント読み込みが完了しない・・・
            browser.Refresh();
            Application.DoEvents();

            // リソースの読み込みを待機
            while (browser.IsBusy || browser.ReadyState != WebBrowserReadyState.Complete)
            {
                Thread.Sleep(100);

                // ↓を入れないと WebBrowser.IsBusy が更新されない
                Application.DoEvents();
            }

            // WebBrowser をキャプチャ
            Bitmap bmp = new Bitmap(browser.Width, browser.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr dc = g.GetHdc();
                SendMessage(browser.Handle, 0x0317/* WM_PRINT */, dc, (0x04 | 0x10));
                g.ReleaseHdc(dc);
            }
            return bmp;
        }
    }
}
