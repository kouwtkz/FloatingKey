using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CI = System.Windows.Input.Custom.CustomInput;
using Keys = System.Windows.Input.Custom.Keys;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace cw_onscl
{
    [DataContract]
    public class ButtonData
    {
        [DataMember(Name = "value")]
        public string Value { get; set; }
        [DataMember(Name = "img")]
        public string PicPath { get; set; }
        [DataMember(Name = "hover-img")]
        public string HoverPicPath { get; set; }
        [DataMember(Name = "tggle-img")]
        public string TogglePicPath { get; set; }
        [DataMember(Name = "keydata")]
        public List<CI.KeyData<CI.KeySendType>> KeyDataList = new List<CI.KeyData<CI.KeySendType>>();
        [DataMember(Name = "pathdata")]
        public List<string> PathDataList = new List<string>();
        [DataMember(Name = "width")]
        public double Width { get; set; }
        [DataMember(Name = "height")]
        public double Height { get; set; }
        [DataMember(Name = "size")]
        public double Size { get; set; }
        [DataMember(Name = "margin")]
        public Thickness Margin;
        [DataMember(Name = "color")]
        public string Foreground { get; set; }
        [DataMember(Name = "background")]
        public string Background { get; set; }
        [DataMember(Name = "border")]
        public string Border { get; set; }
        [DataMember(Name = "hover-color")]
        public string HoverForeground { get; set; }
        [DataMember(Name = "hover-background")]
        public string HoverBackground { get; set; }
        [DataMember(Name = "hover-border")]
        public string HoverBorder { get; set; }
        [DataMember(Name = "toggle-color")]
        public string ToggleForeground { get; set; }
        [DataMember(Name = "toggle-background")]
        public string ToggleBackground { get; set; }
        [DataMember(Name = "toggle-border")]
        public string ToggleBorder { get; set; }

        public ButtonData()
        {
            Height = 30;
            Width = Double.NaN;
            Margin.Top = 10;
            Margin.Bottom = 10;
            Size = 16;
        }
    }
    [DataContract]
    public class FormData
    {
        [DataMember(Name = "list")]
        public List<ButtonData> ListData = new List<ButtonData>();
        [DataMember(Name = "width")]
        public double Width { get; set; }
        [DataMember(Name = "height")]
        public double Height { get; set; }
        [DataMember(Name = "img")]
        public string PicPath { get; set; }
        [DataMember(Name = "color")]
        public string Foreground { get; set; }
        [DataMember(Name = "background")]
        public string Background { get; set; }
        [DataMember(Name = "border")]
        public string Border { get; set; }
        [DataMember(Name = "monitor")]
        public bool Monitor { get; set; }
        public FormData()
        {
            Height = 400;
            Width = 100;
        }
        public FormData(Window window)
        {
            Height = window.Height;
            Width = window.Width;
        }
    }
    public struct BrushObject
    {
        public Brush Background { get; set; }
        public Brush HoverBackground { get; set; }
        public Brush ToggleBackground { get; set; }
        public Brush Foreground { get; set; }
        public Brush HoverForeground { get; set; }
        public Brush ToggleForeground { get; set; }
        public Brush Border { get; set; }
        public Brush HoverBorder { get; set; }
        public Brush ToggleBorder { get; set; }
    }
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private CI cInput = new CI();
        private FormData ThisFormData = null;
        private string JsonFileName = "config.json";
        // モニタニングタイマー
        private DispatcherTimer MoniterTimer;
        private double MoniterTimerInterval = 1;
        // 最小化した際に戻すための変数
        private int tmpComboIndex = 0;
        // ボタン名
        public string ButtonName { get; private set; } = "Button";
        // ブラシの色定義
        private Brush whiteBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("White"));
        private Brush lightgrayBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE0E0E0"));
        private Brush grayBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD0D0D0"));
        private Brush blackBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Black"));
        private Brush ClearBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00000000"));
        private Dictionary<ContentControl, BrushObject> ContentBrushes = new Dictionary<ContentControl, BrushObject>();

        // 編集モード
        private bool formEditingMode = false;
        public bool FormEditingMode
        {
            set
            {
                formEditingMode = value;
                Topmost = !formEditingMode;
                CI.ChangeWindowActivate(this, formEditingMode);
            }
            get { return formEditingMode; }
        }
        private const int
            MODE_TOPMOST = 0,
            MODE_EDITING = 1,
            MODE_MIN = 2,
            MODE_CLOSE = 3,
            MODE_JSON = 4;
        private List<ContentControl> ButtonList;
        private Dictionary<byte, bool> keysdic = new Dictionary<byte, bool>();
        private List<byte> keyslist = new List<byte>();
        public MainWindow()
        {
            InitializeComponent();
            ButtonList = new List<ContentControl>();
            ThisFormData = new FormData(this);
            FormEditingMode = false;
            MouseLeftButtonDown += (o, e) => {
                try { DragMove(); } catch { }
            };
            //割り当て全解除
            MouseRightButtonDown += (o, e) => {
                foreach(var kvPair in keysdic)
                {
                    cInput.KeySend(kvPair.Key, false, true);
                }
            };
            //FromPanelSetButtonData();
            //WriteFormJSON();
            ReadFormJSON();
            SyncFormViewData();
            SyncButtonData();
            MoniterTimer = new DispatcherTimer(DispatcherPriority.Normal, this.Dispatcher);
            MoniterTimer.Interval = TimeSpan.FromMilliseconds(MoniterTimerInterval);
            MoniterTimer.Tick += (o, e) =>
            {
                if (ButtonList == null) return;
                for (int i = 0; i < keyslist.Count; i++)
                {
                    keysdic[keyslist[i]] = CI.GetAsyncKeyState((Keys)keyslist[i]) != 0;
                }
                for (int i = 0; i < ThisFormData.ListData.Count; i++)
                {
                    ContentControl btn = ButtonList[i];
                    BrushObject brush = ContentBrushes[btn];
                    if (btn.IsMouseOver)
                    {
                        if (brush.HoverBackground != null) btn.Background = brush.HoverBackground;
                        if (brush.HoverForeground != null) btn.Foreground = brush.HoverForeground;
                        if (brush.HoverBorder != null) btn.BorderBrush = brush.HoverBorder;
                        continue;
                    }
                    var data = ThisFormData.ListData[i];
                    bool keycheck = false;
                    if (ThisFormData.Monitor)
                    {
                        foreach (var keydata in data.KeyDataList)
                        {
                            keycheck = keysdic[keydata.Code];
                            if (!keycheck) break;
                        }
                    }
                    Brush cb_back, cb_front, cb_border;
                    if (keycheck)
                    {
                        if (brush.ToggleBackground == null)
                            cb_back = brush.HoverBackground;
                        else
                            cb_back = brush.ToggleBackground;
                        if (brush.ToggleBackground == null)
                            cb_front = brush.HoverForeground;
                        else
                            cb_front = brush.ToggleForeground;
                        if (brush.ToggleBorder == null)
                            cb_border = brush.HoverBorder;
                        else
                            cb_border = brush.ToggleBorder;
                    }
                    else
                    {
                        cb_back = brush.Background;
                        cb_front = brush.Foreground;
                        cb_border = brush.Border;
                    }
                    if (cb_back != null) ButtonList[i].Background = cb_back;
                    if (cb_front != null) ButtonList[i].Foreground = cb_front;
                    if (cb_border != null) ButtonList[i].BorderBrush = cb_border;
                }
            };
            MoniterTimer.Start();
        }
        private void SyncFormViewData()
        {
            Width = ThisFormData.Width;
            Height = ThisFormData.Height;
        }

        private void SyncButtonData(bool AppendFlag = false)
        {

            List<ButtonData> ButtonDataList = ThisFormData.ListData;
            StackPanel sPanel = FindName("sPanel") as StackPanel;
            int stock_button = 0;
            if (AppendFlag)
            {
                stock_button = sPanel.Children.Count;
            }
            else
            {
                sPanel.Children.Clear();
                ButtonList.Clear();
                ContentBrushes.Clear();
            }
            if (ButtonDataList != null)
            {
                keysdic.Clear();
                keyslist.Clear();
                for (int i = 0; i < ButtonDataList.Count; i++)
                {
                    ButtonData btnData = ButtonDataList[i];
                    foreach (var keydata in btnData.KeyDataList)
                    {
                        var code = keydata.Code;
                        if (!keysdic.ContainsKey(code))
                        {
                            keyslist.Add(code);
                            keysdic.Add(code, ThisFormData.Monitor);
                        }
                    }
                    BrushObject brush = new BrushObject();
                    brush.Background = whiteBrush;
                    brush.Foreground = blackBrush;
                    brush.Border = ClearBrush;
                    brush.HoverBackground = grayBrush;
                    brush.HoverForeground = blackBrush;
                    brush.HoverBorder = ClearBrush;
                    brush.ToggleBackground = lightgrayBrush;
                    brush.ToggleForeground = blackBrush;
                    brush.ToggleBorder = ClearBrush;
                    var style = new Style();
                    object content = "";
                    if (btnData.Width > 0)
                        style.Setters.Add(new Setter(WidthProperty, btnData.Width));
                    style.Setters.Add(new Setter(HeightProperty, btnData.Height));
                    style.Setters.Add(new Setter(MarginProperty, btnData.Margin));
                    if (btnData.Size > 0)
                        style.Setters.Add(new Setter(FontSizeProperty, btnData.Size));
                    ImageBrush img = null;
                    if (btnData.PicPath == "" || btnData.PicPath == null || !File.Exists(btnData.PicPath))
                    {
                        content = btnData.Value;
                        if (btnData.Background != "" && btnData.Background != null)
                            brush.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnData.Background));
                        if (btnData.HoverBackground != "" && btnData.HoverBackground != null)
                        {
                            brush.HoverBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnData.HoverBackground));
                            brush.ToggleBackground = brush.HoverBackground;
                        }
                        if (btnData.ToggleBackground != "" && btnData.ToggleBackground != null)
                            brush.ToggleBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnData.ToggleBackground));
                    }
                    else
                    {
                        try
                        {
                            img = new ImageBrush();
                            img.ImageSource = new BitmapImage(new Uri(btnData.PicPath, UriKind.RelativeOrAbsolute));
                            img.Stretch = Stretch.Uniform;
                            brush.Background = img;
                            if (btnData.HoverPicPath != "" && File.Exists(btnData.HoverPicPath))
                            {
                                img = new ImageBrush();
                                img.ImageSource = new BitmapImage(new Uri(btnData.HoverPicPath, UriKind.RelativeOrAbsolute));
                                img.Stretch = Stretch.Uniform;
                            }
                            brush.HoverBackground = img;
                            if (btnData.TogglePicPath != "" && File.Exists(btnData.TogglePicPath))
                            {
                                img = new ImageBrush();
                                img.ImageSource = new BitmapImage(new Uri(btnData.TogglePicPath, UriKind.RelativeOrAbsolute));
                                img.Stretch = Stretch.Uniform;
                            }
                            brush.ToggleBackground = img;
                        }
                        catch { }
                    }
                    if (btnData.Foreground != "" && btnData.Foreground != null)
                        brush.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnData.Foreground));
                    if (btnData.Border != "" && btnData.Border != null)
                        brush.Border = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnData.Border));
                    ContentControl btn;
                    if (img == null)
                    {
                        var button = new Button();
                        button.Click += (o, e2) => { Button_Click(o, e2); return; };
                        btn = button;
                    } else
                    {
                        btn = new Label();
                        btn.MouseLeftButtonDown += (o, e2) => { Button_Click(o, e2); return; };
                    }
                    var febtn = btn;
                    febtn.SetValue(ContentProperty, content);
                    febtn.SetValue(FocusableProperty, false);
                    Style festyle = style;
                    if (btnData.HoverForeground != "" && btnData.HoverForeground != null)
                        brush.HoverForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnData.HoverForeground));
                    if (btnData.HoverBorder != "" && btnData.HoverBorder != null)
                        brush.HoverBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnData.HoverBorder));
                    if (brush.Background != null) festyle.Setters.Add(new Setter(BackgroundProperty, brush.Background));
                    if (brush.Foreground != null) festyle.Setters.Add(new Setter(ForegroundProperty, brush.Foreground));
                    if (brush.Border != null) festyle.Setters.Add(new Setter(BorderBrushProperty, brush.Border));

                    btn.Name = ButtonName + (i).ToString();
                    btn.Focusable = false;
                    btn.Style = style;
                    ContentBrushes.Add(btn, brush);
                    sPanel.Children.Add(btn);
                    ButtonList.Add(btn);
                }
            }
        }
        private void FromPanelSetButtonData(bool AppendFlag = false)
        {
            List<ButtonData> ButtonDataList = ThisFormData.ListData;
            if (!AppendFlag) ButtonDataList.Clear();
            StackPanel sPanel = FindName("sPanel") as StackPanel;
            for (int i = 0; i < sPanel.Children.Count; i++)
            {
                ContentControl btn = sPanel.Children[i] as ContentControl;
                ButtonData btnData = new ButtonData();
                btnData.Value = btn.Content.ToString();
                btnData.Width = btn.Width;
                btnData.Height = btn.Height;
                btnData.Margin = btn.Margin;
                btnData.Size = btn.FontSize;
                ButtonDataList.Add(btnData);
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            List<ButtonData> ButtonDataList = ThisFormData.ListData;
            ContentControl btn = sender as ContentControl;
            if (FormEditingMode)
            {
            }
            else
            {
                int i = int.Parse(Regex.Match(btn.Name, "\\d+$").Value);
                ButtonData btnData = ButtonDataList[i];
                try
                {
                    cInput.KeySend(btnData.KeyDataList);
                    if (btnData.PathDataList != null)
                        foreach (string path in btnData.PathDataList)
                        {
                            Process.Start(path);
                        }
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.ToString());
                }
            }
        }
        private void WriteFormJSON()
        {
            using (var fs = new FileStream(JsonFileName, FileMode.Create))
            {
                var serializer = new DataContractJsonSerializer(typeof(FormData));
                serializer.WriteObject(fs, ThisFormData);
                //JsonString = Encoding.UTF8.GetString(ms.ToArray());
            }
        }
        private void ReadFormJSON()
        {
            try
            {
                if (File.Exists(JsonFileName))
                    using (var fs = new FileStream(JsonFileName, FileMode.Open))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(FormData));
                        ThisFormData = (FormData)serializer.ReadObject(fs);
                    }
            }
            catch
            {
                MessageBox.Show(JsonFileName + "が読み込めませんでした");
            }
        }
        private void SyncPanelMenu()
        {
            StackPanel sPanel = FindName("sPanel") as StackPanel;
            if (sPanel == null) return;
            if (FormEditingMode)
            {
                ContextMenu cMenu = new ContextMenu();
                MenuItem m;
                m = new MenuItem();
                m.Header = "前に追加";
                cMenu.Items.Add(m);
                m = new MenuItem();
                m.Header = "後に追加";
                cMenu.Items.Add(m);
                foreach (var item in sPanel.Children)
                {
                    try
                    {
                        ContentControl btn = item as ContentControl;
                        btn.ContextMenu = cMenu;
                    }
                    catch { }
                }
            }
            else
            {
                foreach (var item in sPanel.Children)
                {
                    try
                    {
                        ContentControl btn = item as ContentControl;
                        btn.ContextMenu = null;

                    }
                    catch { }
                }
            }
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            switch (comboBox.SelectedIndex)
            {
                case MODE_EDITING:
                    FormEditingMode = true;
                    tmpComboIndex = comboBox.SelectedIndex;
                    break;
                case MODE_MIN:
                    ComboBox combo = FindName("combo") as ComboBox;
                    combo.SelectedIndex = tmpComboIndex;
                    WindowState = WindowState.Minimized;
                    break;
                case MODE_CLOSE:
                    Close();
                    return;
                case MODE_JSON:
                    Process.Start("notepad.exe", JsonFileName);
                    Close();
                    return;
                default:
                    FormEditingMode = false;
                    tmpComboIndex = comboBox.SelectedIndex;
                    break;
            }
            SyncPanelMenu();
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            CI.ChangeWindowActivate(this, false, false);
        }
    }
}
