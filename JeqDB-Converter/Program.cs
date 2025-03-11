using AngleSharp.Html.Parser;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static JeqDB_Converter.Conv;

namespace JeqDB_Converter
{
    internal class Program
    {
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
        /// <summary>
        /// 文字描画用フォント
        /// </summary>
        public static FontFamily font;
        /// <summary>
        /// 右寄せ用
        /// </summary>
        public static StringFormat string_Right;
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
        /// <summary>
        /// 配色設定
        /// </summary>
        public static Config_Color color = new();

        /// <summary>
        /// JSON出力設定
        /// </summary>
        public readonly static JsonSerializerOptions jsonOption = new() { WriteIndented = true };

        static void Main()//todo:何か特殊文字入力で中止
        {
            //debug
            //Console.WriteLine(@"C:\Ichihai1415\source\vs\JeqDB-Converter\JeqDB-Converter\bin\x64\Debug\net9.0\output\image\x_191901010000-202401010000.csv");
            //Console.WriteLine(@"C:\Ichihai1415\source\vs\JeqDB-Converter\JeqDB-Converter\bin\x64\Debug\net9.0\output\image\null.csv");

            jsonOption.Converters.Add(new ColorConverter());
            var pfc = new PrivateFontCollection();
            pfc.AddFontFile("Koruri-Regular.ttf");
            font = pfc.Families[0];
            string_Right = new()
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Far
            };
            Directory.CreateDirectory("output");
            if (File.Exists("colors.json"))
                color = JsonSerializer.Deserialize<Config_Color>(File.ReadAllText("colors.json"), jsonOption) ?? new Config_Color();
            File.WriteAllText("colors.json", JsonSerializer.Serialize(color, jsonOption));
            ConWrite("" +
                "|\n" +
                "|\n" +
                "|        JeqDB-Converter v1.1.0\n" +
                "|        https://github.com/Ichihai1415/JeqDB-Converter\n" +
                "|        READMEを確認してください。\n" +
                "|\n" +
                "+────────────────────────────────────────────────────────────\n");

