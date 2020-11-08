#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace bindEDplugin
{
    public class bindEDPlugin
    {
        private static Dictionary<String, int>? _map = null;
        private static string? _pluginPath = null;
        private static readonly string _bindingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Frontier Developments\Elite Dangerous\Options\Bindings");
        private static string? _preset = null;
        private static dynamic? _VA = null;
        private static FileSystemWatcher? _watcher = null;
        private static Dictionary<string,int> _fileEventCount = new Dictionary<string, int>();

        public static string VERSION = "3.0";

        public static string VA_DisplayName() => $"bindED Plugin v{VERSION}-alterNERDtive";

        public static string VA_DisplayInfo() => "bindED Plugin\r\n\r\n2016 VoiceAttack.com\r\n2020 alterNERDtive";

        public static Guid VA_Id() => new Guid("{524B4B9A-3965-4045-A39A-A239BF6E2838}");

        public static void VA_Init1(dynamic vaProxy)
        {
            _VA = vaProxy;
            _VA.TextVariableChanged += new Action<string, string, string, Guid?>(TextVariableChanged);
            _pluginPath = Path.GetDirectoryName(_VA.PluginPath());

            try
            {
                LoadBinds("en-us");
            }
            catch (Exception e)
            {
                LogError(e.Message);
            }
        }

        public static void VA_Invoke1(dynamic vaProxy)
        {
            _VA = vaProxy;
            try
            {
                string context = _VA.Context.ToLower();
                string layout = _VA.GetText("bindED.layout#") ?? "en-us";
                if (context == "listbinds")
                {
                    ListBinds(layout, _VA.GetText("bindED.separator") ?? "\r\n");
                }
                else if (context == "loadbinds")
                {
                    LoadBinds(layout);
                }
                else
                {
                    LogWarn("Invoking the plugin with no context / a .binds file as context is deprecated and will be removed in a future version. Please invoke the 'loadbinds' context instead.");
                    LoadBinds(layout);
                }
            }
            catch (Exception e)
            {
                LogError(e.Message);
            }
        }

        public static void VA_StopCommand() { }

        public static void VA_Exit1(dynamic vaProxy) { }

        public static void TextVariableChanged(string name, string from, string to, Guid? internalID)
        {
            if (name.Equals("bindED.layout#"))
            {
                LogInfo($"Keyboard layout changed to '{to}', reloading …");
                LoadBinds(to);
            }
        }

        private static void LogError(string message)
        {
            _VA!.WriteToLog($"ERROR | bindED: {message}", "red");
        }

        private static void LogInfo(string message)
        {
            _VA!.WriteToLog($"INFO | bindED: {message}", "blue");
        }

        private static void LogWarn(string message)
        {
            _VA!.WriteToLog($"WARN | bindED: {message}", "yellow");
        }

        private static Dictionary<String, int> LoadKeyMap(string layout)
        {
            string mapFile = Path.Combine(_pluginPath, $"EDMap-{layout.ToLower()}.txt");
            if (!File.Exists(mapFile))
            {
                throw new FileNotFoundException($"No map file for layout '{layout}' found.");
            }
            Dictionary<string, int> map = new Dictionary<string, int>(256);
            foreach (String line in File.ReadAllLines(mapFile, System.Text.Encoding.UTF8))
            {
                String[] arItem = line.Split(";".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries);
                if ((arItem.Count() == 2) && (!String.IsNullOrWhiteSpace(arItem[0])) && (!map.ContainsKey(arItem[0])))
                {
                    ushort iKey;
                    if (ushort.TryParse(arItem[1], out iKey))
                    {
                        if (iKey > 0 && iKey < 256)
                            map.Add(arItem[0].Trim(), iKey);
                    }
                }
            }
            if (map.Count == 0)
            {
                throw new Exception($"Map file for {layout} does not contain any elements.");
            }
            return map;
        }

        private static string DetectPreset()
        {
            string startFile = Path.Combine(_bindingsDir, "StartPreset.start");
            if (!File.Exists(startFile))
            {
                throw new FileNotFoundException("No 'StartPreset.start' file found. Please run Elite: Dangerous at least once, then restart VoiceAttack.");
            }
            return File.ReadAllText(startFile);
        }

        private static string DetectBindsFile(string preset)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(_bindingsDir);
            FileInfo[] bindFiles = dirInfo.GetFiles().Where(i => Regex.Match(i.Name, $@"^{preset}(\.3\.0)?\.binds$").Success).OrderByDescending(p => p.LastWriteTime).ToArray();

            if (bindFiles.Count() == 0)
            {
                throw new FileNotFoundException($"No bindings file found for preset '{preset}'. If this is a default preset, please change anything in Elite’s controls options.");
            }

            return bindFiles[0].FullName;
        }

        private static Dictionary<string, string> ReadBinds(string file)
        {
            XElement rootElement;

            rootElement = XElement.Load(file);

            Dictionary<string, string> binds = new Dictionary<string, string>(512);
            if (rootElement != null)
            {
                foreach (XElement c in rootElement.Elements().Where(i => i.Elements().Count() > 0))
                {
                    foreach (var element in c.Elements().Where(i => i.HasAttributes))
                    {
                        List<int> keys = new List<int>();
                        if (element.Name == "Primary")
                        {
                            if (element.Attribute("Device").Value == "Keyboard" && !String.IsNullOrWhiteSpace(element.Attribute("Key").Value) && element.Attribute("Key").Value.StartsWith("Key_"))
                            {
                                foreach (var modifier in element.Elements().Where(i => i.Name.LocalName == "Modifier"))
                                {
                                    if (_map!.ContainsKey(modifier.Attribute("Key").Value))
                                        keys.Add(_map[modifier.Attribute("Key").Value]);
                                }

                                if (_map!.ContainsKey(element.Attribute("Key").Value))
                                    keys.Add(_map[element.Attribute("Key").Value]);
                            }
                        }
                        if (keys.Count == 0) //nothing found in primary... look in secondary
                        {
                            if (element.Name == "Secondary")
                            {
                                if (element.Attribute("Device").Value == "Keyboard" && !String.IsNullOrWhiteSpace(element.Attribute("Key").Value) && element.Attribute("Key").Value.StartsWith("Key_"))
                                {
                                    foreach (var modifier in element.Elements().Where(i => i.Name.LocalName == "Modifier"))
                                    {
                                        if (_map!.ContainsKey(modifier.Attribute("Key").Value))
                                            keys.Add(_map[modifier.Attribute("Key").Value]);
                                    }

                                    if (_map!.ContainsKey(element.Attribute("Key").Value))
                                        keys.Add(_map[element.Attribute("Key").Value]);
                                }
                            }
                        }

                        if (keys.Count > 0)
                        {
                            String strTextValue = String.Empty;
                            foreach (int key in keys)
                                strTextValue += String.Format("[{0}]", key);

                            binds.Add($"ed{c.Name.LocalName}", strTextValue);
                        }
                    }
                }
            }
            return binds;
        }

        private static void SetVariables(Dictionary<string, string> binds)
        {
            foreach (KeyValuePair<string, string> bind in binds)
            {
                _VA!.SetText(bind.Key, bind.Value);
            }
        }

        private static string GetBindsList(Dictionary<string, string> binds, string separator)
        {
            return string.Join(separator, binds.Keys);
        }

        public static void ListBinds(string layout, string separator)
        {
            _map = LoadKeyMap(layout);
            _preset = DetectPreset();
            _VA!.SetText("~bindED.bindsList", GetBindsList(ReadBinds(DetectBindsFile(_preset)), separator));
        }

        public static void LoadBinds(string layout)
        {
            try
            {
                _map = LoadKeyMap(layout);
                _preset = DetectPreset();
                SetVariables(ReadBinds(DetectBindsFile(_preset)));
            }
            finally
            {
                if (_watcher == null)
                {
                    FileSystemWatcher watcher = new FileSystemWatcher(_bindingsDir);
                    watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                    watcher.Changed += (source, EventArgs) => { FileChangedHandler(EventArgs.Name); };
                    watcher.Created += (source, EventArgs) => { FileChangedHandler(EventArgs.Name); };
                    watcher.Renamed += (source, EventArgs) => { FileChangedHandler(EventArgs.Name); };
                    watcher.EnableRaisingEvents = true;
                }
            }
        }

        private static void FileChangedHandler(string name)
        {
            // so apparently these events all fire twice … let’s make sure we only handle it once.
            if (_fileEventCount.ContainsKey(name))
            {
                _fileEventCount[name] += 1;
            }
            else
            {
                _fileEventCount.Add(name, 1);
            }
            if (_fileEventCount[name] % 2 == 0)
            {
                try
                {
                    // let’s make semi-sure that the file isn’t locked …
                    // FIXXME: solve this properly
                    Thread.Sleep(500);
                    if (name == "StartPreset.start")
                    {
                        LogInfo("Controls preset changed, reloading …");
                        _preset = DetectPreset();
                        SetVariables(ReadBinds(DetectBindsFile(_preset)));
                    }
                    else if (Regex.Match(name, $@"{_preset}(\.3\.0)?\.binds$").Success)
                    {
                        LogInfo($"Bindings file '{name}' has changed, reloading …");
                        SetVariables(ReadBinds(DetectBindsFile(_preset!)));
                    }
                }
                catch (Exception e)
                {
                    LogError(e.Message);
                }
            }
        }
    }
}
