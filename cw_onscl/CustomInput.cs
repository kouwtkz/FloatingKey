using System.Windows.Input.Custom;
using System.Runtime.InteropServices; // DllImport
using System.Windows.Interop;

namespace System.Windows.Input.Custom
{
    class CustomInput
    {
        // お借りしました http://pgcenter.web.fc2.com/contents/csharp_sendinput.html
        // マウスイベント(mouse_eventの引数と同様のデータ)
        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public int dwExtraInfo;
        };

        // キーボードイベント(keybd_eventの引数と同様のデータ)
        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public short wVk;
            public short wScan;
            public int dwFlags;
            public int time;
            public int dwExtraInfo;
        };

        // ハードウェアイベント
        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        };

        // 各種イベント(SendInputの引数データ)
        [StructLayout(LayoutKind.Explicit)]
        private struct INPUT
        {
            [FieldOffset(0)] public int type;
            [FieldOffset(4)] public MOUSEINPUT mi;
            [FieldOffset(4)] public KEYBDINPUT ki;
            [FieldOffset(4)] public HARDWAREINPUT hi;
        };

        // キー操作、マウス操作をシミュレート(擬似的に操作する)
        [DllImport("user32.dll")]
        private extern static void SendInput(
            int nInputs, ref INPUT pInputs, int cbsize);

        // 仮想キーコードをスキャンコードに変換
        [DllImport("user32.dll", EntryPoint = "MapVirtualKeyA")]
        private extern static int MapVirtualKey(
            int wCode, int wMapType);

        private const int INPUT_MOUSE = 0;                  // マウスイベント
        private const int INPUT_KEYBOARD = 1;               // キーボードイベント
        private const int INPUT_HARDWARE = 2;               // ハードウェアイベント
        private const int KEYEVENTF_KEYDOWN = 0x0;          // キーを押す
        private const int KEYEVENTF_KEYUP = 0x2;            // キーを離す
        private const int KEYEVENTF_EXTENDEDKEY = 0x1;      // 拡張コード

        public bool keybdFlag = true;

        public enum KeySendType
        {
            KeyClick = 0,
            KeyUp = 1,
            KeyDown = 2,
        }

        public void KeySend(Keys key, KeySendType keySendType = KeySendType.KeyClick)
        {
            int sendCount = 0, sendCur = 0;
            bool keydown = false, keyup = false;
            switch (keySendType)
            {
                case KeySendType.KeyClick:
                    sendCount = 2;
                    keydown = true;
                    keyup = true;
                    break;
                case KeySendType.KeyDown:
                    sendCount = 1;
                    keydown = true;
                    break;
                case KeySendType.KeyUp:
                    sendCount = 1;
                    keyup = true;
                    break;
            }
            if (keybdFlag)
            {
                byte bVk = (byte)key;
                byte bScan = (byte)MapVirtualKey((short)key, 0);
                if (keydown)
                {
                    keybd_event(bVk, bScan, KEYEVENTF_KEYDOWN, (UIntPtr)0);
                }
                if (keyup)
                {
                    keybd_event(bVk, bScan, KEYEVENTF_KEYUP, (UIntPtr)0);
                }
            }
            else
            {
                short wVk = (short)key;
                short wScan = (short)MapVirtualKey(wVk, 0);
                INPUT[] inp = new INPUT[sendCount];
                if (keydown)
                {
                    inp[sendCur].type = INPUT_KEYBOARD;
                    inp[sendCur].ki.wVk = wVk;
                    inp[sendCur].ki.wScan = wScan;
                    inp[sendCur].ki.dwFlags = KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYDOWN;
                    inp[sendCur].ki.dwExtraInfo = 0;
                    sendCur++;
                }
                if (keyup)
                {
                    inp[sendCur].type = INPUT_KEYBOARD;
                    inp[sendCur].ki.wVk = wVk;
                    inp[sendCur].ki.wScan = wScan;
                    inp[sendCur].ki.dwFlags = KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP;
                    inp[sendCur].ki.dwExtraInfo = 0;
                    sendCur++;
                }
                // キーボード操作実行
                SendInput(sendCount, ref inp[0], Marshal.SizeOf(inp[0]));
            }
            return;
        }
        // お借りしました http://zourimusi.hatenadiary.jp/entry/20131018/1382093051
        [DllImport("user32.dll")]
        private static extern uint keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        // アクティブにしないウィンドウ処理
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        public static void ChangeWindowActivate(Window win, bool Activate)
        {
            var helper = new WindowInteropHelper(win);
            if (Activate)
            {
                SetWindowLong(helper.Handle, GWL_EXSTYLE, GetWindowLong(helper.Handle, GWL_EXSTYLE)
                    & ~WS_EX_NOACTIVATE);
            } else
            {
                SetWindowLong(helper.Handle, GWL_EXSTYLE, GetWindowLong(helper.Handle, GWL_EXSTYLE)
                    | WS_EX_NOACTIVATE);
            }
        }
    }
}