﻿using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.Versioning;
using System.Text;
using static JeqDB_Converter.Conv;

namespace JeqDB_Converter
{
    internal class Program
    {

        /// <summary>
        /// 文字描画用フォント
        /// </summary>
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
        public static FontFamily font;
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。

        [SupportedOSPlatform("windows")]//CA1416回避
        static void Main()
        {
            PrivateFontCollection pfc = new();
            pfc.AddFontFile("Koruri-Regular.ttf");
            font = pfc.Families[0];
            Directory.CreateDirectory("output");
            ConWrite("モードを入力してください。");
            ConWrite("> 1.複数ファイルの結合");
            ConWrite("> 2.画像描画");
            ConWrite("> 3.動画作成");
            int mode;
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                string? select = Console.ReadLine();
                if (int.TryParse(select, null, out int selectNum))
                    if (0 < selectNum && selectNum < 4)
                    {
                        mode = selectNum;
                        break;
                    }
                ConWrite("値は1から3の間である必要があります。", ConsoleColor.Red);
            }
            switch (mode)
            {
                case 1:
                    MergeFiles();
                    break;
                case 2:
                    DrawImage();
                    break;
                case 3:
                    ReadyVideo();
                    break;
            }
            Main();
        }

        /// <summary>
        /// ファイルを結合します。
        /// </summary>
        public static void MergeFiles()
        {
            ConWrite("結合するファイルのパスを1行ごとに入力してください。空文字が入力されたら結合を開始します。");
            List<string> files = [];
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                string? file = Console.ReadLine();
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

            StringBuilder stringBuilder = new();
            foreach (string file in files)
            {
                if (File.Exists(file))
                {
                    ConWrite("読み込み中... ", false);
                    ConWrite(file, ConsoleColor.Green);
                    stringBuilder.Append(File.ReadAllText(file).Replace("地震の発生日,地震の発生時刻,震央地名,緯度,経度,深さ,Ｍ,最大震度\n", ""));
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
                    ConWrite("エラーが発生しました。" + ex.Message + "再度入力してください。", ConsoleColor.Red);
                }
            }
            ConWrite("保存しました。");
        }

