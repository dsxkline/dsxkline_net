using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using DsxKline_WinForm.dsxkline;

namespace DsxKline_WinForm
{
    public partial class Form1 : Form
    {
        DsxKline dsxkline;
        int page = 1;
        List<String> datas;
        String cycle = "timeline";
        String code = "sh000001";
        int y = 50;
        int x = 5;
        public Form1()
        {
            InitializeComponent();
            dsxkline = new DsxKline();
            this.Controls.Add(dsxkline);
            dsxkline.SetBounds(x, y, this.ClientRectangle.Width - 2 * x, this.ClientRectangle.Height-y);
            dsxkline.onLoading = (() => {
                Console.WriteLine("onLoading");
                page = 1;
                datas = new List<String>();
                if (dsxkline.chartType == DsxKline.ChartType.timeSharing) getQuote(code);
                if (dsxkline.chartType == DsxKline.ChartType.timeSharing5) getTimeLine5();
                if (dsxkline.chartType == DsxKline.ChartType.candle) getDay();
            });
            dsxkline.nextPage = (() => {
                // 继续请求下一页
                // .....

                // 完成后执行
                dsxkline.finishLoading();
            });
            dsxkline.onCrossing = ((data,index) => {
                // 十字线滑动数据
                Console.WriteLine(data);
            });
        }

        private void tab(int i)
        {
            if (i == 0) dsxkline.chartType = DsxKline.ChartType.timeSharing;
            if (i == 1) dsxkline.chartType = DsxKline.ChartType.timeSharing5;
            if (i >= 2) dsxkline.chartType = DsxKline.ChartType.candle;
            if (i == 0) cycle = "timeline";
            if (i == 1) cycle = "timeline5";
            if (i == 2) cycle = "day";
            if (i == 3) cycle = "week";
            if (i == 4) cycle = "month";
            if (i == 5) cycle = "m1";

            dsxkline.startLoading();
        }
        private void getDay()
        {
            if (cycle.StartsWith("m") && !cycle.StartsWith("month"))
            {
                if (code.StartsWith("hk") || code.StartsWith("us"))
                {
                    dsxkline.finishLoading();
                    return;
                }
                List<String> data = QqHq.getMinLine(code, cycle, 320);
                if (data.Count > 0)
                {
                    //d.data = [];
                    if (page <= 1) datas = data;

                    dsxkline.update(datas,page);
                    page++;
                }
                dsxkline.finishLoading();
               
            }
            else
            {
                List<String> data = QqHq.getkLine(code, cycle, "", "", 320, "qfq");
                if (data.Count > 0)
                {
                    //d.data = [];
                    if (page <= 1) datas = data;

                    dsxkline.update(datas, page);
                    page++;
                }
                dsxkline.finishLoading();
            }
        }

        public void getTimeLine()
        {
            List<String> data = QqHq.getTimeLine(code);
            if (data.Count > 0)
            {
                //d.data = [];
                datas = data;

                dsxkline.update(datas, page);
                page++;
            }
            dsxkline.finishLoading();

        }

        public void getTimeLine5()
        {
            Dictionary<String,dynamic> data = QqHq.getFdayLine(code);
            if (data.Count > 0)
            {
                //d.data = [];
                datas = data["data"];
                dsxkline.lastClose = data["lastClose"];
                dsxkline.update(datas, page);
                page++;
            }
            dsxkline.finishLoading();

        }

        public void getQuote(String code)
        {
            List<HqModel> hqModels = QqHq.getQuote(code);
                HqModel d = hqModels[0];
            dsxkline.lastClose = double.Parse(d.lastClose);
            if (cycle.Equals("timeline")) getTimeLine();
            if (cycle.Equals("timeline5")) getTimeLine5();
           
        }

        public void getQuoteRefresh(String code)
        {
            if (dsxkline!=null) return;
            List<HqModel> data = QqHq.getQuote(code);
                HqModel d = data[0];
  
                var item = d.date.Replace("-", "").Replace("-", "") + "," + d.time.Replace(":", "").Substring(0, 4) + "," + d.price + "," + d.vol + "," + d.volAmount;
                if (dsxkline.chartType == DsxKline.ChartType.candle)
                {
                    if (cycle.StartsWith("m1"))
                    {
                        item = d.date.Replace("-", "").Replace("-", "") + "," + d.time.Replace(":", "").Substring(0, 4) + "," + d.price + "," + d.price + "," + d.price + "," + d.price + "," +d.vol + "," + d.volAmount;
                    }
                    else
                    {
                        item = d.date.Replace("-", "").Replace("-", "") + "," + d.open + "," + d.high + "," + d.low + "," + d.price + "," + d.vol + "," + d.volAmount;
                    }
                }
                //console.log(item);
                var c = "t";
                if (cycle == "day") c = "d";
                if (cycle == "week") c = "w";
                if (cycle == "month") c = "m";
                if (cycle == "year") c = "y";
                if (cycle == "min1") c = "m1";
                if (cycle == "timeline") c = "t";
                if (cycle == "timeline5") c = "t5";
                //console.log(cycle+"_"+item);
                dsxkline.refreshLastOneData(item, c);

 

            
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            tab(0);
        }
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            dsxkline.SetBounds(x, y, this.ClientRectangle.Width - 2 * x, this.ClientRectangle.Height-y);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            tab(0);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            tab(1);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            tab(2);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            tab(3);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            tab(4);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            tab(5);
        }
    }
}
