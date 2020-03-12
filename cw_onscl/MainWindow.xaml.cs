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

namespace cw_onscl
{
    [DataContract]
    public class KeyData
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
        [DataMember(Name = "keydown")]
        public bool KeyDown { get; set; }
        [DataMember(Name = "keyup")]
        public bool KeyUp { get; set; }
        public KeyData()
        {
            KeyDown = true;
            KeyUp = true;
        }
    }
    [DataContract]
    public class ButtonData
    {
        [DataMember(Name = "value")]
        public string Value { get; set; }
        [DataMember(Name = "img")]
        public string PicPath { get; set; }
        [DataMember(Name = "hover-img")]
        public string HoverPicPath { get; set; }
        [DataMember(Name = "keydata")]
        public List<KeyData> KeyDataList = new List<KeyData>();
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
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private CI cInput = new CI();
        private FormData ThisFormData = null;
        private string JsonFileName = "config.json";
        private bool formEditingMode = false;
        private int tmpComboIndex = 0;
        private Brush whiteBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("White"));
        private Brush grayBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE0E0E0"));
        // 編集モード状態かどうかを取得する
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
            MODE_CLOSE = 3;
        public MainWindow()
        {
            InitializeComponent();
            //Button hb = (Button)FindName("HoverTestButton");
            //Setter s = (Setter)hb.Style.Setters[1];
            //ControlTemplate tmp = (ControlTemplate)s.Value;
            //MessageBox.Show(tmp.VisualTree.ToString());
            //Close(); return;
            ThisFormData = new FormData(this);
            FormEditingMode = false;
            MouseLeftButtonDown += (o, e) => {
                DragMove();
            };
            //FromPanelSetButtonData();
            //WriteFormJSON();
            ReadFormJSON();
            SyncFormViewData();
            SyncButtonData();
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
            }
            if (ButtonDataList != null)
            {
                for (int i = 0; i < ButtonDataList.Count; i++)
                {
                    ButtonData btnData = ButtonDataList[i];
                    var style = new Style();
                    object content = "";
                    if (btnData.Width > 0)
                        style.Setters.Add(new Setter(WidthProperty, btnData.Width));
                    style.Setters.Add(new Setter(HeightProperty, btnData.Height));
                    style.Setters.Add(new Setter(MarginProperty, btnData.Margin));
                    if (btnData.Size > 0)
                        style.Setters.Add(new Setter(FontSizeProperty, btnData.Size));
                    Brush cb_back = whiteBrush;
                    Brush cb_front = null, cb_border = null;
                    Brush cb_back_hover = grayBrush;
                    Brush cb_front_hover = null, cb_border_hover = null;
                    if (btnData.PicPath == "" || btnData.PicPath == null || !File.Exists(btnData.PicPath))
                    {
                        content = btnData.Value;
                        if (btnData.Background != "" && btnData.Background != null)
                            cb_back = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnData.Background));
                        if (btnData.HoverBackground != "" && btnData.HoverBackground != null)
                            cb_back_hover = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnData.HoverBackground));
                    }
                    else
                    {
                        try
                        {
                            var img = new ImageBrush();
                            img.ImageSource = new BitmapImage(new Uri(btnData.PicPath, UriKind.RelativeOrAbsolute));
                            img.Stretch = Stretch.Uniform;
                            cb_back = img;
                            if (btnData.HoverPicPath != "" && File.Exists(btnData.HoverPicPath))
                            {
                                img = new ImageBrush();
                                img.ImageSource = new BitmapImage(new Uri(btnData.HoverPicPath, UriKind.RelativeOrAbsolute));
                                img.Stretch = Stretch.Uniform;
                            }
                            cb_back_hover = img;
                        }
                        catch { }

                    }
                    if (btnData.Foreground != "" && btnData.Foreground != null)
                        cb_front = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnData.Foreground));
                    if (btnData.Border != "" && btnData.Border != null)
                        cb_border = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnData.Border));
                    if (btnData.HoverForeground != "" && btnData.HoverForeground != null)
                        cb_front_hover = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnData.HoverForeground));
                    if (btnData.HoverBorder != "" && btnData.HoverBorder != null)
                        cb_border_hover = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnData.HoverBorder));
                    var tHover = new Trigger();
                    tHover.Property = IsMouseOverProperty;
                    tHover.Value = true;
                    if (cb_back_hover != null) tHover.Setters.Add(new Setter(BackgroundProperty, cb_back_hover));
                    if (cb_front_hover != null) tHover.Setters.Add(new Setter(ForegroundProperty, cb_front_hover));
                    //style.Triggers.Add(tHover);
                    var controlTemp = new ControlTemplate();
                    controlTemp.TargetType = typeof(Button);

                    var febtn = new FrameworkElementFactory(typeof(Label));
                    febtn.SetValue(ContentProperty, content);
                    febtn.SetValue(FocusableProperty, false);
                    Style festyle = new Style();
                    festyle.Triggers.Add(tHover);
                    if (cb_back != null) festyle.Setters.Add(new Setter(BackgroundProperty, cb_back));
                    if (cb_front != null) festyle.Setters.Add(new Setter(ForegroundProperty, cb_front));
                    if (cb_border != null) festyle.Setters.Add(new Setter(BorderBrushProperty, cb_border));
                    febtn.SetValue(StyleProperty, festyle);
                    controlTemp.VisualTree = febtn;

                    style.Setters.Add(new Setter(TemplateProperty, controlTemp));
                    Button btn = new Button();
                    btn.Name = "Button" + (i).ToString();
                    btn.Focusable = false;
                    btn.Style = style;
                    btn.Click += (sender2, e2) => { Button_Click(sender2, e2); return; };
                    sPanel.Children.Add(btn);
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
                Button btn = sPanel.Children[i] as Button;
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
            Button btn = sender as Button;
            if (FormEditingMode)
            {
            }
            else
            {
                int i = int.Parse(Regex.Match(btn.Name, "\\d+$").Value);
                ButtonData btnData = ButtonDataList[i];
                try
                {
                    if (btnData.KeyDataList != null)
                        foreach (KeyData keyData in btnData.KeyDataList)
                        {
                            if (keyData.Win) cInput.KeySend(Keys.LWin, CI.KeySendType.KeyDown);
                            if (keyData.Ctrl) cInput.KeySend(Keys.LControlKey, CI.KeySendType.KeyDown);
                            if (keyData.Alt) cInput.KeySend(Keys.LAltKey, CI.KeySendType.KeyDown);
                            if (keyData.Shift) cInput.KeySend(Keys.LShiftKey, CI.KeySendType.KeyDown);
                            CI.KeySendType keySendType = CI.KeySendType.KeyClick;
                            if (keyData.KeyDown && !keyData.KeyUp)
                            {
                                keySendType = CI.KeySendType.KeyDown;
                            }
                            else if (keyData.KeyUp && !keyData.KeyDown)
                            {
                                keySendType = CI.KeySendType.KeyUp;
                            }
                            cInput.KeySend((Keys)keyData.Code, keySendType);
                            if (keyData.Shift) cInput.KeySend(Keys.LShiftKey, CI.KeySendType.KeyUp);
                            if (keyData.Alt) cInput.KeySend(Keys.LAltKey, CI.KeySendType.KeyUp);
                            if (keyData.Ctrl) cInput.KeySend(Keys.LControlKey, CI.KeySendType.KeyUp);
                            if (keyData.Win) cInput.KeySend(Keys.LWin, CI.KeySendType.KeyUp);
                        }
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
                        Button btn = item as Button;
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
                        Button btn = item as Button;
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
            CI.ChangeWindowActivate(this, false);
        }
    }
}
