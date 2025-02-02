using System.Drawing;
using System.Runtime.Versioning;

namespace JeqDB_Converter
{
    /// <summary>
    /// 変換処理クラス
    /// </summary>
    public static class Conv
    {
        /// <summary>
        /// csv1行のデータをData形式にします。
        /// </summary>
        /// <param name="text">csv1行</param>
        /// <returns>Data形式のデータ</returns>
        public static Data Text2Data(string text)
        {
            string[] datas = text.Split(',');
            return new Data
            {
                Time = DateTime.Parse($"{datas[0]} {datas[1]}"),
                Hypo = datas[2],
                Lat = LatLonString2Double(datas[3]),
                Lon = LatLonString2Double(datas[4]),
                Depth = (datas[2].StartsWith("詳細不明") && datas[5] == "0 km") ? -1 : int.Parse(datas[5].Replace(" km", "").Replace("不明", "-1")),//詳細不明震源で0kmの場合不明に
                Mag = double.Parse(datas[6].Replace("不明", "-1")),
                MaxInt = MaxIntString2Int(datas[7])
            };
        }

        /// <summary>
        /// 震源リスト1行のデータをData形式にします。
        /// </summary>
        /// <param name="text">csv1行</param>
        /// <returns>Data形式のデータ</returns>
        public static Data HypoText2Data(string text)
        {
            //0    1  2  3     4     5         6              7      8    9
            //2024 11 22 00:28 56.7  41° 1.5'N 141° 3.6'E   13     0.2  陸奥湾                   //33°18.2'N 135°27.9'E
            var datas = text.Replace("° ", "°").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return new Data
            {
                Time = DateTime.Parse($"{datas[0]}/{datas[1]}/{datas[2]} {datas[3]}:{datas[4]}"),
                Hypo = datas[9],
                Lat = LatLonString2Double(datas[5]),
                Lon = LatLonString2Double(datas[6]),
                Depth = int.Parse(datas[7]),
                Mag = double.Parse(datas[8].Replace("-", "-1")),
                MaxInt = -1
            };
        }

        /// <summary>
        /// 緯度や経度を60進数表記からdoubleに変換します。
        /// </summary>
        /// <param name="ll">緯度や経度</param>
        /// <returns>doubleの経度</returns>
        public static double LatLonString2Double(string ll)
        {
            string[] lls = ll.Replace("N", "").Replace("E", "").Replace("'", "").Split(['°', '′']);
            return double.Parse(lls[0]) + double.Parse(lls[1]) / 60d;
        }


        /// <summary>
        /// 緯度や経度を60進数表記からdoubleに変換します。
        /// </summary>
        /// <param name="ll">緯度や経度</param>
        /// <param name="isLat">緯度か</param>
        /// <returns>doubleの経度</returns>
        public static string LatLonDouble2String(double ll, bool isLat)
        {//37°12.6′N
            var d = (int)ll;
            var m = (ll - d) * 60;
            return $"{d}°{Math.Round(m, MidpointRounding.AwayFromZero):F1}′" + (isLat ? "N" : "E");
        }

        /// <summary>
        /// string形式の震度をint形式にします。
        /// </summary>
        /// <param name="maxInt">震度</param>
        /// <returns>int形式の震度(1~9)</returns>
        /// <exception cref="ArgumentException">値が不正の時</exception>
        public static int MaxIntString2Int(string maxInt)
        {
            return maxInt switch
            {
                null => -1,
                "---" => -1,
                "震度０" => 0,
                "震度１" => 1,
                "震度２" => 2,
                "震度３" => 3,
                "震度４" => 4,
                "震度５" => -5,
                "震度５弱" => 5,
                "震度５強" => 6,
                "震度６" => -7,
                "震度６弱" => 7,
                "震度６強" => 8,
                "震度７" => 9,
                _ => throw new ArgumentException("震度の変換に失敗しました。", nameof(maxInt)),
            };
        }

        /// <summary>
        /// int形式の震度をstring形式にします。
        /// </summary>
        /// <param name="maxInt">震度(1~9)</param>
        /// <param name="hankaku">数字を半角にする場合true</param>
        /// <returns>string形式の震度</returns>
        /// <exception cref="ArgumentException">値が不正の時</exception>
        public static string MaxIntInt2String(int maxInt, bool hankaku = false)
        {
            return maxInt switch
            {
                -1 => hankaku ? "---" : "---",
                0 => hankaku ? "震度0" : "震度０",
                1 => hankaku ? "震度1" : "震度１",
                2 => hankaku ? "震度2" : "震度２",
                3 => hankaku ? "震度3" : "震度３",
                4 => hankaku ? "震度4" : "震度４",
                -5 => hankaku ? "震度5" : "震度５",
                5 => hankaku ? "震度5弱" : "震度５弱",
                6 => hankaku ? "震度5強" : "震度５強",
                -7 => hankaku ? "震度6" : "震度６",
                7 => hankaku ? "震度6弱" : "震度６弱",
                8 => hankaku ? "震度6強" : "震度６強",
                9 => hankaku ? "震度7" : "震度７",
                _ => throw new ArgumentException("震度の変換に失敗しました。", nameof(maxInt)),
            };
        }

