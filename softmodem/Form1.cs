using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace SoftModem
{
    public partial class Form1 : Form
    {
        public Form1() {
            InitializeComponent();
        }

        private void runRToolStripMenuItem_Click(object _sender, EventArgs _e) {
            Encoder e = new Encoder(44100);
            var encodedData = e.encoding(new byte[] { 0,1,2,3,4,5 }).ToArray();
            var samples = Enumerable.Repeat(0.0f,165).Concat(e.modulating(encodedData)).Concat(Enumerable.Repeat(0.0f,143)).ToArray();
            {
                ChartArea area1 = new ChartArea("Area1");

                Title title1 = new Title("Title1");
                title1.DockedToChartArea = "Area1";
                chart1.ChartAreas.Add(area1);
                chart1.Titles.Add(title1);
                {
                    var series = chart1.Series.Add("Encoded");
                    series.ChartType = SeriesChartType.Line;
                    series.LegendText = "入力データ";
                    series.BorderWidth = 3;
                    series.ChartArea = "Area1";
                    var values = encodedData.Select(x => (double)x).ToList().Resample(samples.Length).ToList();
                    for (var i = 0; i < values.Count; i++) {
                        series.Points.Add(new DataPoint(i, values[i]));
                    }
                }
                {
                    var series = chart1.Series.Add("Samples");
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                    series.LegendText = "波形データ";
                    series.BorderWidth = 1;
                    series.ChartArea = "Area1";
                    var values = samples.Select(x => (double)x).ToList();
                    for (var i = 0; i < values.Count; i++) {
                        series.Points.Add(new DataPoint(i, values[i]));
                    }
                }
            }

            // ホワイトガウスノイズを通信経路ノイズとして加算（ボックス＝ミュラー法で作っている）
            var rand = new Random();
            for (var i = 0; i < samples.Length; i++) {
                var x = rand.NextDouble();
                var y = rand.NextDouble();
                var z = Math.Sqrt(-2 * Math.Log(x)) * Math.Cos(2 * Math.PI * y);

                samples[i] += (float)(z * 0.2);
            }

            {
                ChartArea area2 = new ChartArea("Area2");
                Title title2 = new Title("Title2");
                title2.DockedToChartArea = "Area2";
                chart1.ChartAreas.Add(area2);
                chart1.Titles.Add(title2);
                {
                    var series = chart1.Series.Add("Samples+Noise");
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                    series.LegendText = "波形データ+ノイズ";
                    series.BorderWidth = 1;
                    series.ChartArea = "Area2";
                    var values = samples.Select(x => (double)x).ToList();
                    for (var i = 0; i < values.Count; i++) {
                        series.Points.Add(new DataPoint(i, values[i]));
                    }
                }
            }


            Decoder d = new Decoder(44100);
            var decorded = new List<byte>();
            d.onReceive += (_s, _v) => decorded.AddRange(_v);
            var rets = d.demodding(samples);
            d.decode(rets.Item5);
            Console.WriteLine(string.Join(", ", decorded.Select(x => $"0x{x:X2}")));
            {
                ChartArea area2 = new ChartArea("Area3");
                Title title2 = new Title("Title3");
                title2.DockedToChartArea = "Area3";
                chart1.ChartAreas.Add(area2);
                chart1.Titles.Add(title2);
                {
                    var series = chart1.Series.Add("LS");
                    series.ChartType = SeriesChartType.Line;
                    series.LegendText = "伝送波Low";
                    series.BorderWidth = 1;
                    series.ChartArea = "Area3";
                    for (var i = 0; i < rets.Item1.Length; i++) {
                        series.Points.Add(new DataPoint(i, rets.Item1[i]));
                    }
                }
                {
                    var series = chart1.Series.Add("HS");
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                    series.LegendText = "伝送波High";
                    series.BorderWidth = 1;
                    series.ChartArea = "Area3";
                    for (var i = 0; i < rets.Item2.Length; i++) {
                        series.Points.Add(new DataPoint(i, rets.Item2[i]));
                    }
                }
                {
                    var series = chart1.Series.Add("伝送波Low-伝送波High");
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                    series.LegendText = "伝送波High - 伝送波Low";
                    series.BorderWidth = 1;
                    series.ChartArea = "Area3";
                    for (var i = 0; i < rets.Item3.Length; i++) {
                        series.Points.Add(new DataPoint(i, rets.Item3[i]));
                    }
                }
                {
                    var series = chart1.Series.Add("伝送波Low-伝送波High(smooth)");
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                    series.LegendText = "伝送波High - 伝送波Low(smooth)";
                    series.BorderWidth = 1;
                    series.ChartArea = "Area3";
                    for (var i = 0; i < rets.Item4.Length; i++) {
                        series.Points.Add(new DataPoint(i, rets.Item4[i]));
                    }
                }
            }

            // 信号のトグル地点を算出
            {
                ChartArea area2 = new ChartArea("Area4");
                Title title2 = new Title("Title4");
                title2.DockedToChartArea = "Area4";
                chart1.ChartAreas.Add(area2);
                chart1.Titles.Add(title2);
                {
                    var series = chart1.Series.Add("信");
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                    series.LegendText = "toggles";
                    series.BorderWidth = 1;
                    series.ChartArea = "Area4";

                    var x = 0;
                    foreach (var p in rets.Item5) {
                        series.Points.Add(new DataPoint(x, p.Item1 * 10));
                        x += p.Item2;
                        series.Points.Add(new DataPoint(x, p.Item1 * 10));
                    }
                }
            }

            for (var i = 0; i < chart1.ChartAreas.Count; i++) {
                chart1.ChartAreas[i].AlignmentOrientation = AreaAlignmentOrientations.Vertical;
                chart1.ChartAreas[i].AlignmentStyle = AreaAlignmentStyles.AxesView;
                chart1.ChartAreas[i].AlignWithChartArea = chart1.ChartAreas[0].Name;
            }

        }

        private void fileFToolStripMenuItem_Click(object sender, EventArgs e) {

        }
    }

    public static class Ext
    {
        public static IList<double> Resample(this IList<double> self, int len) {
            var ret = new List<double>();
            var selfLen = self.Count();
            for (var i = 0; i < len; i++) {
                ret.Add(self[i * selfLen / len]);
            }
            return ret;
        }
    }
}
