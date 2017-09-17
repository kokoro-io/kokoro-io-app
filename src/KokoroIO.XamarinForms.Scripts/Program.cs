﻿using System;
using System.IO;
using System.Linq;
using System.Text;
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

            Generate(inDir, outDir);
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
    }
}