        [SupportedOSPlatform("windows")]//CA1416回避
        public static SolidBrush Depth2Color(int depth, int alpha = 204)
        {
            //震度データベースjsより
            double l = 50;
            double h = 0;
            if (depth <= 10)
                l = 50 - 25d * ((10 - depth) / 10d);
            else if (depth <= 20)
                h = 30d * ((depth - 10) / 10d);
            else if (depth <= 30)
                h = 30d + 30d * ((depth - 20) / 10d);
            else if (depth <= 50)
                h = 60;
            else if (depth <= 100)
            {
                h = 60 + 60d * ((depth - 50) / 50d);
                l = 50 + 25d * ((50 - depth) / 100d);
            }
            else if (depth <= 200)
            {
                h = 120 + 90d * ((depth - 100) / 100d);
                l = 25 - 30d * ((100 - depth) / 100d);
            }
            else if (depth <= 700)
            {
                h = 210 + 30d * ((depth - 200) / 500d);
                l = 55 + 30d * ((200 - depth) / 500d);
            }
            else
            {
                h = 240;
                l = 25;
            }
            return new SolidBrush(HSL2RGB((int)h, 100, (int)l, alpha));
        }

        public static Color HSL2RGB(int hue, int saturation, int lightness, int alpha = 255)
        {
            double h = hue / 360d;
            double s = saturation / 100d;
            double l = lightness / 100d;
            double r, g, b;
            if (s == 0)
                r = g = b = l;
            else
            {
                double q = l < 0.5 ? l * (1d + s) : l + s - l * s;
                double p = 2 * l - q;
                r = Hue2RGB(p, q, h + 1d / 3d);
                g = Hue2RGB(p, q, h);
                b = Hue2RGB(p, q, h - 1d / 3d);
            }
            return Color.FromArgb(alpha, (int)(255 * r), (int)(255 * g), (int)(255 * b));
        }

        public static double Hue2RGB(double p, double q, double t)
        {
            if (t < 0)
                t += 1;
            else if (t > 1)
                t -= 1;
            if (t < 1d / 6d)
                return p + (q - p) * 6 * t;
            else if (t < 1d / 2d)
                return q;
            else if (t < 2d / 3d)
                return p + (q - p) * (2d / 3d - t) * 6;
            return p;
        }
    }

    /// <summary>
    /// データ保存用クラス
    /// </summary>
    public class Data
    {
        /// <summary>
        /// 地震の発生日時
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// 震央地名
        /// </summary>
        public string Hypo { get; set; } = "";//CS8618回避

        /// <summary>
        /// 緯度
        /// </summary>
        public double Lat { get; set; }

        /// <summary>
        /// 経度
        /// </summary>
        public double Lon { get; set; }

        /// <summary>
        /// 深さ
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// マグニチュード
        /// </summary>
        public double Mag { get; set; }//不明を-1にしてるけど震源一覧でMマイナスあるからだめかも

        /// <summary>
        /// 最大震度
        /// </summary>
        public int MaxInt { get; set; }
    }

    public class Config
    {
        /// <summary>
        /// 画像の高さ
        /// </summary>
        public int MapSize { get; set; } = 1080;

        /// <summary>
        /// 緯度の始点
        /// </summary>
        public double LatSta { get; set; } = 20;

        /// <summary>
        /// 緯度の終点
        /// </summary>
        public double LatEnd { get; set; } = 50;

        /// <summary>
        /// 経度の始点
        /// </summary>
        public double LonSta { get; set; } = 120;

        /// <summary>
        /// 経度の終点
        /// </summary>
        public double LonEnd { get; set; } = 150;

        /// <summary>
        /// マグニチュードの大きさのタイプ
        /// </summary>
        /// <remarks>
        /// 11. [既定] マグニチュードx(画像の高さ÷216) <br/>
        /// 12. 11の2倍 <br/>
        /// 13. 11の3倍 <br/>
        /// 21. [マグニチュード強調] マグニチュードxマグニチュードx(画像の高さ÷216) <br/>
        /// 22. 21の2倍 <br/>
        /// </remarks>
        public int MagSizeType { get; set; } = 11;

        /// <summary>
        /// テキスト表示最小震度
        /// </summary>
        public int TextInt { get; set; } = 3;

        /// <summary>
        /// [動画のみ]描画開始日時
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// [動画のみ]描画終了日時
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// [動画のみ]描画間隔
        /// </summary>
        public TimeSpan DrawSpan { get; set; }

        /// <summary>
        /// [動画のみ]完全に消えるまで
        /// </summary>
        public TimeSpan DisappTime { get; set; }
    }

    /// <summary>
    /// 描画色の設定
    /// </summary>
    public class Config_Color
    {
        /// <summary>
        /// 地図の色
        /// </summary>
        public MapColor Map { get; set; } = new MapColor();

        /// <summary>
        /// 地図の色
        /// </summary>
        public class MapColor
        {
            /// <summary>
            /// 海洋の塗りつぶし色
            /// </summary>
            public Color Sea { get; set; } = Color.FromArgb(30, 30, 60);

            /// <summary>
            /// 世界(日本除く)の塗りつぶし色
            /// </summary>
            public Color World { get; set; } = Color.FromArgb(100, 100, 150);
            /*
            /// <summary>
            /// 世界(日本除く)の境界線色
            /// </summary>
            public Color World_Border { get; set; }
            */
            /// <summary>
            /// 日本の塗りつぶし色
            /// </summary>
            public Color Japan { get; set; } = Color.FromArgb(90, 90, 120);

            /// <summary>
            /// 日本の境界線色
            /// </summary>
            public Color Japan_Border { get; set; } = Color.FromArgb(127, 255, 255, 255);
        }

        /// <summary>
        /// 右側部分背景色
        /// </summary>
        public Color InfoBack { get; set; } = Color.FromArgb(30, 60, 90);

        /// <summary>
        /// 右側部分等テキスト色
        /// </summary>
        public Color Text { get; set; } = Color.FromArgb(255, 255, 255);

        /// <summary>
        /// 震央円の透明度
        /// </summary>
        public int Hypo_Alpha { get; set; } = 204;
    }
}
