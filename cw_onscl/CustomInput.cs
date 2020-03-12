using System.Windows.Input.Custom;
using System.Runtime.InteropServices; // DllImport
using System.Windows.Interop;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Timers;

namespace System.Windows.Input.Custom
{
    public class CustomInput
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
            KeyHold = 3,
        }
        public enum KeyInputType
        {
            KeyPress = 0,
            KeyUp = 1,
            KeyDown = 2,
        }
        [DataContract]
        public class KeyData<T> where T : Enum
        {
            [DataMember(Name = "code")]
            public byte Code { get; set; }
            [DataMember(Name = "ctrl")]
            public bool Ctrl { get; set; }
            [DataMember(Name = "alt")]
            public bool Alt { get; set; }
            [DataMember(Name = "shift")]
            public bool Shift { get; set; }
            [DataMember(Name = "win")]
            public bool Win { get; set; }
            [DataMember(Name = "keytype")]
            public T KeyType { get; set; }
            public KeyData() { }
            public KeyData(byte code, T keyType)
            {
                Code = code;
                KeyType = keyType;
            }
            public KeyData(Keys code, T keyType)
            {
                Code = (byte)code;
                KeyType = keyType;
            }
            public KeyData(KeyData<T> keyData)
            {
                Code = keyData.Code;
                Ctrl = keyData.Ctrl;
                Alt = keyData.Alt;
                Shift = keyData.Shift;
                Win = keyData.Win;
                KeyType = keyData.KeyType;
            }
        }

        public InputTimer inputTimer;
        private List<KeyData<KeySendType>> LockKeyStock;
        private bool FlgNextUnLock;
        private Timer DelayTimer;
        public const double DelaySync = 100;
        public CustomInput()
        {
            LockKeyStock = new List<KeyData<KeySendType>>();
            DelayTimer = new Timer(DelaySync);
            DelayTimer.Elapsed += (sender, e) => {
                DelayTimer.Stop();
                if (LockKeyStock.Count > 0) FlgNextUnLock = true;
            };
            inputTimer = new InputTimer(Keys.LButton);
            inputTimer.MethodDown = (sender, e) =>
            {
                if (!FlgNextUnLock)
                {
                    DelayTimer.Start();
                }
            };
            inputTimer.MethodUp = (sender, e) =>
            {
                if (FlgNextUnLock)
                {
                    FlgNextUnLock = false;
                    try
                    {
                        foreach(var data in LockKeyStock) { KeySend(data); }
                    }
                    catch { }
                }
            };
            inputTimer.Start();
        }

        public void KeySend(KeyData<KeySendType> keyData)
        {
            bool keydown = false, keyup = false;
            switch (keyData.KeyType)
            {
                case KeySendType.KeyClick:
                    keydown = true;
                    keyup = true;
                    break;
                case KeySendType.KeyDown:
                    keydown = true;
                    break;
                case KeySendType.KeyUp:
                    keyup = true;
                    break;
                case KeySendType.KeyHold:
                    keydown = true;
                    var subKeySend = new KeyData<KeySendType>(keyData);
                    subKeySend.KeyType = KeySendType.KeyUp;
                    LockKeyStock.Add(subKeySend);
                    break;
            }
            if (keyData == null) return;
            if (keyData.Win) KeySend(Keys.LWin, true, false);
            if (keyData.Ctrl) KeySend(Keys.LControlKey, true, false);
            if (keyData.Alt) KeySend(Keys.LAltKey, true, false);
            if (keyData.Shift) KeySend(Keys.LShiftKey, true, false);
            KeySend((Keys)keyData.Code, keydown, keyup);
            if (keyData.Shift) KeySend(Keys.LShiftKey, false, true);
            if (keyData.Alt) KeySend(Keys.LAltKey, false, true);
            if (keyData.Ctrl) KeySend(Keys.LControlKey, false, true);
            if (keyData.Win) KeySend(Keys.LWin, false, true);
        }
        public void KeySend(Keys key, KeySendType keySendType = KeySendType.KeyClick)
        {
            KeySend(new KeyData<KeySendType>(key, keySendType));
        }
        public void KeySend(Keys key, bool keydown = true, bool keyup = true)
        {
            int sendCount = (keydown ? 1 : 0) + (keyup ? 1 : 0);
            int sendCur = 0;
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
        public void KeySend(List<KeyData<KeySendType>> keyDataList)
        {
            if (keyDataList == null) return;
            foreach (var keyData in keyDataList)
            {
                KeySend(keyData);
            }
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
            }
            else
            {
                SetWindowLong(helper.Handle, GWL_EXSTYLE, GetWindowLong(helper.Handle, GWL_EXSTYLE)
                    | WS_EX_NOACTIVATE);
            }
        }
        //キーイベント取得
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(Keys vKey);
        public class InputTimer
        {
            //A_Press:押している間、 A_Up:押した瞬間、 A_Down:離した瞬間
            public Action<object, ElapsedEventArgs> MethodPress, MethodUp, MethodDown;
            public Timer TimerObj { get; private set; }
            public Keys KeyValue { get; private set; }
            public bool FlgPress { get; private set; }
            public InputTimer(Keys keys, double interval = 1)
            {
                KeyValue = keys;
                TimerObj = new Timer(interval);
                TimerObj.Elapsed += (sender, e) =>
                {
                    bool press = GetAsyncKeyState(KeyValue) != 0;
                    if (press)
                    {
                        Press(sender, e);
                        if (!FlgPress)
                        {
                            FlgPress = true;
                            Down(sender, e);
                        }
                    } else
                    {
                        if (FlgPress)
                        {
                            FlgPress = false;
                            Up(sender, e);
                        }
                    }
                };
            }
            void Press(object sender, ElapsedEventArgs e)
            {
                MethodPress?.Invoke(sender, e);
            }
            void Up(object sender, ElapsedEventArgs e)
            {
                MethodUp?.Invoke(sender, e);
            }
            void Down(object sender, ElapsedEventArgs e)
            {
                MethodDown?.Invoke(sender, e);
            }
            public void Start()
            {
                TimerObj.Start();
            }
            public void Stop()
            {
                TimerObj.Stop();
            }
        }
    }
}