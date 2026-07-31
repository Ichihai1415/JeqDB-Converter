using System.Drawing;
using System.Runtime.Versioning;
using System.Text.Json.Serialization;

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
            if (datas[1].EndsWith("データ")) throw new Exception("処理ミスです。備考:datas[1]が" + datas[1] + "です。");
            //2021/04/11,05:30:08.0,詳細不明,29°13.0′N,129°20.0′E,0 km,不明,震度１
            //2008/05/12,15:41:53.0,詳細不明（阿蘇山付近）,32°53.0′N,131°05.0′E,0 km,不明,震度１
            //1981/06/26,時分不明データ,不明,不明,不明,不明,震度１
            //1962年08月,日時分不明データ,不明,不明,不明,不明,震度１
            return new Data
            {
                Time = DateTime.Parse($"{datas[0]} {datas[1]}"),
                Hypo = datas[2],
                Lat = LatLonString2Double(datas[3]),
                Lon = LatLonString2Double(datas[4]),
                Depth = datas[2].StartsWith("詳細不明") || datas[5] == "不明" ? null : double.Parse(datas[5].Replace(" km", "")),//震源、規模不明なとき
                Mag = datas[6] == "不明" ? double.NaN : double.Parse(datas[6]),
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
                Depth = double.Parse(datas[7]),
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
        /// 緯度や経度をdoubleから60進数表記に変換します。
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
        /// string形式の震度をstring形式にします。
        /// </summary>
        /// <param name="maxInt">震度</param>
        /// <returns>int形式の震度(1~9)</returns>
        /// <exception cref="ArgumentException">値が不正の時</exception>
        public static string MaxIntP2PInt2String(int maxInt)
        {
            return maxInt switch
            {
                -1 => "---",
                10 => "震度１",
                20 => "震度２",
                30 => "震度３",
                40 => "震度４",
                45 => "震度５弱",
                50 => "震度５強",
                55 => "震度６弱",
                60 => "震度６強",
                70 => "震度７",
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
                -1 => hankaku ? " - - - - - " : " - - - - - ",
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
        public static SolidBrush Depth2Color(double? depth, Config_Color? config = null, int alpha = 204)
        {
            config ??= new Config_Color();
            var lv = config.DepthLevel;
            var d = depth == null ? 0d : (double)depth;
            if (d < 0) d = 0;
            //Console.WriteLine(depth + "-" + d);
            /*//震度データベースjsより https://www.data.jma.go.jp/eqdb/data/shindo/assets/index.js
    const yT = e => {
    let n = 0;
    return e === "不明" || parseFloat(e) < 1 || e === "" || Number.isNaN(parseFloat(e)) ? n = 1 * 2.5 : parseFloat(e) > 8 ? n = 8 * 2.5 : n = parseFloat(e) * 2.5,
    n
}
  , _T = e => {
    let t = 0
      , n = Number(e)
      , i = 50;
n <= 10 ? (i = 50 - 25 * ((10 - n) / 10), t = 0) : 
n <= 20 ? t = 0 + 30 * ((n - 10) / 10) : 
n <= 30 ? t = 30 + 30 * ((n - 20) / 10) : 
n <= 50 ? t = 60 : 
n <= 100 ? (t = 60 + 60 * ((n - 50) / 50), i = 50 + 25 * ((50 - n) / 100)) : 
n <= 200 ? (t = 120 + 90 * ((n - 100) / 100), i = 25 - 30 * ((100 - n) / 100)) : 
n <= 700 ? (t = 210 + 30 * ((n - 200) / 500), i = 55 + 30 * ((200 - n) / 500)) : (t = 240, i = 25),
`hsl(${t}, 100%, ${i}%)`
             */

            /*震央分布 html内のscript
             V = function(a) {
                            var i = 0
                              , e = Number(a)
                              , n = 50;
                            return e <= 10 ? (n = 50 - 25 * ((10 - e) / 10),
                            i = 0) : e <= 20 ? i = 0 + 30 * ((e - 10) / 10) : e <= 30 ? i = 30 + 30 * ((e - 20) / 10) : e <= 50 ? i = 60 : e <= 100 ? (i = 60 + 60 * ((e - 50) / 50),
                            n = 50 + 25 * ((50 - e) / 100)) : e <= 200 ? (i = 120 + 90 * ((e - 100) / 100),
                            n = 25 - 30 * ((100 - e) / 100)) : e <= 700 ? (i = 210 + 30 * ((e - 200) / 500),
                            n = 55 + 30 * ((200 - e) / 500)) : (i = 240,
                            n = 25),
                            {
                                weight: .5,
                                color: "#000000",
                                fillColor: "hsl(" + i + ", 100%, " + n + "%)",
                                opacity: 1,
                                fillOpacity: .8
                            }
             */
            /* hypLeg.svg コメントアウトされた色がある(50km,100km)
<linearGradient id="grad2"  x1="100" y1="100" x2="380" y2="100" gradientUnits="userSpaceOnUse" spreadMethod="repeat">
  <stop  offset="0%" stop-color="#800000" />
  <stop  offset="14.3%" stop-color="#ff0000"/>
  <stop  offset="28.5%" stop-color="#ff8c00"/>
  <stop  offset="42.9%" stop-color="#ffff00"/>
  <stop  offset="57.1%" stop-color="#ffff00"/>
  <stop  offset="71.4%" stop-color="#008000"/>
  <!--
  <stop  offset="57.1%" stop-color="#00ff00"/>
  <stop  offset="71.4%" stop-color="#008000"/>
  -->
  <stop  offset="85.7%" stop-color="#1e90ff"/>
  <stop  offset="100%" stop-color="#00008b"/>
</linearGradient>
             */
            var l = 50d;
            var h = 0d;
            if (d <= lv.L1)
                l = 50 - 25d * ((lv.L1 - d) / lv.L1);
            else if (d <= lv.L2)
                h = 30d * ((d - lv.L1) / (lv.L2 - lv.L1));
            else if (d <= lv.L3)
                h = 30d + 30d * ((d - lv.L2) / (lv.L3 - lv.L2));
            else if (d <= lv.L4)
                h = 60d;
            else if (d <= lv.L5)
            {
                h = 60d + 60d * ((d - lv.L4) / (lv.L5 - lv.L4));
                l = 50d + 25d * ((lv.L4 - d) / (lv.L5 - lv.L4));//jsではミス？で/100= /lv.L5 - lv.L4)/2 or /(lv.L6 - lv.L5))になってる
            }
            else if (d <= lv.L6)
            {
                h = 120d + 90d * ((d - lv.L5) / (lv.L6 - lv.L5));
                l = 25d - 30d * ((lv.L5 - d) / (lv.L6 - lv.L5));
            }
            else if (d <= lv.L7)
            {
                h = 210d + 30d * ((d - lv.L6) / (lv.L7 - lv.L6));
                l = 55d + 30d * ((lv.L6 - d) / (lv.L7 - lv.L6));
            }
            else
            {
                h = 240d;
                l = 25d;
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
        public double? Depth { get; set; } = null;

        /// <summary>
        /// マグニチュード
        /// </summary>
        public double Mag { get; set; } = double.NaN;

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
        /// マグニチュード・深さ凡例
        /// </summary>
        public bool EnableLegend { get; set; } = true;

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
        public int Hypo_Alpha { get; set; } = 153;//公式の変更より204から変更

        /// <summary>
        /// マグニチュード凡例の塗りつぶし
        /// </summary>
        public Color Legend_Mag_Fill { get; set; } = Color.Red;


        public DepthColorLevel DepthLevel { get; set; } = new();

        /// <summary>
        /// 気象庁標準の深さ色の段階の深さの値
        /// </summary>
        public class DepthColorLevel
        {
            /// <summary>
            /// レベル1: 標準10km
            /// </summary>
            public int L1 { get; set; } = 10;

            /// <summary>
            /// レベル2: 標準20km
            /// </summary>
            public int L2 { get; set; } = 20;

            /// <summary>
            /// レベル3: 標準30km
            /// </summary>
            public int L3 { get; set; } = 30;

            /// <summary>
            /// レベル4: 標準50km
            /// </summary>
            public int L4 { get; set; } = 50;

            /// <summary>
            /// レベル5: 標準100km
            /// </summary>
            public int L5 { get; set; } = 100;

            /// <summary>
            /// レベル6: 標準200km
            /// </summary>
            public int L6 { get; set; } = 200;

            /// <summary>
            /// レベル7: 標準700km
            /// </summary>
            public int L7 { get; set; } = 700;
        }
    }

    /// <summary>
    /// 震央分布気象庁内部API
    /// </summary>
    public class JMAEpicenters
    {
        [JsonPropertyName("type")]
        public required string Type { get; set; }

        [JsonPropertyName("features")]
        public required C_Feature[] Features { get; set; }

        public class C_Feature
        {
            [JsonPropertyName("type")]
            public required string Type { get; set; }

            [JsonPropertyName("geometry")]
            public required C_Geometry Geometry { get; set; }

            [JsonPropertyName("properties")]
            public required C_Properties Properties { get; set; }


            public class C_Geometry
            {
                [JsonPropertyName("type")]
                public required string Type { get; set; }

                [JsonPropertyName("coordinates")]
                public required double[] Coordinates { get; set; }
            }

            public class C_Properties
            {
                [JsonPropertyName("date")]
                public required string Date { get; set; }

                [JsonPropertyName("dep")]
                public required string Dep { get; set; }

                [JsonPropertyName("mag")]
                public required string Mag { get; set; }

                [JsonPropertyName("mj")]
                public required string Mj { get; set; }

                [JsonPropertyName("place")]
                public required string Place { get; set; }

                [JsonPropertyName("si")]
                public required string Si { get; set; }

                [JsonPropertyName("aflag")]
                public required string Aflag { get; set; }

                [JsonPropertyName("flag")]
                public required string Flag { get; set; }
            }
        }
    }

    public class P2PQuakeV2_JMAQuake
    {
        [JsonPropertyName("code")]
        public required int Code { get; set; }

        [JsonPropertyName("comments")]
        public required C_Comments Comments { get; set; }

        [JsonPropertyName("created_at")]
        public required string CreatedAt { get; set; }

        [JsonPropertyName("earthquake")]
        public required C_Earthquake Earthquake { get; set; }

        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("issue")]
        public required C_Issue Issue { get; set; }

        [JsonPropertyName("points")]
        public required C_Point[] Points { get; set; }

        [JsonPropertyName("time")]
        public required string Time { get; set; }

        [JsonPropertyName("timestamp")]
        public required C_Timestamp Timestamp { get; set; }

        [JsonPropertyName("user_agent")]
        public required string UserAgent { get; set; }

        [JsonPropertyName("ver")]
        public required string Ver { get; set; }

        public class C_Comments
        {
            [JsonPropertyName("freeFormComment")]
            public required string FreeFormComment { get; set; }
        }
        public class C_Earthquake
        {
            [JsonPropertyName("domesticTsunami")]
            public required string DomesticTsunami { get; set; }

            [JsonPropertyName("foreignTsunami")]
            public required string ForeignTsunami { get; set; }

            [JsonPropertyName("hypocenter")]
            public required C_Hypocenter Hypocenter { get; set; }

            [JsonPropertyName("maxScale")]
            public required int MaxScale { get; set; }

            [JsonPropertyName("time")]
            public required string Time { get; set; }
            public class C_Hypocenter
            {
                [JsonPropertyName("depth")]
                public required int Depth { get; set; }

                [JsonPropertyName("latitude")]
                public required double Latitude { get; set; }

                [JsonPropertyName("longitude")]
                public required double Longitude { get; set; }

                [JsonPropertyName("magnitude")]
                public required double Magnitude { get; set; }

                [JsonPropertyName("name")]
                public required string Name { get; set; }
            }
        }

        public class C_Issue
        {
            [JsonPropertyName("correct")]
            public required string Correct { get; set; }

            [JsonPropertyName("source")]
            public required string Source { get; set; }

            [JsonPropertyName("time")]
            public required string Time { get; set; }

            [JsonPropertyName("type")]
            public required string Type { get; set; }
        }

        public class C_Point
        {
            [JsonPropertyName("addr")]
            public required string Addr { get; set; }

            [JsonPropertyName("isArea")]
            public required bool IsArea { get; set; }

            [JsonPropertyName("pref")]
            public required string Pref { get; set; }

            [JsonPropertyName("scale")]
            public required int Scale { get; set; }
        }

        public class C_Timestamp
        {
            [JsonPropertyName("convert")]
            public required string Convert { get; set; }

            [JsonPropertyName("register")]
            public required string Register { get; set; }
        }
    }

}
