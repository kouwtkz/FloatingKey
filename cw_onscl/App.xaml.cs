using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace cw_onscl
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Application Entry Point.protected override void OnSourceInitialized(EventArgs e)
        /// </summary>
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public static void Main()
        {
            //const string SemaphoreName = ".NET TopmostLaunture #054702 (C#)";
            string SemaphoreName = ".NET TopmostLaunture "
                + System.Reflection.Assembly.GetExecutingAssembly().Location.Replace("\\", "/");
            bool createdNew;
            // Semaphoreクラスのインスタンスを生成し、アプリケーション終了まで保持する
            using (var semaphore
                    = new System.Threading.Semaphore(1, 1, SemaphoreName,
                                                         out createdNew))
            {
                if (!createdNew)
                {
                    // 他のプロセスが先にセマフォを作っていた
                    return; // プログラム終了
                }

                // アプリケーション起動
                cw_onscl.App app = new cw_onscl.App();
                app.InitializeComponent();
                //このタイミング辺りで以下のメソッドが働く
                //protected override void OnSourceInitialized(EventArgs e)
                app.Run();

            } // Semaphoreクラスのインスタンスを破棄
        }
    }
}
