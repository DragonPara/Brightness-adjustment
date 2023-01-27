using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace light
{
    public struct RAMP
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public UInt16[] Red;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public UInt16[] Green;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public UInt16[] Blue;
    }
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("gdi32.dll")]
        public static extern bool GetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);
        [DllImport("gdi32.dll")]
        public static extern bool SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);


        private Icon light = Properties.Resources.sun_2;
        private Icon black = Properties.Resources.sun;
        private bool _status = true;
        private bool _shine = false; //shine 闪烁
        private short BRIGHTNESS_MAX = 100;
        private short BRIGHTNESS_MIN = 10;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }

        private void 显示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            short max, min, cur;
            max = min = cur = 0;
            GetBrightness(ref min, ref cur, ref max);
            cur += 10;
            if (cur > BRIGHTNESS_MAX)
            {
                notifyIcon1.ShowBalloonTip(5000, "不能再亮了哥","最大亮度100", ToolTipIcon.Info);
                return;
            }
            SetBrightness(cur); 
            notifyIcon1.ShowBalloonTip(5000, "当前亮度信息", "当前亮度" + cur + "|最小亮度" + min + "|最大亮度" + max, ToolTipIcon.Info);
        }

        private void 隐藏ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = false;
            short max, min, cur;
            max = min = cur = 0;
            GetBrightness(ref min, ref cur, ref max);
            cur -= 10;
            if (cur < BRIGHTNESS_MIN)
            {
                notifyIcon1.ShowBalloonTip(5000, "不能再暗了哥", "最小亮度10", ToolTipIcon.Info);
                return;
            }
            SetBrightness(cur);
            notifyIcon1.ShowBalloonTip(5000, "当前亮度信息", "当前亮度" + cur + "|最小亮度" + min + "|最大亮度" + max, ToolTipIcon.Info);
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_status)
            {
                notifyIcon1.Icon = light;
            }
            else
            {
                notifyIcon1.Icon = black; 
            }
            _status= !_status;
        }

        private void 闪烁ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!_shine)
            {
                _shine = true;
                timer1.Enabled = true;
                timer1.Start(); 
            }
            else
            {
                notifyIcon1.Icon = light;
                _shine = false;
                timer1.Stop();
                notifyIcon1.ShowBalloonTip(5000, "停止闪烁", "已停止闪烁图标", ToolTipIcon.Info);
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {

            this.Visible = false;
        }
        private double CalColorGammaVal(ushort[] line)
        {
            var max = line.Max();
            var min = line[0];
            var index = Array.FindIndex(line, n => n == max);
            var gamma = Math.Round((((double)(max - min) / index) / 255), 2);
            return gamma;
        }
        private double CalAllGammaVal(RAMP ramp)
        {
            return Math.Round(((CalColorGammaVal(ramp.Blue) + CalColorGammaVal(ramp.Red) +
                                CalColorGammaVal(ramp.Green)) / 3), 2);
        }
        public bool GetBrightness(ref short minBrightness, ref short currentBrightness,
            ref short maxBrightness)
        {
            var handle = GetDC(IntPtr.Zero);
            //0-50 亮度变化太小，所以从50开始
            minBrightness = BRIGHTNESS_MIN;
            maxBrightness = BRIGHTNESS_MAX;
            var ramp = default(RAMP);
            var deviceGammaRamp = GetDeviceGammaRamp(handle, ref ramp);
            currentBrightness = (short)((deviceGammaRamp ? CalAllGammaVal(ramp) : 0.5) * 100);
            return deviceGammaRamp;
        }
        public bool SetBrightness(short brightness)
        {
            var handle = GetDC(IntPtr.Zero);
            double value = (double)brightness / 100;
            RAMP ramp = default(RAMP);
            ramp.Red = new ushort[256];
            ramp.Green = new ushort[256];
            ramp.Blue = new ushort[256];

            for (int i = 1; i < 256; i++)
            {
                var tmp = (ushort)(i * 255 * value);
                ramp.Red[i] = ramp.Green[i] = ramp.Blue[i] = Math.Max(ushort.MinValue, Math.Min(ushort.MaxValue, tmp));
            }

            var deviceGammaRamp = SetDeviceGammaRamp(handle, ref ramp);
            return deviceGammaRamp;
        }
    }
}