        /// <summary>
        /// 画像を描画します。
        /// </summary>
        [SupportedOSPlatform("windows")]//CA1416回避
        public static void DrawImage()
        {
#if !DEBUG
            ConWrite("読み込むcsvファイルのパスを入力してください。");
#endif
            try
            {
#if DEBUG
                string[] datas_ = File.ReadAllLines("C:\\Users\\proje\\Downloads\\地震リスト (x).csv");
#else
                string path = Console.ReadLine() ?? "";
                Console.ForegroundColor = ConsoleColor.Cyan;
                string[] datas_ = File.ReadAllLines(path.Replace("\"", ""));
#endif
                ConWrite("変換中...");
                IEnumerable<Data> datas = datas_.Where(x => x.Contains('°')).Select(Text2Data).OrderBy(a => a.Time);//データじゃないやつついでに緯度経度ないやつも除外
#if DEBUG
                Config config = new()
                {
                    SavePath = $"output\\{DateTime.Now:yyyyMMddHHmmss}.png",
                    MapSize = 2160,
                    LatSta = 20,
                    LatEnd = 50,
                    LonSta = 120,
                    LonEnd = 150,
                    TextInt = 3
                };
#else
                Config config = new()
                {
                    SavePath = $"output\\{DateTime.Now:yyyyMMddHHmmss}.png",
                    MapSize = (int)UserInput("画像の高さを入力してください。幅は16:9になるように計算されます。例:720/1080/2160/4320", typeof(int)),
                    LatSta = (double)UserInput("緯度の始点(地図の下端)を入力してください。例:20", typeof(double)),
                    LatEnd = (double)UserInput("緯度の終点(地図の上端)を入力してください。例:50", typeof(double)),
                    LonSta = (double)UserInput("経度の始点(地図の左端)を入力してください。例:120", typeof(double)),
                    LonEnd = (double)UserInput("経度の終点(地図の右端)を入力してください。例:150", typeof(double)),
                    TextInt = (int)UserInput("右欄に表示する最小震度を入力してください。震度1:1 震度5弱:5 震度6強:8のようにしてください。", typeof(int))
                };
#endif
                ConWrite("描画中...");
                double ZoomW = config.MapSize / (config.LonEnd - config.LonSta);
                double ZoomH = config.MapSize / (config.LatEnd - config.LatSta);
                Bitmap bitmap = DrawMap(config);
                Graphics g = Graphics.FromImage(bitmap);

                StringBuilder[] text = [new StringBuilder("発生時刻\n"), new StringBuilder("震源\n"), new StringBuilder("深さ\n"), new StringBuilder("規模\n"), new StringBuilder("最大震度\n")];
                foreach (Data data in datas)
                {
                    int size = (int)(Math.Max(1, data.Mag) * config.MapSize / 216);
                    int alpha = 204;
                    g.FillEllipse(Depth2Color(data.Depth, alpha), (int)((data.Lon - config.LonSta) * ZoomW) - size / 2, (int)((config.LatEnd - data.Lat) * ZoomH) - size / 2, size, size);
                    g.DrawEllipse(new Pen(Color.FromArgb(alpha, 127, 127, 127)), (int)((data.Lon - config.LonSta) * ZoomW) - size / 2, (int)((config.LatEnd - data.Lat) * ZoomH) - size / 2, size, size);
                    if (data.MaxInt >= config.TextInt)
                    {
                        text[0].AppendLine(data.Time.ToString());
                        text[1].AppendLine(data.Hypo);
                        text[2].Append(data.Depth.ToString().Replace("-1", "不明"));
                        text[2].AppendLine("km");
                        text[3].Append('M');
                        text[3].AppendLine(data.Mag.ToString("0.0").Replace("-1.0", "不明"));
                        text[4].AppendLine(MaxIntInt2String(data.MaxInt));
                    }
                }
                g.FillRectangle(new SolidBrush(Color.FromArgb(30, 60, 90)), config.MapSize, 0, bitmap.Width - config.MapSize, config.MapSize);
                g.DrawString(text[0].ToString(), new Font(font, config.MapSize / 45, GraphicsUnit.Pixel), Brushes.White, config.MapSize, 0);
                g.DrawString(text[1].ToString(), new Font(font, config.MapSize / 45, GraphicsUnit.Pixel), Brushes.White, (int)(config.MapSize * 1.225), 0);
                g.DrawString(text[2].ToString(), new Font(font, config.MapSize / 45, GraphicsUnit.Pixel), Brushes.White, (int)(config.MapSize * 1.475), 0);
                g.DrawString(text[3].ToString(), new Font(font, config.MapSize / 45, GraphicsUnit.Pixel), Brushes.White, (int)(config.MapSize * 1.575), 0);
                g.DrawString(text[4].ToString(), new Font(font, config.MapSize / 45, GraphicsUnit.Pixel), Brushes.White, (int)(config.MapSize * 1.675), 0);
                g.DrawLine(new Pen(Color.White, config.MapSize / 1024), config.MapSize, config.MapSize * 36 / 1024, bitmap.Width, config.MapSize * 36 / 1024);
                bitmap.Save(config.SavePath, ImageFormat.Png);
                ConWrite($"{config.SavePath} : {datas.Count()}", ConsoleColor.Green);
                g.Dispose();
                bitmap.Dispose();
            }
            catch (Exception ex)
            {
                ConWrite("エラーが発生しました。" + ex.Message + "再度実行してください。", ConsoleColor.Red);
            }
        }