        restart:
            ConWrite("モードを入力してください。");
            ConWrite("> 1.複数ファイルの結合");
            ConWrite("> 2.画像描画");
            ConWrite("> 3.動画作成");
            ConWrite("> 4.csv取得");
            ConWrite("> 5.震源リスト取得");
            ConWrite("> 0.終了");
            int mode;
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                var select = Console.ReadLine();
                if (int.TryParse(select, null, out int selectNum))
                    if (0 <= selectNum && selectNum <= 5)
                    {
                        mode = selectNum;
                        break;
                    }
                ConWrite("値は0から5の間である必要があります。", ConsoleColor.Red);
            }
            switch (mode)
            {
                case 0:
                    Console.ForegroundColor = defaultColor;
                    Environment.Exit(0);
                    break;
                case 1:
                    MergeFiles();
                    break;
                case 2:
                    DrawImage();
                    break;
                case 3:
                    ReadyVideo();
                    break;
                case 4:
                    GetCsv();
                    break;
                case 5:
                    GetHypo();
                    break;
            }
            Console.WriteLine();
            goto restart;
        }

        /// <summary>
        /// ファイルを結合します。
        /// </summary>
        public static void MergeFiles()
        {
            ConWrite("結合するファイルのパスを1行ごとに入力してください。空文字が入力されたら結合を開始します。フォルダのパスを入力するとすべて読み込みます。※観測震度検索をしているものとしていないものの結合はできますが他ソフトで処理をする際エラーとなる可能性があります。このソフトでは問題ありません。");
            List<string> files = [];
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                var file = Console.ReadLine();
                if (!string.IsNullOrEmpty(file))
                    files.Add(file.Replace("\"", ""));
                else if (files.Count < 2)
                {
                    ConWrite("中止します。");
                    return;
                }
                else
                    break;
            }

            var stringBuilder = new StringBuilder();
            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    ConWrite("読み込み中... ", false);
                    ConWrite(file, ConsoleColor.Green);
                    stringBuilder.Append(File.ReadAllText(file).Replace("地震の発生日,地震の発生時刻,震央地名,緯度,経度,深さ,Ｍ,最大震度,検索対象最大震度\n", "").Replace("地震の発生日,地震の発生時刻,震央地名,緯度,経度,深さ,Ｍ,最大震度\n", ""));
                }
                else
                    ConWrite($"{file}が見つかりません。", ConsoleColor.Red);
            }
            stringBuilder.Insert(0, "地震の発生日,地震の発生時刻,震央地名,緯度,経度,深さ,Ｍ,最大震度\n");
            ConWrite("読み込みました。保存するパスを入力してください。すでにある場合上書きされます。");
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                string path = Console.ReadLine() ?? "";
                try
                {
                    File.WriteAllText(path.Replace("\"", ""), stringBuilder.ToString());
                    break;
                }
                catch (Exception ex)
                {
#if DEBUG
                    ConWrite("エラーが発生しました。" + ex + "\n再度実行してください。", ConsoleColor.Red);
#else
                    ConWrite("エラーが発生しました。" + ex.Message + " 再度実行してください。", ConsoleColor.Red);
#endif
                }
            }
            ConWrite("保存しました。最初の行の\",検索対象最大震度\"は付かないため含まれる場合手動で追加してください。");
        }

        /// <summary>
        /// ファイルを結合します。
        /// </summary>
        public static string MergeFiles(string[] files)
        {
            if (files.Length == 0)
            {
                ConWrite("結合するファイルのパスを1行ごとに入力してください。空文字が入力されたら結合を開始します。フォルダのパスを入力するとすべて読み込みます。※観測震度検索をしているものとしていないものの結合はできますが他ソフトで処理をする際エラーとなる可能性があります。このソフトでは問題ありません。");
                List<string> filesTmp = [];
                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    var file = Console.ReadLine();
                    if (!string.IsNullOrEmpty(file))
                        filesTmp.Add(file.Replace("\"", ""));
                    else if (filesTmp.Count < 2)
                    {
                        ConWrite("中止します。");
                        return "";
                    }
                    else
                        break;
                }
                files = [.. filesTmp];
            }

            var stringBuilder = new StringBuilder();
            List<string> files2 = [];
            foreach (var file in files)
            {
                var f = file.Replace("\"", "");
                if (f.EndsWith(".csv"))
                    files2.Add(f);
                else
                {
                    ConWrite("ファイル名取得中... ", false);
                    ConWrite(f, ConsoleColor.Green);
                    var openPaths = Directory.EnumerateFiles(f, "*.csv", SearchOption.AllDirectories);
                    foreach (var path in openPaths)
                        files2.Add(path);
                }
            }
            foreach (var file in files2)
            {
                if (File.Exists(file))
                {
                    ConWrite("読み込み中... ", false);
                    ConWrite(file, ConsoleColor.Green);
                    stringBuilder.Append(File.ReadAllText(file).Replace("地震の発生日,地震の発生時刻,震央地名,緯度,経度,深さ,Ｍ,最大震度,検索対象最大震度\n", "").Replace("地震の発生日,地震の発生時刻,震央地名,緯度,経度,深さ,Ｍ,最大震度\n", ""));
                }
                else
                    ConWrite($"{file}が見つかりません。", ConsoleColor.Red);
            }
            stringBuilder.Insert(0, "地震の発生日,地震の発生時刻,震央地名,緯度,経度,深さ,Ｍ,最大震度\n");
            return stringBuilder.ToString();
        }

        /// <summary>
        /// 右下マグニチュード凡例用(11~13用)(0は不使用)　mapSize=216での右欄(168*216)内でのx座標
        /// </summary>
        public static readonly int[] LEGEND_MAG_X_1X = [-1, 6, 17, 30, 46, 65, 87, 112, 140];

        /// <summary>
        /// 右下深さ凡例値
        /// </summary>
        public static readonly int[] LEGEND_DEP_EX = [0, 10, 20, 30, 50, 100, 300, 700];

        /// <summary>
        /// 右下マグニチュード凡例用(21~22用)(0は不使用)　mapSize=216での右欄(168*216)内での座標(yは適当基準(コード参照))
        /// </summary>
        public static readonly int[,] LEGEND_MAG_X_2X = { { -1, -1 }, { 13, 50 }, { 11, 62 }, { 7, 80 }, { 20, 73 }, { 40, 64 }, { 70, 53 }, { 111, 40 } };


        /// <summary>
        /// 画像を描画します。
        /// </summary>
        public static void DrawImage()
        {
#if !TEST
            ConWrite("読み込むcsvファイルのパスを入力してください。");
#endif
            try
            {
#if TEST
                var datas_ = File.ReadAllLines("C:\\Users\\proje\\Downloads\\地震リスト (n).csv");
#else
                Console.ForegroundColor = ConsoleColor.Cyan;
                var path = Console.ReadLine() ?? "";
                Console.ForegroundColor = ConsoleColor.Cyan;
                var datas_ = File.ReadAllLines(path.Replace("\"", ""));
#endif
                ConWrite("変換中...");
                IEnumerable<Data> datas = datas_.Where(x => x.Contains('°')).Where(x => !x.Contains("不明データ")).Select(Text2Data).OrderBy(a => a.Time);//データじゃないやつついでに緯度経度ないやつも除外
#if TEST
                Config config = new()
                {
                    MapSize = 2160,
                    LatSta = 20,
                    LatEnd = 50,
                    LonSta = 120,
                    LonEnd = 150,
                    MagSizeType = 22,
                    TextInt = 1
                };
#else
                var config = new Config()
                {
                    MapSize = (int)UserInput("画像の高さを入力してください。幅は16:9になるように計算されます。例:720/1080/2160/4320", typeof(int), "1080"),
                    LatSta = (double)UserInput("緯度の始点(地図の下端)を入力してください。例:20", typeof(double), "20"),
                    LatEnd = (double)UserInput("緯度の終点(地図の上端)を入力してください。例:50", typeof(double), "50"),
                    LonSta = (double)UserInput("経度の始点(地図の左端)を入力してください。例:120", typeof(double), "120"),
                    LonEnd = (double)UserInput("経度の終点(地図の右端)を入力してください。例:150", typeof(double), "150"),
                    MagSizeType = (int)UserInput("円の描画サイズのタイプを入力してください。マグニチュードは1未満の場合1に置き換えられます。\n" +
                    "> 11. [既定] マグニチュードx(画像の高さ÷216)\n" +
                    "> 12. 11の2倍\n" +
                    "> 13. 11の3倍\n" +
                    "> 21. [マグニチュード強調] マグニチュードxマグニチュードx(画像の高さ÷216)\n" +
                    "> 22. 21の2倍", typeof(int), "11"),
                    TextInt = (int)UserInput("右欄に表示する最小震度を入力してください。震度2:2 震度5弱,震度5(強弱なし):5 震度6強:8のようにしてください。例:3", typeof(int), "3"),
                    EnableLegend = (bool)UserInput("マグニチュード・深さの凡例を描画しますか？(y/n)", typeof(bool), "y")
                };
#endif
                var savePath = $"output\\image\\{DateTime.Now:yyyyMMddHHmmss}.png";

                ConWrite("描画中...");
                var zoomW = config.MapSize / (config.LonEnd - config.LonSta);
                var zoomH = config.MapSize / (config.LatEnd - config.LatSta);
                var sizeX = config.MagSizeType - (config.MagSizeType / 10 * 10);//倍率


                var bitmap = DrawMap(config);
                var g = Graphics.FromImage(bitmap);

                var texts = new StringBuilder[] { new("\n"), new("\n"), new("\n\n"), new("\n"), new("\n") };
                var alpha = color.Hypo_Alpha;

                var font_msD45 = new Font(font, config.MapSize / 45f, GraphicsUnit.Pixel);
                var sb_text = new SolidBrush(color.Text);
                var sb_text_sub = new SolidBrush(Color.FromArgb(127, color.Text));
                var pen_hypo = new Pen(Color.FromArgb(alpha, 127, 127, 127));
                var pen_line = new Pen(Color.FromArgb(127, color.Text), config.MapSize / 1080f);

                foreach (var data in datas)
                {
                    var size = (float)(config.MagSizeType / 10 == 1
                        ? (Math.Max(1, data.Mag) * config.MapSize / 216d)
                        : (Math.Max(1, data.Mag) * (Math.Max(1, data.Mag) * config.MapSize / 216d))) * sizeX;//精度と統一のためd
                    g.FillEllipse(Depth2Color(data.Depth, alpha), (float)(((data.Lon - config.LonSta) * zoomW) - size / 2f), (float)(((config.LatEnd - data.Lat) * zoomH) - size / 2f), size, size);
                    g.DrawEllipse(pen_hypo, (float)(((data.Lon - config.LonSta) * zoomW) - size / 2f), (float)(((config.LatEnd - data.Lat) * zoomH) - size / 2f), size, size);
                    if (Math.Abs(data.MaxInt) >= config.TextInt)
                    {
                        texts[0].AppendLine(data.Time.ToString("yyyy/MM/dd HH:mm:ss.f"));
                        texts[1].AppendLine(data.Hypo);//詳細不明の可能性
                        texts[2].Append(data.Depth == -1 ? "不明" : data.Depth.ToString());
                        texts[2].AppendLine(data.Depth == -1 ? "" : "km");
                        texts[3].Append(data.Mag == -1d ? "不明" : 'M');
                        texts[3].AppendLine(data.Mag == -1d ? "" : data.Mag.ToString("0.0"));
                        texts[4].AppendLine(MaxIntInt2String(data.MaxInt, true));
                    }
                }
                var depthSize = g.MeasureString(texts[2].ToString(), font_msD45);//string Formatに必要
                var depthHeadSize = g.MeasureString("999km\n深さ", font_msD45);//999kmは最大幅計算用
                var oneLineHeight = g.MeasureString("999km", font_msD45).Height;//調整用

                g.FillRectangle(new SolidBrush(color.InfoBack), config.MapSize, 0, bitmap.Width - config.MapSize, config.MapSize);
                g.DrawString("発生日時", font_msD45, sb_text_sub, config.MapSize, 0);
                g.DrawString("震央", font_msD45, sb_text_sub, config.MapSize * 1.25f, 0);
                g.DrawString("999km\n深さ", font_msD45, sb_text_sub, new RectangleF(new PointF(config.MapSize * 1.5f, -oneLineHeight), depthHeadSize), string_Right);
                g.DrawString("規模", font_msD45, sb_text_sub, config.MapSize * 1.5875f, 0);
                g.DrawString("最大震度", font_msD45, sb_text_sub, config.MapSize * 1.675f, 0); g.DrawString(texts[0].ToString(), font_msD45, sb_text, config.MapSize, 0);

                g.DrawString(texts[0].ToString(), font_msD45, sb_text, config.MapSize, 0);
                g.DrawString(texts[1].ToString(), font_msD45, sb_text, config.MapSize * 1.25f, 0);
                g.DrawString(texts[2].ToString(), font_msD45, sb_text, new RectangleF(new PointF(config.MapSize * 1.5f, -oneLineHeight), depthSize), string_Right);
                g.DrawString(texts[3].ToString(), font_msD45, sb_text, config.MapSize * 1.5875f, 0);
                g.DrawString(texts[4].ToString(), font_msD45, sb_text, config.MapSize * 1.675f, 0);
                g.DrawLine(pen_line, config.MapSize, config.MapSize / 30f, bitmap.Width, config.MapSize / 30f);
                g.DrawImage(DrawLegend(config), 0, 0);

                Directory.CreateDirectory("output\\image");
                bitmap.Save(savePath, ImageFormat.Png);
#if TEST
                ConWrite(Path.GetFullPath(savePath), ConsoleColor.Green);
#endif
                ConWrite($"{savePath} : {datas.Count()}", ConsoleColor.Green);
                g.Dispose();
                bitmap.Dispose();
            }
            catch (Exception ex)
            {
#if DEBUG
                ConWrite("エラーが発生しました。" + ex + "\n再度実行してください。", ConsoleColor.Red);
#else
                ConWrite("エラーが発生しました。" + ex.Message + " 再度実行してください。", ConsoleColor.Red);
#endif                
            }
        }

        /// <summary>
        /// 動画用画像を描画します。
        /// </summary>
        public static void ReadyVideo()//todo:終わったやつを消す?
        {//todo:円と右欄を結ぶ？
            // 36.4 -  38.4
            //136.2 - 138.2
#if !TEST
            ConWrite("読み込むcsvファイルのパスを入力してください。複数読み込む場合は\\だけ入力してください。");
#endif
            try
            {
#if TEST
                var datas_ = File.ReadAllLines("C:\\Users\\proje\\Downloads\\地震リスト (y).csv");
#else
                Console.ForegroundColor = ConsoleColor.Cyan;
                var path = Console.ReadLine() ?? "";

                var datas_ = path == "\\" ? MergeFiles([]).Replace("\r", "").Split('\n') : File.ReadAllLines(path.Replace("\"", ""));//gitで触ると\r付く
#endif
                ConWrite("変換中...");
                IEnumerable<Data> datas = datas_.Where(x => x.Contains('°')).Where(x => !x.Contains("不明データ")).Select(Text2Data).OrderBy(a => a.Time);//データじゃないやつついでに緯度経度ないやつも除外
#if TEST
                var config = new Config()
                {
                    StartTime = new DateTime(2023, 1, 1),
                    EndTime = new DateTime(2024, 1, 1),
                    DrawSpan = new TimeSpan(1, 0, 0),
                    DisappTime = new TimeSpan(60, 0, 0),
                    MapSize = 1080,
                    LatSta = 20,
                    LatEnd = 50,
                    LonSta = 120,
                    LonEnd = 150,
                    MagSizeType = 21,
                    TextInt = 3
                };
#else
                var config = new Config()
                {
                    StartTime = (DateTime)UserInput("開始日時を入力してください。例(2023年1月1日):2023/01/01 00:00:00または2023/01/01", typeof(DateTime)),
                    EndTime = (DateTime)UserInput("終了日時を入力してください。この時間未満まで描画されます。例(2024年1月1日):2024/01/01 00:00:00または2024/01/01", typeof(DateTime)),
                    DrawSpan = (TimeSpan)UserInput("描画間隔を入力してください。この時間毎に描画時刻からこの時間までのものが描画されます。例(6時間):06:00:00", typeof(TimeSpan)),
                    DisappTime = (TimeSpan)UserInput("消失時間を入力してください。発生日時からこの時間過ぎたら完全に消えます。例(1日):1.00:00:00", typeof(TimeSpan)),
                    MapSize = (int)UserInput("画像の高さを入力してください。幅は16:9になるように計算されます。例:720/1080/2160/4320", typeof(int), "1080"),
                    LatSta = (double)UserInput("緯度の始点(地図の下端)を入力してください。例:20", typeof(double), "20"),
                    LatEnd = (double)UserInput("緯度の終点(地図の上端)を入力してください。例:50", typeof(double), "50"),
                    LonSta = (double)UserInput("経度の始点(地図の左端)を入力してください。例:120", typeof(double), "120"),
                    LonEnd = (double)UserInput("経度の終点(地図の右端)を入力してください。例:150", typeof(double), "150"),
                    MagSizeType = (int)UserInput("円の描画サイズのタイプを入力してください。マグニチュードは1未満の場合1に置き換えられます。\n" +
                    "> 11. [既定] マグニチュードx(画像の高さ÷216)\n" +
                    "> 12. 11の2倍\n" +
                    "> 13. 11の3倍\n" +
                    "> 21. [マグニチュード強調] マグニチュードxマグニチュードx(画像の高さ÷216)\n" +
                    "> 22. 21の2倍", typeof(int), "11"),
                    TextInt = (int)UserInput("右欄に表示する最小震度を入力してください。震度2:2 震度5弱,震度5(強弱なし):5 震度6強:8のようにしてください。", typeof(int), "3"),
                    EnableLegend = (bool)UserInput("マグニチュード・深さの凡例を描画しますか？(y/n)", typeof(bool), "y")
                };
#endif
                ConWrite("描画中...");
                var saveDir = $"output\\videoimage\\{DateTime.Now:yyyyMMddHHmmss}";
                var dataSum = datas.Count();
                Directory.CreateDirectory(saveDir);
                ConWrite("dir: " + saveDir, ConsoleColor.Green);
                var zoomW = config.MapSize / (config.LonEnd - config.LonSta);
                var zoomH = config.MapSize / (config.LatEnd - config.LatSta);
                var sizeX = config.MagSizeType - (config.MagSizeType / 10 * 10);//倍率
                var drawTime = config.StartTime;//描画対象時間
                var bitmap_baseMap = DrawMap(config);
                var bitmap_legend = DrawLegend(config);

                //各描画開始
                for (var i = 1; drawTime < config.EndTime; i++)//DateTime:古<新==true
                {
                    datas = datas.SkipWhile(data => data.Time < drawTime - config.DisappTime).ToList();//除外//SkipWhileなので.OrderBy(a => a.Time)で並び替えられていることが必要
                    var datas_Draw = datas.Where(data => data.Time < drawTime + config.DrawSpan);//抜き出し

                    using var bitmap = (Bitmap)bitmap_baseMap.Clone();
                    using var g = Graphics.FromImage(bitmap);

                    var texts = new StringBuilder[] { new("\n"), new("\n"), new("\n\n"), new("\n"), new("\n") };
                    var alpha = color.Hypo_Alpha;

                    var font_msD45 = new Font(font, config.MapSize / 45f, GraphicsUnit.Pixel);
                    var sb_text = new SolidBrush(color.Text);
                    var sb_text_sub = new SolidBrush(Color.FromArgb(127, color.Text));
                    var pen_hypo = new Pen(Color.FromArgb(alpha, 127, 127, 127));
                    var pen_line = new Pen(Color.FromArgb(127, color.Text), config.MapSize / 1080f);

                    foreach (var data in datas_Draw)//imageとの違い
                    {
                        //imageとの違い
                        alpha = data.Time >= drawTime ? color.Hypo_Alpha : (int)((1d - (drawTime - data.Time).TotalSeconds / config.DisappTime.TotalSeconds) * color.Hypo_Alpha);//消える時間の割合*基本透明度
                        var size = (float)(config.MagSizeType / 10 == 1
                            ? (Math.Max(1, data.Mag) * config.MapSize / 216d)
                            : (Math.Max(1, data.Mag) * (Math.Max(1, data.Mag) * config.MapSize / 216d))) * sizeX;//精度と統一のためd
                        g.FillEllipse(Depth2Color(data.Depth, alpha), (float)(((data.Lon - config.LonSta) * zoomW) - size / 2f), (float)(((config.LatEnd - data.Lat) * zoomH) - size / 2f), size, size);
                        g.DrawEllipse(new Pen(Color.FromArgb(alpha, 127, 127, 127)), (float)(((data.Lon - config.LonSta) * zoomW) - size / 2f), (float)(((config.LatEnd - data.Lat) * zoomH) - size / 2f), size, size);
                        if (Math.Abs(data.MaxInt) >= config.TextInt)//↑imageとの違い
                        {
                            texts[0].AppendLine(data.Time.ToString("yyyy/MM/dd HH:mm:ss.f"));
                            texts[1].AppendLine(data.Hypo);//詳細不明の可能性
                            texts[2].Append(data.Depth == -1 ? "不明" : data.Depth.ToString());
                            texts[2].AppendLine(data.Depth == -1 ? "" : "km");
                            texts[3].Append(data.Mag == -1d ? "不明" : 'M');
                            texts[3].AppendLine(data.Mag == -1d ? "" : data.Mag.ToString("0.0"));
                            texts[4].AppendLine(MaxIntInt2String(data.MaxInt, true));
                        }
                    }
                    var depthSize = g.MeasureString(texts[2].ToString(), font_msD45);//string Formatに必要
                    var depthHeadSize = g.MeasureString("999km\n深さ", font_msD45);//999kmは最大幅計算用
                    var oneLineHeight = g.MeasureString("999km", font_msD45).Height;//調整用

                    g.FillRectangle(new SolidBrush(color.InfoBack), config.MapSize, 0, bitmap.Width - config.MapSize, config.MapSize);
                    g.DrawString("発生日時", font_msD45, sb_text_sub, config.MapSize, 0);
                    g.DrawString("震央", font_msD45, sb_text_sub, config.MapSize * 1.25f, 0);
                    g.DrawString("999km\n深さ", font_msD45, sb_text_sub, new RectangleF(new PointF(config.MapSize * 1.5f, -oneLineHeight), depthHeadSize), string_Right);
                    g.DrawString("規模", font_msD45, sb_text_sub, config.MapSize * 1.5875f, 0);
                    g.DrawString("最大震度", font_msD45, sb_text_sub, config.MapSize * 1.675f, 0); g.DrawString(texts[0].ToString(), font_msD45, sb_text, config.MapSize, 0);

                    g.DrawString(texts[0].ToString(), font_msD45, sb_text, config.MapSize, 0);
                    g.DrawString(texts[1].ToString(), font_msD45, sb_text, config.MapSize * 1.25f, 0);
                    g.DrawString(texts[2].ToString(), font_msD45, sb_text, new RectangleF(new PointF(config.MapSize * 1.5f, -oneLineHeight), depthSize), string_Right);
                    g.DrawString(texts[3].ToString(), font_msD45, sb_text, config.MapSize * 1.5875f, 0);
                    g.DrawString(texts[4].ToString(), font_msD45, sb_text, config.MapSize * 1.675f, 0);
                    g.DrawLine(pen_line, config.MapSize, config.MapSize / 30f, bitmap.Width, config.MapSize / 30f);

                    g.DrawImage(bitmap_legend, 0, 0);
                    var xBase = config.MapSize;
                    //imageとの違い
                    g.DrawString(drawTime.ToString("yyyy/MM/dd HH:mm:ss"), new Font(font, config.MapSize / 30f, GraphicsUnit.Pixel), new SolidBrush(color.Text), xBase + config.MapSize / 9f * 4, config.MapSize * 23 / 24f);


                    var savePath = $"{saveDir}\\{i:d5}.png";
                    bitmap.Save(savePath, ImageFormat.Png);
                    ConWrite($"{drawTime:yyyy/MM/dd HH:mm:ss}  {i:d5}.png : {datas_Draw.Count()}  (内部残り: {datas.Count()} / {dataSum})", ConsoleColor.Green);
                    drawTime += config.DrawSpan;
                    if (i % 10 == 0)
                        GC.Collect();
                }
                ConWrite($"画像出力完了\n動画化(30fps)(画像ファイルがあるフォルダで): ffmpeg -framerate 30 -i %05d.png -vcodec libx264 -pix_fmt yuv420p -r 30 _output.mp4");
                if (int.TryParse((string)UserInput("ffmpegで動画を作成する場合、fps(フレームレート)を入力してください。ffmpeg.exeのパスが通っている必要があります。\n数値への変換に失敗したら終了します。数字以外を何か入力してください。例:30", typeof(string), "30"), out int f))
                {
                    using var pro = Process.Start("ffmpeg", $"-framerate {f} -i \"{saveDir}\\%05d.png\" -vcodec libx264 -pix_fmt yuv420p -r {f} \"{saveDir}\\_output_{f}.mp4\"");
                    pro.WaitForExit();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                ConWrite("エラーが発生しました。" + ex + "\n再度実行してください。", ConsoleColor.Red);
#else
                ConWrite("エラーが発生しました。" + ex.Message + " 再度実行してください。", ConsoleColor.Red);
#endif                
            }
        }

        /// <summary>
        /// 凡例を描画します(同一描画防止)。
        /// </summary>
        /// <param name="config">設定</param>
        /// <returns>凡例(凡例外は透明)</returns>
        public static Bitmap DrawLegend(Config config)
        {
            var alpha = color.Hypo_Alpha;
            var sizeX = config.MagSizeType - (config.MagSizeType / 10 * 10);//倍率
            var xBase = config.MapSize;

            var font_msD45 = new Font(font, config.MapSize / 45f, GraphicsUnit.Pixel);
            var sb_text_sub = new SolidBrush(Color.FromArgb(127, color.Text));
            var pen_hypo = new Pen(Color.FromArgb(alpha, 127, 127, 127));
            var pen_line = new Pen(Color.FromArgb(127, color.Text), config.MapSize / 1080f);

            var bitmap_legend = new Bitmap(config.MapSize * 16 / 9, config.MapSize);
            using var g = Graphics.FromImage(bitmap_legend);

            if (!config.EnableLegend)
            {
                g.FillRectangle(new SolidBrush(color.InfoBack), config.MapSize, config.MapSize * 26f / 27f, config.MapSize / 9f * 7f + 1f, config.MapSize / 27f + 1f);//一応+1
                g.DrawString("地図データ:気象庁, Natural Earth", new Font(font, config.MapSize / 36f, GraphicsUnit.Pixel), sb_text_sub, xBase, config.MapSize * 26 / 27f);

                //g.FillRectangle(Brushes.Black, config.MapSize, config.MapSize * 26f / 27f, config.MapSize / 9f * 7f + 1f, config.MapSize / 27f + 1f);//一応+1
                //g.DrawString("2222/22/22 22:22:22", new Font(font, config.MapSize / 30f, GraphicsUnit.Pixel), new SolidBrush(color.Text), xBase + config.MapSize / 9f * 4, config.MapSize * 23 / 24f);

                return bitmap_legend;
            }

            //凡例
            switch (config.MagSizeType)
            {
                case 11:
                case 12:
                case 13:
                    // sizes:                    - (config.MapSize / 10f + magMaxSize)
                    //   [mag_txt]               -  config.MapSize / 48f
                    //   [mag_leg]               -  magMaxSize
                    //   [dep_leg]               -  config.MapSize / 48f
                    //   [map_source, datetime]  -  config.MapSize / 30f
                    var magMaxSize = 10 * config.MapSize / 216f * sizeX;//マグニチュード凡例円のサイズ(余白を含めたM10相当サイズ)
                    var yBase = config.MapSize - magMaxSize - config.MapSize / 10f;

                    g.FillRectangle(new SolidBrush(color.InfoBack), xBase, yBase, config.MapSize / 9f * 7f + 1f, magMaxSize + 20f * config.MapSize * sizeX + 1f);//一応+1
                    g.DrawLine(pen_line, xBase + config.MapSize / 80f, yBase + config.MapSize / 216f, config.MapSize * 1261f / 720f, yBase + config.MapSize / 216f);

                    for (int m = 1; m <= 8; m++)
                    {
                        var size = m * config.MapSize / 216f * sizeX;
                        var magLTx = config.MapSize + LEGEND_MAG_X_1X[m] * config.MapSize / 216f;//left top
                        var magLTy = config.MapSize - magMaxSize / 2f - size / 2f - config.MapSize / 14f;
                        // mag_text
                        var magSampleSize = g.MeasureString("M" + m + ".0", font_msD45);
                        g.DrawString("M" + m + ".0", font_msD45, sb_text_sub, magLTx + size / 2f - magSampleSize.Width / 2f, config.MapSize * 9f / 10f + config.MapSize / 540f - magMaxSize);
                        // mag_legend
                        g.FillEllipse(new SolidBrush(color.Legend_Mag_Fill), magLTx, magLTy, size, size);
                        g.DrawEllipse(pen_hypo, magLTx, magLTy, size, size);
                    }
                    break;
                case 21:
                case 22:
                    // sizes:                    - (config.MapSize / 3.8f + config.MapSize / 10f (= config.MapSize * 69 / 190f))
                    //   [mag_txt]               - (not fixed position)
                    //   [mag_leg]               - (not use value)
                    //   [dep_leg]               -  config.MapSize / 48f
                    //   [map_source, datetime]  -  config.MapSize / 30f
                    magMaxSize = config.MapSize / 3.8f;//マグニチュード凡例円部分のサイズ(適当)
                    yBase = config.MapSize - magMaxSize - config.MapSize / 10f;

                    g.FillRectangle(new SolidBrush(color.InfoBack), xBase, yBase, config.MapSize / 9 * 7 + 1, magMaxSize + 20 * config.MapSize * sizeX + 1);//一応+1
                    g.DrawLine(pen_line, xBase + config.MapSize / 80f, yBase + config.MapSize / 108f, config.MapSize * 1263 / 720f, yBase + config.MapSize / 108f);

                    for (int m = 1; m <= 7; m++)
                    {
                        var size = m * m * config.MapSize / 216f * sizeX;
                        var magLTx = config.MapSize + LEGEND_MAG_X_2X[m, 0] * config.MapSize / 216f;//left top
                        var magLTy = config.MapSize / 2f + LEGEND_MAG_X_2X[m, 1] * config.MapSize / 216f;
                        // mag_text
                        var magSampleSize = g.MeasureString("M" + m + ".0", font_msD45);
                        g.DrawString("M" + m + ".0", font_msD45, sb_text_sub, magLTx + size / 2f - magSampleSize.Width / 2f, magLTy - config.MapSize / 30f);
                        // mag_legend
                        g.FillEllipse(new SolidBrush(color.Legend_Mag_Fill), magLTx, magLTy, size, size);
                        g.DrawEllipse(pen_hypo, magLTx, magLTy, size, size);
                    }
                    break;
            }
            // dep_legend
            using (var textGP = new GraphicsPath())
                for (int di = 0; di < LEGEND_DEP_EX.Length; di++)
                {
                    //円部分
                    textGP.StartFigure();
                    textGP.AddString("●", font, 0, config.MapSize / 48f, new PointF(config.MapSize + config.MapSize * (di + 0.125f) / 10.8f, config.MapSize * 13f / 14f), StringFormat.GenericDefault);
                    g.FillPath(Depth2Color(LEGEND_DEP_EX[di], 255), textGP);
                    g.DrawPath(new Pen(Color.FromArgb(color.Hypo_Alpha, color.Text), config.MapSize / 1080f), textGP);
                    textGP.Reset();
                    //文字部分
                    g.DrawString("　" + (LEGEND_DEP_EX[di] == 0 ? " " : string.Empty) + LEGEND_DEP_EX[di] + "km", new Font(font, config.MapSize / 48f, GraphicsUnit.Pixel), sb_text_sub, config.MapSize + config.MapSize * (di + 0.125f) / 10.8f, config.MapSize * 13 / 14f);
                };

            g.DrawString("地図データ:気象庁, Natural Earth", new Font(font, config.MapSize / 36f, GraphicsUnit.Pixel), sb_text_sub, xBase, config.MapSize * 26 / 27f);
            //g.DrawString("2222/22/22 22:22:22", new Font(font, config.MapSize / 30f, GraphicsUnit.Pixel), new SolidBrush(color.Text), xBase + config.MapSize / 9f * 4, config.MapSize * 23 / 24f);
            return bitmap_legend;
        }

        /// <summary>
        /// ベースの地図を描画します
        /// </summary>
        /// <param name="config">設定</param>
        /// <returns>描画された地図</returns>
        /// <exception cref="Exception">マップデータの読み込みに失敗した場合</exception>
        public static Bitmap DrawMap(Config config)
        {
            color = JsonSerializer.Deserialize<Config_Color>(File.ReadAllText("colors.json"), jsonOption) ?? new Config_Color();
            var mapImg = new Bitmap(config.MapSize * 16 / 9, config.MapSize);
            var zoomW = config.MapSize / (config.LonEnd - config.LonSta);
            var zoomH = config.MapSize / (config.LatEnd - config.LatSta);
            var json = JsonNode.Parse(File.ReadAllText("map-world.geojson")) ?? throw new Exception("マップデータの読み込みに失敗しました。");
            var g = Graphics.FromImage(mapImg);
            g.Clear(color.Map.Sea);
            var maps = new GraphicsPath();
            maps.StartFigure();
            foreach (var json_1 in json["features"]!.AsArray())
            {
                if (json_1!["geometry"] == null)
                    continue;
                var points = json_1["geometry"]!["coordinates"]![0]!.AsArray().Select(json_2 => new Point((int)(((double)json_2![0]! - config.LonSta) * zoomW), (int)((config.LatEnd - (double)json_2[1]!) * zoomH))).ToArray();
                if (points.Length > 2)
                    maps.AddPolygon(points);
            }
            g.FillPath(new SolidBrush(color.Map.World), maps);

            json = JsonNode.Parse(File.ReadAllText("map-jp.geojson")) ?? throw new Exception("マップデータの読み込みに失敗しました。");
            maps.Reset();
            maps.StartFigure();
            foreach (var json_1 in json["features"]!.AsArray())
            {
                if ((string?)json_1!["geometry"]!["type"] == "Polygon")
                {
                    var points = json_1["geometry"]!["coordinates"]![0]!.AsArray().Select(json_2 => new Point((int)(((double)json_2![0]! - config.LonSta) * zoomW), (int)((config.LatEnd - (double)json_2[1]!) * zoomH))).ToArray();
                    if (points.Length > 2)
                        maps.AddPolygon(points);
                }
                else
                {
                    foreach (var json_2 in json_1["geometry"]!["coordinates"]!.AsArray())
                    {
                        var points = json_2![0]!.AsArray().Select(json_3 => new Point((int)(((double)json_3![0]! - config.LonSta) * zoomW), (int)((config.LatEnd - (double)json_3[1]!) * zoomH))).ToArray();
                        if (points.Length > 2)
                            maps.AddPolygon(points);
                    }
                }
            }
            g.FillPath(new SolidBrush(color.Map.Japan), maps);
            g.DrawPath(new Pen(color.Map.Japan_Border, config.MapSize / 1080f), maps);
            //var mdsize = g.MeasureString("地図データ:気象庁, Natural Earth", new Font(font, config.MapSize / 28, GraphicsUnit.Pixel));
            //g.DrawString("地図データ:気象庁, Natural Earth", new Font(font, config.MapSize / 28, GraphicsUnit.Pixel), new SolidBrush(color.Text), config.MapSize - mdsize.Width, config.MapSize - mdsize.Height);
            g.Dispose();
            return mapImg;
        }

        public static HttpClient client = new();

        /// <summary>
        /// 震度データベースcsvを取得します。
        /// </summary>
        public static void GetCsv()
        {
            try
            {
                ConWrite("一度に取得できる数は1000までとなるので注意してください。");
#if TEST
                var startTime = DateTime.Parse("2023/01/01 00:00");
                var endTime = DateTime.Parse("2023/01/01 23:59");
                var minMag = 0d;
                var maxMag = 9.9;
                var minDepth = 0;
                var maxDepth = 999;
                var minMaxInt = "1";
#else
                var startTime = (DateTime)UserInput("開始日時を入力してください。例:2023/01/01 00:00", typeof(DateTime));
                var endTime = (DateTime)UserInput("終了日時を入力してください。例:2023/01/31 23:59", typeof(DateTime));
                var minMag = (double)UserInput("最小マグニチュードを入力してください。例:0", typeof(double), "0");
                var maxMag = (double)UserInput("最大マグニチュードを入力してください。例:9.9", typeof(double), "9.9");
                var minDepth = (int)UserInput("最小深さを入力してください。例:0", typeof(int), "0");
                var maxDepth = (int)UserInput("最大深さを入力してください。例:999", typeof(int), "999");
                var minMaxInt = (string)UserInput("最大震度x以上 xを入力してください。ただし5弱:A,5強:B,6弱:C,6強:Dです。例:1", typeof(string), "1");
#endif
                var savePath = $"output\\csv\\{startTime:yyyyMMddHHmm}-{endTime:yyyyMMddHHmm}.csv";
                ConWrite("取得中...");
                var response = Regex.Unescape(client.GetStringAsync($"https://www.data.jma.go.jp/svd/eqdb/data/shindo/api/api.php?mode=search&dateTimeF[]={startTime:yyyy-MM-dd}&dateTimeF[]={startTime:HH:mm}&dateTimeT[]={endTime:yyyy-MM-dd}&dateTimeT[]={endTime:HH:mm}&mag[]={minMag:0.0}&mag[]={maxMag:0.0}&dep[]={minDepth:000}&dep[]={maxDepth:000}&epi[]=99&pref[]=99&city[]=99&station[]=99&obsInt=1&maxInt={minMaxInt}&additionalC=true&Sort=S0&Comp=C0&seisCount=false&observed=false").Result);
                if (string.IsNullOrEmpty(response))
                    throw new Exception("応答内容がありません。");
                var json = JsonNode.Parse(response!) ?? throw new Exception("応答内容がありません。");
                ConWrite($"データ個数 : {json["res"]!.AsArray()?.Count}", ConsoleColor.Green);

                var str = json["str"]!.AsArray();
                if (str != null)
                    foreach (var data in str)
                        ConWrite((string?)data, ConsoleColor.Green);
                ConWrite("変換中...");
                var csv = new StringBuilder("地震の発生日,地震の発生時刻,震央地名,緯度,経度,深さ,Ｍ,最大震度\n");
                var res = json["res"]!.AsArray();
                if (res != null)
                    foreach (var data in res)
                    {
                        var ot = (string?)data!["ot"];
                        if (ot == null)
                            continue;
                        csv.Append(ot.Replace(" ", ","));
                        csv.Append(',');
                        csv.Append((string?)data["name"]);
                        csv.Append(',');
                        csv.Append((string?)data["latS"]);
                        csv.Append(',');
                        csv.Append((string?)data["lonS"]);
                        csv.Append(',');
                        csv.Append((string?)data["dep"]);
                        csv.Append(',');
                        csv.Append((string?)data["mag"]);
                        csv.Append(',');
                        csv.Append((string?)data["maxI"]);
                        csv.AppendLine();
                    }
                Directory.CreateDirectory("output\\csv");
                File.WriteAllText(savePath, csv.ToString());
                ConWrite(savePath, ConsoleColor.Green);
                ConWrite("保存しました。");
            }
            catch (Exception ex)
            {
#if DEBUG
                ConWrite("エラーが発生しました。" + ex + "\n再度実行してください。", ConsoleColor.Red);
#else
                ConWrite("エラーが発生しました。" + ex.Message + " 再度実行してください。", ConsoleColor.Red);
#endif                
            }
        }

        public static readonly HtmlParser parser = new();

        /// <summary>
        /// 震源リスト(無感含む)を震度データベース互換に変換
        /// </summary>
        /// <remarks>震度は---になります</remarks>
        public static void GetHypo()
        {
            try
            {
                ConWrite("無感含む、2023年04月01日以降の震源リスト(https://www.data.jma.go.jp/eqev/data/daily_map/index.html)を震度データベース互換に変換します。保存後に複数ファイルの結合をすることで震度データベースのものと一緒に描画できます(同一地震判定はしません)。");
                var url = (string)UserInput("取得するURLを入力してください。形式:https://www.data.jma.go.jp/eqev/data/daily_map/yyyyMMdd.html", typeof(string));
                ConWrite("取得中...");
                var response = client.GetStringAsync(url).Result;
                ConWrite("解析中...");
                var document = parser.ParseDocument(response);
                var pre = document.QuerySelector("pre")!.TextContent;
                var lines = pre.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(2);
                var datas = lines.Select(HypoText2Data);
                ConWrite($"データ個数 : {datas.Count()}", ConsoleColor.Green);

                var csv = new StringBuilder("地震の発生日,地震の発生時刻,震央地名,緯度,経度,深さ,Ｍ,最大震度\n");
                var savePath = "output\\csv\\hypo\\" + url.Split('/').Last().Replace(".html", ".csv");

                foreach (var data in datas)
                {
                    //2024/01/03,20:07:02.4,能登半島沖,37°12.6′N,136°40.9′E,8 km,2.9,震度１
                    csv.Append(data.Time.ToString("yyyy/MM/dd"));
                    csv.Append(',');
                    csv.Append(data.Time.ToString("HH:mm:ss.f"));
                    csv.Append(',');
                    csv.Append(data.Hypo);
                    csv.Append(',');
                    csv.Append(LatLonDouble2String(data.Lat, true));
                    csv.Append(',');
                    csv.Append(LatLonDouble2String(data.Lon, false));
                    csv.Append(',');
                    csv.Append(data.Depth);
                    csv.Append(" km,");
                    csv.Append(data.Mag);
                    csv.Append(",---");
                    csv.AppendLine();
                }

                Directory.CreateDirectory("output");
                Directory.CreateDirectory("output\\csv");
                Directory.CreateDirectory("output\\csv\\hypo");
                File.WriteAllText(savePath, csv.ToString());
                ConWrite(savePath, ConsoleColor.Green);
                ConWrite("保存しました。");
            }
            catch (Exception ex)
            {
#if DEBUG
                ConWrite("エラーが発生しました。" + ex + "\n再度実行してください。", ConsoleColor.Red);
#else
                ConWrite("エラーが発生しました。" + ex.Message + " 再度実行してください。", ConsoleColor.Red);
#endif                
            }
        }

        /// <summary>
        /// ユーザーに値の入力を求めます。
        /// </summary>
        /// <remarks>入力値が変換可能なとき返ります。</remarks>
        /// <param name="message">表示するメッセージ</param>
        /// <param name="resType">変換するタイプ</param>
        /// <param name="nullText">何も入力されなかった場合に選択</param>
        /// <returns><paramref name="resType"/>で指定したタイプに変換された入力された値</returns>
        public static object UserInput(string message, Type resType, string? nullText = null)
        {
            while (true)
                try
                {
                    ConWrite(message);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    var input = Console.ReadLine();
                    if (string.IsNullOrEmpty(input))
                    {
                        if (string.IsNullOrEmpty(nullText))
                            throw new Exception("値を入力してください。");
                        input = nullText;
                        ConWrite(nullText + "(自動入力)", ConsoleColor.Cyan);
                    }
                    if (resType == typeof(bool))
                        return input == "y";
                    return resType.Name switch
                    {
                        "String" => input,
                        "Int32" => int.Parse(input),
                        "Double" => double.Parse(input),
                        "DateTime" => DateTime.Parse(input),
                        "TimeSpan" => TimeSpan.Parse(input),
                        _ => throw new Exception($"変換タイプが未定義です({resType})。"),
                    };
                }
                catch (Exception ex)
                {
                    ConWrite("入力の処理に失敗しました。" + ex.Message, ConsoleColor.Red);
                }
        }

        /// <summary>
        /// コンソールのデフォルトの色
        /// </summary>
        public static readonly ConsoleColor defaultColor = Console.ForegroundColor;

        /// <summary>
        /// コンソールにデフォルトの色で出力します。
        /// </summary>
        /// <param name="text">出力するテキスト</param>
        /// <param name="withLine">改行するか</param>
        public static void ConWrite(string? text, bool withLine = true)
        {
            ConWrite(text, defaultColor, withLine);
        }

        /// <summary>
        /// 例外のテキストを赤色で出力します。
        /// </summary>
        /// <param name="ex">出力する例外</param>
        public static void ConWrite(Exception ex)
        {
            ConWrite(ex.ToString(), ConsoleColor.Red);
        }

        /// <summary>
        /// コンソールに色付きで出力します。色は変わったままとなります。
        /// </summary>
        /// <param name="text">出力するテキスト</param>
        /// <param name="color">表示する色</param>
        /// <param name="withLine">改行するか</param>
        public static void ConWrite(string? text, ConsoleColor color, bool withLine = true)
        {
            Console.ForegroundColor = color;
            if (withLine)
                Console.WriteLine(text);
            else
                Console.Write(text);
        }
    }

    /// <summary>
    /// ColorをJSONシリアライズ/デシアライズできるようにします。
    /// </summary>
    public class ColorConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var colorString = reader.GetString() ?? throw new ArgumentException("値が正しくありません。");
            var argbValues = colorString.Replace(" ", "").Split(',');
            if (argbValues.Length == 3)
                return Color.FromArgb(int.Parse(argbValues[0]), int.Parse(argbValues[1]), int.Parse(argbValues[2]));
            else if (argbValues.Length == 4)
                return Color.FromArgb(int.Parse(argbValues[0]), int.Parse(argbValues[1]), int.Parse(argbValues[2]), int.Parse(argbValues[3]));
            else
                throw new ArgumentException("値が正しくありません。");
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.A}, {value.R}, {value.G}, {value.B}");
        }
    }
}
