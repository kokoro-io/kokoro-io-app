using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace KokoroIO.XamarinForms.Scripts
{
    internal class Program
    {
        private static void Main()
        {
            Console.WriteLine("Output Directory: {0}", typeof(Program).Assembly.Location);
            var htmlPath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(typeof(Program).Assembly.Location)), "Messages.template.html");

            var inDir = Path.GetDirectoryName(htmlPath);
            var outDir = new Uri(new Uri(htmlPath), "../KokoroIO.XamarinForms/Resources").LocalPath;

            Console.WriteLine("Input Directory: {0}", inDir);
            Console.WriteLine("Output Directory: {0}", outDir);

            //OutputColorVariation(inDir, "ic_warning_black_24px.svg", 0xF39C12);

            Generate(inDir, outDir);

            //GenerateIcon(inDir, "kokoroio_icon_white.svg", "kokoro_white", baseSize: 48);

            //GenerateIcon(inDir, "ic_camera_alt_black_24px.svg", "camera");
            //GenerateIcon(inDir, "ic_image_black_24px.svg", "image");
            //GenerateIcon(inDir, "ic_send_black_24px.svg", "send");
            //GenerateIcon(inDir, "ic_visibility_black_24px.svg", "visibility");
            //GenerateIcon(inDir, "ic_visibility_off_black_24px.svg", "visibility_off");

            //GenerateIcon(inDir, "ic_account_circle_black_24px.svg", "account");
            //GenerateIcon(inDir, "ic_account_circle_black_24px.svg", "account_white", color: -1);
            //GenerateIcon(inDir, "ic_info_outline_black_24px.svg", "info");
            //GenerateIcon(inDir, "ic_info_outline_black_24px.svg", "info_white", color: -1);
            //GenerateIcon(inDir, "ic_notifications_black_24px.svg", "notifications");
            //GenerateIcon(inDir, "ic_notifications_black_24px.svg", "notifications_white", color: -1);
            //GenerateIcon(inDir, "ic_notifications_off_black_24px.svg", "notifications_off");
            //GenerateIcon(inDir, "ic_image_black_24px.svg", "image_white", color: -1);
            //GenerateIcon(inDir, "ic_search_black_24px.svg", "search");
            //GenerateIcon(inDir, "ic_settings_black_24px.svg", "settings");
            //GenerateIcon(inDir, "ic_email_black_24px.svg", "email");
        }

        private static string GetTempFileName(string extension)
        {
            var tmp = Path.GetTempFileName();
            File.Delete(tmp);
            return Path.ChangeExtension(tmp, extension);
        }

        private static void OutputColorVariationCore(string src, string dest, int color)
        {
            var xd = XDocument.Load(src);
            xd.Root.SetAttributeValue("fill", "#" + (color & 0xffffff).ToString("x6"));

            xd.Save(dest);
        }

        private static void OutputColorVariation(string inDir, string src, int color)
        {
            var s = Path.Combine(inDir, "icons", src);
            var d = Path.Combine(inDir, "icons", "variants", src.Replace("_black_24px.svg", $"_{color & 0xffffff:x6}_24px.svg"));

            OutputColorVariationCore(s, d, color);
        }

        private static void Generate(string inDir, string outDir)
        {
            foreach (var html in Directory.GetFiles(inDir, "*.template.html"))
            {
                var bu = new Uri(html);
                var xd = XDocument.Load(html);

                var head = xd.Elements("html").Elements("head").FirstOrDefault();

                if (head != null)
                {
                    {
                        var links = head.Elements("link").Where(l => l.Attribute("rel")?.Value == "stylesheet").ToArray();

                        if (links.Any())
                        {
                            var style = new XElement("style");
                            links[0].AddBeforeSelf(style);

                            foreach (var l in links)
                            {
                                var rules = File.ReadAllText(new Uri(bu, l.Attribute("href").Value).LocalPath);

                                rules = Regex.Replace(rules,
                                    @"url\(([^)]+)\.svg\)",
                                    svgm =>
                                    {
                                        var sfn = svgm.Value.Substring(4, svgm.Length - 5);
                                        var svgu = new Uri(bu, sfn).LocalPath;
                                        if (File.Exists(svgu))
                                        {
                                            var svg = File.ReadAllText(svgu);

                                            return $"url(data:image/svg+xml," + Uri.EscapeDataString(svg) + ")";
                                        }

                                        return svgm.Value;
                                    });

                                style.Add(new XText(rules));
                                l.Remove();
                            }
                        }
                    }
                    {
                        var scripts = head.Elements("script").Where(l => l.Attribute("src")?.Value != null).ToArray();

                        if (scripts.Any())
                        {
                            var script = new XElement("script");
                            scripts[0].AddBeforeSelf(script);

                            foreach (var l in scripts)
                            {
                                var rules = File.ReadAllText(new Uri(bu, l.Attribute("src").Value).LocalPath);
                                script.Add(new XText(rules));
                                l.Remove();
                            }
                        }
                    }
                }

                var fn = Path.GetFileNameWithoutExtension(html);
                var dest = Path.Combine(outDir, fn.Substring(0, fn.Length - "template".Length) + "html");

                using (var sw = new StreamWriter(dest, false, new UTF8Encoding(false)))
                using (var xw = XmlWriter.Create(sw, new XmlWriterSettings()
                {
                    OmitXmlDeclaration = true,
                    CheckCharacters = false,
                    Indent = true,
                    IndentChars = "    "
                }))
                using (var xr = xd.CreateReader())
                {
                    while (xr.Read())
                    {
                        if (xr.NodeType == XmlNodeType.DocumentType)
                        {
                            xw.WriteDocType("html", null, null, null);
                            xw.WriteComment($"\r\nGenerated by {typeof(Program).Namespace}.exe\r\nDO NOT EDIT THIS FILE DIRECTLY\r\n");
                        }
                        else if (xr.NodeType == XmlNodeType.Element)
                        {
                            var empty = xr.IsEmptyElement;
                            xw.WriteStartElement(xr.LocalName);

                            while (xr.MoveToNextAttribute())
                            {
                                xw.WriteAttributeString(xr.LocalName, xr.Value);
                            }

                            if (empty)
                            {
                                xw.WriteEndElement();
                            }
                        }
                        else if (xr.NodeType == XmlNodeType.Text)
                        {
                            xw.WriteRaw(xr.Value);
                        }
                        else if (xr.NodeType == XmlNodeType.EndElement)
                        {
                            xw.WriteEndElement();
                        }
                        else
                        {
                            xw.WriteNode(xr, false);
                        }
                    }
                }
            }
        }

        private static void GenerateIcon(string inDir, string src, string dest, int color = 0, int baseSize = 24)
        {
            var exed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Inkscape");

            if (!Directory.Exists(exed))
            {
                exed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Inkscape");
            }

            var exe = Path.Combine(exed, "inkscape.exe");

            var sf = Path.Combine(inDir, "icons", src);

            if (color != 0)
            {
                var temp = GetTempFileName(".svg");
                OutputColorVariationCore(sf, temp, color);
                sf = temp;
            }

            {
                var dirs = new[] { "m", "h", "xh", "xxh", "xxxh" };
                var sizes = new[] { baseSize, baseSize * 1.5, baseSize * 2, baseSize * 3, baseSize * 4 };

                // var procs = new List<Process>();

                for (var i = 0; i < dirs.Length; i++)
                {
                    var size = sizes[i];

                    var psi = new ProcessStartInfo(exe);
                    psi.CreateNoWindow = true;
                    var dp = new Uri(new Uri(inDir), $"./KokoroIO.XamarinForms.Android/Resources/drawable-{dirs[i]}dpi/{dest}.png");
                    psi.Arguments = $"-z \"{sf}\" -w {size:0} -h {size:0} -e {dp.LocalPath}";
                    psi.UseShellExecute = false;
                    psi.RedirectStandardInput = true;
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;

                    var p = Process.Start(psi);
                    p.WaitForExit();

                    //procs.Add(p);
                }

                //foreach (var p in procs)
                //{
                //    p.WaitForExit();
                //}
            }
            {
                File.Copy(new Uri(new Uri(inDir), $"./KokoroIO.XamarinForms.Android/Resources/drawable-mdpi/{dest}.png").LocalPath
                        , new Uri(new Uri(inDir), $"./KokoroIO.XamarinForms.iOS/Resources/{dest}.png").LocalPath, true);
                File.Copy(new Uri(new Uri(inDir), $"./KokoroIO.XamarinForms.Android/Resources/drawable-xhdpi/{dest}.png").LocalPath
                        , new Uri(new Uri(inDir), $"./KokoroIO.XamarinForms.iOS/Resources/{dest}@2x.png").LocalPath, true);
                File.Copy(new Uri(new Uri(inDir), $"./KokoroIO.XamarinForms.Android/Resources/drawable-xxhdpi/{dest}.png").LocalPath
                        , new Uri(new Uri(inDir), $"./KokoroIO.XamarinForms.iOS/Resources/{dest}@3x.png").LocalPath, true);
            }
            {
                File.Copy(new Uri(new Uri(inDir), $"./KokoroIO.XamarinForms.iOS/Resources/{dest}.png").LocalPath
                        , new Uri(new Uri(inDir), $"./KokoroIO.XamarinForms.UWP/{dest}.png").LocalPath, true);
            }
        }
    }
}