﻿using System.Windows.Input.Custom;
using System.Runtime.InteropServices; // DllImport
using System.Windows.Interop;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Timers;
using System.Windows.Threading;

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
            public int wVk;
            public int wScan;
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
            KeyToggle = 4,
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
            public int Code { get; set; }
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
            public KeyData(int code, T keyType)
            {
                Code = code;
                KeyType = keyType;
            }
            public KeyData(Keys code, T keyType)
            {
                Code = (int)code;
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
        public Dictionary<int,KeyData<KeySendType>> LockKeyStock;
        private bool FlgNextUnLock;
        public const double DelaySync = 40;
        public CustomInput(double interval = 1, Dispatcher dispatcher = null, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            LockKeyStock = new Dictionary<int, KeyData<KeySendType>>();
            inputTimer = new InputTimer(Keys.LButton, interval, dispatcher, priority);
            inputTimer.MethodDown = () =>
            {
                if (!FlgNextUnLock)
                {
                    if (LockKeyStock.Count > 0) FlgNextUnLock = true;
                }
            };
            inputTimer.MethodUp = () =>
            {
                if (FlgNextUnLock)
                {
                    FlgNextUnLock = false;
                    foreach(var data in LockKeyStock) {
                        KeySend(data.Value);
                    }
                    LockKeyStock.Clear();
                }
            };
            inputTimer.Start();
        }
        public void KeySend(KeyData<KeySendType> keyData)
        {
            KeySend(keyData, keyData.KeyType);
        }
        public void KeySend(KeyData<KeySendType> keyData, KeySendType keySendType)
        {
            if (keyData == null) return;
            bool keydown = false, keyup = false;
            switch (keySendType)
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
                    var subKeySend = new KeyData<KeySendType>(keyData);
                    var stockDelayTimer = new Timer(DelaySync);
                    stockDelayTimer.Elapsed += (sender, e) => {
                        stockDelayTimer.Stop();
                        if (!LockKeyStock.ContainsKey(subKeySend.Code))
                        {
                            subKeySend.KeyType = KeySendType.KeyDown;
                            KeySend(subKeySend);
                            subKeySend.KeyType = KeySendType.KeyUp;
                            LockKeyStock.Add(subKeySend.Code, subKeySend);
                        }
                    };
                    stockDelayTimer.Start();
                    keyData = null;
                    break;
                case KeySendType.KeyToggle:
                    var keypress = GetAsyncKeyState((Keys)keyData.Code) != 0;
                    keydown = !keypress;
                    keyup = keypress;
                    break;
            }
            if (keyData == null) return;
            if (keyData.Win && keydown) KeySend(Keys.LWin, true, false);
            if (keyData.Ctrl && keydown) KeySend(Keys.LControlKey, true, false);
            if (keyData.Alt && keydown) KeySend(Keys.LAltKey, true, false);
            if (keyData.Shift && keydown) KeySend(Keys.LShiftKey, true, false);
            KeySend((Keys)keyData.Code, keydown, keyup);
            if (keyData.Shift && keyup) KeySend(Keys.LShiftKey, false, true);
            if (keyData.Alt && keyup) KeySend(Keys.LAltKey, false, true);
            if (keyData.Ctrl && keyup) KeySend(Keys.LControlKey, false, true);
            if (keyData.Win && keyup) KeySend(Keys.LWin, false, true);
        }
        public void KeySend(Keys key, KeySendType keySendType = KeySendType.KeyClick)
        {
            KeySend(new KeyData<KeySendType>(key, keySendType));
        }
        public void KeySend(Keys key, bool keydown = true, bool keyup = true)
        {
            KeySend((int)key, keydown, keyup);
        }
        public void KeySend(int key, bool keydown = true, bool keyup = true)
        {
            int sendCount = (keydown ? 1 : 0) + (keyup ? 1 : 0);
            int sendCur = 0;
            if (keybdFlag)
            {
                int bVk = key;
                int bScan = MapVirtualKey(key, 0);
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
                int wVk = key;
                int wScan = MapVirtualKey(wVk, 0);
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
        private static extern uint keybd_event(int bVk, int bScan, uint dwFlags, UIntPtr dwExtraInfo);
        // アクティブにしないウィンドウ処理
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_MAXIMIZE = 0x00010000;
        private const int WS_POPUP = unchecked((int)0x80000000);



        public static void ChangeWindowActivate(Window win, bool Activate, bool Maximize = true, int windef = GWL_EXSTYLE)
        {
            var helper = new WindowInteropHelper(win);
            int winint = GetWindowLong(helper.Handle, GWL_STYLE);
            int winexint = GetWindowLong(helper.Handle, GWL_EXSTYLE);
            if (Activate)
                winexint &= ~WS_EX_NOACTIVATE;
            else
                winexint |= WS_EX_NOACTIVATE;
            if (Maximize)
                winint |= WS_EX_MAXIMIZE;
            else
                winint &= ~WS_EX_MAXIMIZE;
            SetWindowLong(helper.Handle, GWL_STYLE, winint);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, winexint);
        }
        //キーイベント取得
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(Keys vKey);
        public class InputTimer
        {
            //A_Press:押している間、 A_Up:押した瞬間、 A_Down:離した瞬間
            public Action MethodPress, MethodUp, MethodDown;
            public Timer TimerObj { get; private set; } = null;
            public DispatcherTimer DpTimerObj { get; private set; } = null;
            public Keys KeyValue { get; private set; }
            public bool FlgPress { get; private set; }
            void LoopAction(object sender, EventArgs e)
            {
                bool press = GetAsyncKeyState(KeyValue) != 0;
                if (press)
                {
                    Press();
                    if (!FlgPress)
                    {
                        FlgPress = true;
                        Down();
                    }
                }
                else
                {
                    if (FlgPress)
                    {
                        FlgPress = false;
                        Up();
                    }
                }
            }
            public InputTimer(Keys keys, double interval = 1,
                Dispatcher dispatcher = null, DispatcherPriority priority = DispatcherPriority.Normal)
            {
                KeyValue = keys;
                if (dispatcher == null)
                {
                    TimerObj = new Timer(interval);
                    TimerObj.Elapsed += (sender, e) => { LoopAction(sender, e); };
                }
                else
                {
                    DpTimerObj = new DispatcherTimer(priority, dispatcher);
                    DpTimerObj.Interval = TimeSpan.FromMilliseconds(interval);
                    DpTimerObj.Tick += (sender, e) => { LoopAction(sender, e); };
                }
            }
            void Press()
            {
                MethodPress?.Invoke();
            }
            void Up()
            {
                MethodUp?.Invoke();
            }
            void Down()
            {
                MethodDown?.Invoke();
            }
            public void Start()
            {
                if (DpTimerObj == null)
                    TimerObj.Start();
                else
                    DpTimerObj.Start();
            }
            public void Stop()
            {
                if (DpTimerObj == null)
                    TimerObj.Stop();
                else
                    DpTimerObj.Stop();
            }
        }
        // 環境変数ありのパスから変換する関数
        public static string ExpandEnvironmentStrings(string path)
        {
            return Text.RegularExpressions.Regex.Replace(path, @"\%([^\%]*)\%", x =>
            {
                return Environment.GetEnvironmentVariable(x.Groups[1].Value);
            });
        }
    }
}