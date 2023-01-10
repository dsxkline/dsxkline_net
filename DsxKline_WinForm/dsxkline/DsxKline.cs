using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace DsxKline_WinForm.dsxkline
{
    class DsxKline : Control
    {
        public ChromiumWebBrowser browser;

        public delegate void OnLoading();
        public delegate void NextPage();
        public delegate void UpdateComplate();
        public delegate void OnCrossing(String data,int index);

        // 图表类型
        public enum ChartType
        {
            timeSharing,    // 分时图
            timeSharing5,   // 五日分时图
            candle,         // K线图
        };

        // 蜡烛图实心空心
        public enum CandleType
        {
            hollow, // 空心
            solid   // 实心
        };
        // 缩放K线锁定类型
        public enum ZoomLockType
        {
            none,       // 无
            left,       // 锁定左边进行缩放
            middle,     // 锁定中间进行缩放
            right,      // 锁定右边进行缩放
            follow,     // 跟随鼠标位置进行缩放，web版效果比较好
        };

        private static string homeUrl = AppDomain.CurrentDomain.BaseDirectory + @"dsxkline\index.html";

        public List<String> datas;
        // 主题 white dark 等
        public String theme = "white";
        // 图表类型 1=分时图 2=k线图
        public ChartType chartType = ChartType.timeSharing;
        // 蜡烛图k线样式 1=空心 2=实心
        public CandleType candleType = CandleType.hollow;
        // 缩放类型 1=左 2=中 3=右 4=跟随
        public ZoomLockType zoomLockType = ZoomLockType.right;
        // 每次缩放大小
        public double zoomStep = 1;
        // k线默认宽度
        public double klineWidth = 5;
        // 是否显示默认k线提示
        public bool isShowKlineTipPannel = true;
        // 副图高度
        public double sideHeight = 60;
        // 高度
        public double height;
        // 宽度
        public double width;
        // 默认主图指标 ["MA"]
        public String[] main = new String[] { "MA" };
        // 默认副图指标 副图数组代表副图数量 ["VOL","MACD"]
        public String[] sides = new String[] { "VOL", "MACD", "RSI" };
        // 昨日收盘价
        public double lastClose = 0;
        // 首次加载回调
        public OnLoading onLoading;
        // 完成加载回调
        public UpdateComplate updateComplate;
        // 滚动到左边尽头回调 通常用来加载下一页数据
        public NextPage nextPage;
        // 提示数据返回
        public OnCrossing onCrossing;
        // 右边空出k线数量
        public int rightEmptyKlineAmount = 2;
        // 当前页码
        public int page = 1;
        // 开启调试
        public bool debug = false;
        public double paddingBottom = 0;

        public DsxKline()
        {
            InitializeComponent();
            InitBrowser(homeUrl);
        }

        public void InitBrowser(String url)
        {
            Cef.Initialize(new CefSharp.WinForms.CefSettings());
            browser = new ChromiumWebBrowser(url);
            browser.BackColor = this.BackColor;
            browser.Dock = DockStyle.None;
            browser.SetBounds(0, 0, this.Bounds.Width, this.Bounds.Height);
            //绑定：
            browser.FrameLoadEnd += webview_FrameLoadEnd;
            browser.ConsoleMessage += webview_ConsoleMessage;
            this.Controls.Add(browser);
            
            CefSharpSettings.WcfEnabled = true;
            browser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
            browser.JavascriptObjectRepository.Register("DsxKlineJSEvent", new DsxKlineJSEvent(this), isAsync: false, options: BindingOptions.DefaultBinder);
        }


        private void createKline()
        {
            String js = "console.log('create kline js');" +
               "var c = document.getElementById(\"kline\");" +
               "dsxConfig.theme.white.klineWidth="+klineWidth+";"+
               "dsxConfig.theme.dark.klineWidth=" + klineWidth + ";" +
               "var kline = new dsxKline({" +
                   "element:c," +
                   "chartType:"+ (int)chartType+"," +
                   "theme:\""+theme+"\"," +
                   "candleType:" + (int)candleType + "," +
                   "zoomLockType: " + (int)zoomLockType + "," +
                   "isShowKlineTipPannel:"+(isShowKlineTipPannel?"true":"false")+"," +
                   (lastClose>0?"lastClose: " +lastClose+",":"") +
                   "sideHeight: " +sideHeight+"," +
                   "paddingBottom: "+paddingBottom+"," +
                   "autoSize: true," +
                   "debug:"+(debug?"true":"false")+"," +
                   "main:"+ Newtonsoft.Json.JsonConvert.SerializeObject(main) + "," +
                   "sides:" + Newtonsoft.Json.JsonConvert.SerializeObject(sides) + ", " +
                   "onLoading: function(o){" +
                   "    DsxKlineJSEvent.onLoading();" +
                   "}," +
                   "nextPage: function(data, index){" +
                   "    DsxKlineJSEvent.nextPage();" +
                   "}," +
                   "onCrossing: function(data, index){" +
                   "    DsxKlineJSEvent.onCrossing(JSON.stringify(data),index);" +
                   "}," +
                   "updateComplate: function(){" +
                   "    DsxKlineJSEvent.updateComplate();" +
                   "}," +
                "});";
            browser.ExecuteScriptAsync(js);
            //Console.WriteLine(js);
        }

        public void update(List<String>datas,int page)
        {
            if (browser == null) return;
            if (!browser.IsBrowserInitialized) return;
            String data = datas!=null?Newtonsoft.Json.JsonConvert.SerializeObject(datas):"[]";
            this.datas = datas;
            this.page = page;
            String js = "if(kline){" +
               "kline.update({" +
               "datas:"+ data + "," +
               "page:'" + page + "'," +
               "chartType:" + (int)chartType + "," +
               (lastClose > 0 ? "lastClose: " + lastClose + "," : "") +
               "main:" + Newtonsoft.Json.JsonConvert.SerializeObject(main) + "," +
               "sides:" + Newtonsoft.Json.JsonConvert.SerializeObject(sides) + ", " +
           "})};";
            browser.ExecuteScriptAsync(js);
        }

        /**
         * 加载数据前调用
         * @throws JSONException
         */
        public void startLoading()
        {
            if (browser == null) return;
            if (!browser.IsBrowserInitialized) return;
            String js = "kline.chartType="+(int)chartType+";kline.startLoading();";
            browser.ExecuteScriptAsync(js);
        }

        /**
         * 更新完K线图后调用
         */
        public void finishLoading()
        {
            String js = "kline.finishLoading();";
            browser.ExecuteScriptAsync(js);
        }
       
        /// <summary>
        /// 刷新最后一个K线
        /// </summary>
        /// <param name="lastData"></param>
        /// <param name="cycle">t,t5,d,w,m,m1,m5,m30</param>
        public void refreshLastOneData(String item,String cycle)
        {
            String js = "kline.refreshLastOneData('"+ item + "','"+cycle+"');";
            browser.ExecuteScriptAsync(js);
        }

        private void webview_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            createKline();
        }

        private void webview_ConsoleMessage(object sender, ConsoleMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // DsxKline
            // 
            this.SizeChanged += new System.EventHandler(this.DsxKline_SizeChanged);
            this.ResumeLayout(false);

        }

        private void DsxKline_SizeChanged(object sender, EventArgs e)
        {
            browser.SetBounds(0,0, this.Bounds.Width, this.Bounds.Height);
            update(datas,page);
        }

        class DsxKlineJSEvent
        {
            DsxKline dsxkline;
            public DsxKlineJSEvent(DsxKline dsx)
            {
                dsxkline = dsx;
            }
            public void onLoading()
            {
                if (dsxkline.onLoading!=null) dsxkline.onLoading();
            }
            public void nextPage()
            {
                if (dsxkline.nextPage != null) dsxkline.nextPage();
            }
            public void onCrossing(String data,int index)
            {
                if (dsxkline.onCrossing != null) dsxkline.onCrossing(data,index);
            }

            public void updateComplate()
            {
                if (dsxkline.updateComplate != null) dsxkline.updateComplate();
            }
        }

    }

    
}