        [SupportedOSPlatform("windows")]//CA1416回避
        public static void ReadyVideo()
        {
#if !DEBUG
            ConWrite("読み込むcsvファイルのパスを入力してください。");
#endif
            try
            {
#if DEBUG
                string[] datas_ = File.ReadAllLines("C:\\Users\\proje\\Downloads\\地震リスト (x).csv");
#else
                string path = Console.ReadLine() ?? "";
                Console.ForegroundColor = ConsoleColor.Cyan;
                string[] datas_ = File.ReadAllLines(path.Replace("\"", ""));
#endif
                ConWrite("変換中...");
                List<Data> datas = [.. datas_.Where(x => x.Contains('°')).Select(Text2Data).OrderBy(a => a.Time)];//データじゃないやつついでに緯度経度ないやつも除外
#if DEBUG
                Config config = new()
                {
                    SavePath = $"output\\{DateTime.Now:yyyyMMddHHmmss}",
                    StartTime = new DateTime(2023, 1, 1),
                    EndTime = new DateTime(2024, 1, 1),
                    DrawSpan = new TimeSpan(6, 0, 0),
                    DisappTime = new TimeSpan(96, 0, 0),
                    MapSize = 1080,
                    LatSta = 20,
                    LatEnd = 50,
                    LonSta = 120,
                    LonEnd = 150,
                    TextInt = 3
                };
#else
                Config config = new()
                {
                    SavePath = $"output\\{(string)UserInput("画像を保存するフォルダのパス(output\\)を入力してください。", typeof(string))}",
                    StartTime = (DateTime)UserInput("開始時刻を入力してください。例(2023年1月1日):2023/01/01 00:00:00", typeof(DateTime)),
                    EndTime = (DateTime)UserInput("終了時刻を入力してください。この時間未満まで描画されます。例(2024年1月1日):2024/01/01 00:00:00", typeof(DateTime)),
                    DrawSpan = (TimeSpan)UserInput("描画間隔を入力してください。この時間毎に描画時刻からこの時間までのものが描画されます。例(6時間):06:00:00", typeof(TimeSpan)),
                    DisappTime = (TimeSpan)UserInput("消失時間を入力してください。発生時刻からこの時間過ぎたら完全に消えます。例(1日):1.00:00:00", typeof(TimeSpan)),
                    MapSize = (int)UserInput("画像の高さを入力してください。幅は16:9になるように計算されます。例:720/1080/2160/4320", typeof(int)),
                    LatSta = (double)UserInput("緯度の始点(地図の下端)を入力してください。例:20", typeof(double)),
                    LatEnd = (double)UserInput("緯度の終点(地図の上端)を入力してください。例:50", typeof(double)),
                    LonSta = (double)UserInput("経度の始点(地図の左端)を入力してください。例:120", typeof(double)),
                    LonEnd = (double)UserInput("経度の終点(地図の右端)を入力してください。例:150", typeof(double)),
                    TextInt = (int)UserInput("右欄に表示する最小震度を入力してください。震度1:1 震度5弱:5 震度6強:8のようにしてください。", typeof(int))
                };
#endif
                ConWrite("描画中...");
                Directory.CreateDirectory(config.SavePath);

                double ZoomW = config.MapSize / (config.LonEnd - config.LonSta);
                double ZoomH = config.MapSize / (config.LatEnd - config.LatSta);
                Bitmap baseMap = DrawMap(config);

                DateTime DrawTime = config.StartTime;//描画対象時間
                for (int i = 1; DrawTime < config.EndTime; i++)//DateTime:古<新==true
                {
                    List<Data> datas_Draw = [];
                    List<Data> datas_Copy = datas.Select(item => new Data
                    {
                        Time = item.Time,
                        Hypo = item.Hypo,
                        Lat = item.Lat,
                        Lon = item.Lon,
                        Depth = item.Depth,
                        Mag = item.Mag,
                        MaxInt = item.MaxInt
                    }).ToList();

                    foreach (Data data in datas_Copy)
                    {
                        if (data.Time < DrawTime - config.DisappTime)//描画終了
                            datas.RemoveAt(0);
                        else if (data.Time < DrawTime + config.DrawSpan)//描画
                            datas_Draw.Add(data);
                        else//未描画
                            break;
                    }
                    datas_Copy.Clear();

                    Bitmap bitmap = (Bitmap)baseMap.Clone();
                    Graphics g = Graphics.FromImage(bitmap);
                    StringBuilder[] text = [new StringBuilder("発生時刻\n"), new StringBuilder("震源\n"), new StringBuilder("深さ\n"), new StringBuilder("規模\n"), new StringBuilder("最大震度\n")];
                    foreach (Data data in datas_Draw)
                    {
                        int size = (int)(Math.Max(1, data.Mag) * config.MapSize / 216);
                        int alpha = 204;
                        if (data.Time < DrawTime)//描画時間より前
                            alpha = (int)((1d - (DrawTime - data.Time).TotalSeconds / config.DisappTime.TotalSeconds) * alpha);//消える時間の割合*基本透明度
                        g.FillEllipse(Depth2Color(data.Depth, alpha), (int)((data.Lon - config.LonSta) * ZoomW) - size / 2, (int)((config.LatEnd - data.Lat) * ZoomH) - size / 2, size, size);
                        g.DrawEllipse(new Pen(Color.FromArgb(alpha, 127, 127, 127)), (int)((data.Lon - config.LonSta) * ZoomW) - size / 2, (int)((config.LatEnd - data.Lat) * ZoomH) - size / 2, size, size);
                        if (data.MaxInt >= config.TextInt)
                        {
                            text[0].AppendLine(data.Time.ToString());
                            text[1].AppendLine(data.Hypo);
                            text[2].Append(data.Depth.ToString().Replace("-1", "不明"));
                            text[2].AppendLine("km");
                            text[3].Append('M');
                            text[3].AppendLine(data.Mag.ToString("0.0").Replace("-1.0", "不明"));
                            text[4].AppendLine(MaxIntInt2String(data.MaxInt));
                        }
                    }
                    g.FillRectangle(new SolidBrush(Color.FromArgb(30, 60, 90)), config.MapSize, 0, bitmap.Width - config.MapSize, config.MapSize);
                    g.DrawString(DrawTime.ToString("yyyy/MM/dd HH:mm:ss"), new Font(font, config.MapSize / 24, GraphicsUnit.Pixel), Brushes.White, 0, 0);
                    g.DrawString(text[0].ToString(), new Font(font, config.MapSize / 45, GraphicsUnit.Pixel), Brushes.White, config.MapSize, 0);
                    g.DrawString(text[1].ToString(), new Font(font, config.MapSize / 45, GraphicsUnit.Pixel), Brushes.White, (int)(config.MapSize * 1.225), 0);
                    g.DrawString(text[2].ToString(), new Font(font, config.MapSize / 45, GraphicsUnit.Pixel), Brushes.White, (int)(config.MapSize * 1.475), 0);
                    g.DrawString(text[3].ToString(), new Font(font, config.MapSize / 45, GraphicsUnit.Pixel), Brushes.White, (int)(config.MapSize * 1.575), 0);
                    g.DrawString(text[4].ToString(), new Font(font, config.MapSize / 45, GraphicsUnit.Pixel), Brushes.White, (int)(config.MapSize * 1.675), 0);
                    g.DrawLine(new Pen(Color.White, config.MapSize / 1024), config.MapSize, config.MapSize * 36 / 1024, bitmap.Width, config.MapSize * 36 / 1024);
                    string savePath = $"{config.SavePath}\\{i:d4}.png";
                    bitmap.Save(savePath, ImageFormat.Png);
                    g.Dispose();
                    bitmap.Dispose();
                    ConWrite($"{DrawTime:yyyy/MM/dd HH:mm:ss} {i:d4}.png : {datas_Draw.Count}", ConsoleColor.Green);
                    DrawTime += config.DrawSpan;
                }
                ConWrite($"画像出力完了\n動画化(30fps)(画像ファイルがあるフォルダで): ffmpeg -framerate 30 -i %04d.png -vcodec libx264 -pix_fmt yuv420p -r 30 _output.mp4");
            }
            catch (Exception ex)
            {
                ConWrite("エラーが発生しました。" + ex.Message + "再度実行してください。", ConsoleColor.Red);
            }
        }

        [SupportedOSPlatform("windows")]//CA1416回避
        public static Bitmap DrawMap(Config config)
        {
#pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
#pragma warning disable CS8604 // Null 参照引数の可能性があります。
            Bitmap mapImg = new(config.MapSize * 16 / 9, config.MapSize);
            double ZoomW = config.MapSize / (config.LonEnd - config.LonSta);
            double ZoomH = config.MapSize / (config.LatEnd - config.LatSta);
            JObject json = JObject.Parse(File.ReadAllText("map-world.geojson"));
            Graphics g = Graphics.FromImage(mapImg);
            g.Clear(Color.FromArgb(30, 30, 60));
            GraphicsPath Maps = new();
            Maps.StartFigure();
            foreach (JToken json_1 in json.SelectToken("features"))
            {
                if (!json_1.SelectToken("geometry").Any())
                    continue;
                List<Point> points = [];
                foreach (JToken json_2 in json_1.SelectToken("geometry.coordinates[0]"))
                    points.Add(new Point((int)(((double)json_2.SelectToken("[0]") - config.LonSta) * ZoomW), (int)((config.LatEnd - (double)json_2.SelectToken("[1]")) * ZoomH)));
                if (points.Count > 2)
                    Maps.AddPolygon(points.ToArray());
            }
            g.FillPath(new SolidBrush(Color.FromArgb(100, 100, 150)), Maps);

            json = JObject.Parse(File.ReadAllText("map-jp.geojson"));
            Maps.Reset();
            Maps.StartFigure();
            foreach (JToken json_1 in json.SelectToken("features"))
            {
                if ((string?)json_1.SelectToken("geometry.type") == "Polygon")
                {
                    List<Point> points = [];
                    foreach (JToken json_2 in json_1.SelectToken("geometry.coordinates[0]"))
                        points.Add(new Point((int)(((double)json_2.SelectToken("[0]") - config.LonSta) * ZoomW), (int)((config.LatEnd - (double)json_2.SelectToken("[1]")) * ZoomH)));
                    if (points.Count > 2)
                        Maps.AddPolygon(points.ToArray());
                }
                else
                {
                    foreach (JToken json_2 in json_1.SelectToken("geometry.coordinates"))
                    {
                        List<Point> points = [];
                        foreach (JToken json_3 in json_2.SelectToken("[0]"))
                            points.Add(new Point((int)(((double)json_3.SelectToken("[0]") - config.LonSta) * ZoomW), (int)((config.LatEnd - (double)json_3.SelectToken("[1]")) * ZoomH)));
                        if (points.Count > 2)
                            Maps.AddPolygon(points.ToArray());
                    }
                }
            }
            g.FillPath(new SolidBrush(Color.FromArgb(90, 90, 120)), Maps);
            g.DrawPath(new Pen(Color.FromArgb(127, 255, 255, 255), (int)(config.MapSize / 1080d)), Maps);
            SizeF mdsize = g.MeasureString("地図データ:気象庁, Natural Earth", new Font(font, config.MapSize / 28, GraphicsUnit.Pixel));
            g.DrawString("地図データ:気象庁, Natural Earth", new Font(font, config.MapSize / 28, GraphicsUnit.Pixel), Brushes.White, config.MapSize - mdsize.Width, config.MapSize - mdsize.Height);
            g.Dispose();
            return mapImg;
#pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。
#pragma warning restore CS8604 // Null 参照引数の可能性があります。
        }

        /// <summary>
        /// ユーザーに値の入力を求めます。
        /// </summary>
        /// <remarks>入力値が変換可能なとき返ります。</remarks>
        /// <param name="message">表示するメッセージ</param>
        /// <param name="resType">変換するタイプ</param>
        /// <returns><paramref name="resType"/>で指定したタイプに変換された入力された値</returns>
        public static object UserInput(string message, Type resType)
        {
            while (true)
                try
                {
                    ConWrite(message);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    string? input = Console.ReadLine();
                    if (string.IsNullOrEmpty(input))
                        throw new Exception("値を入力してください。");
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
        public static void ConWrite(string text, bool withLine = true)
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
        public static void ConWrite(string text, ConsoleColor color, bool withLine = true)
        {
            Console.ForegroundColor = color;
            if (withLine)
                Console.WriteLine(text);
            else
                Console.Write(text);
        }
    }
}