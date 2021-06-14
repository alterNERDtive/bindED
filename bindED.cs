﻿#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace bindEDplugin
{
    public class bindEDPlugin
    {
        private static dynamic? _VA;
        private static string? _pluginPath;
        private static readonly string _bindingsDir = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
            @"Frontier Developments\Elite Dangerous\Options\Bindings"
            );
        private static readonly Dictionary<string, int> _fileEventCount = new Dictionary<string, int>();

        private static FileSystemWatcher BindsWatcher
        {
            get
            {
                if (_bindsWatcher == null)
                {
                    _bindsWatcher = new FileSystemWatcher(_bindingsDir);
                    _bindsWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                    _bindsWatcher.Changed += (source, EventArgs) => { FileChangedHandler(EventArgs.Name); };
                    _bindsWatcher.Created += (source, EventArgs) => { FileChangedHandler(EventArgs.Name); };
                    _bindsWatcher.Renamed += (source, EventArgs) => { FileChangedHandler(EventArgs.Name); };
                }
                return _bindsWatcher!;
            }
        }
        private static FileSystemWatcher? _bindsWatcher;

        private static FileSystemWatcher MapWatcher
        {
            get
            {
                if (_mapWatcher == null)
                {
                    _mapWatcher = new FileSystemWatcher(_pluginPath);
                    _mapWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                    _mapWatcher.Changed += (source, EventArgs) => { FileChangedHandler(EventArgs.Name); };
                    _mapWatcher.Created += (source, EventArgs) => { FileChangedHandler(EventArgs.Name); };
                    _mapWatcher.Renamed += (source, EventArgs) => { FileChangedHandler(EventArgs.Name); };
                }
                return _mapWatcher!;
            }
        }
        private static FileSystemWatcher? _mapWatcher;

        private static string? Layout
        {
            get => _layout ??= _VA?.GetText("bindED.layout#") ?? "en-us";
            set
            {
                _layout = value;
                KeyMap = null;
            }
        }
        private static string? _layout;

        private static Dictionary<string, int>? KeyMap
        {
            get => _keyMap ??= LoadKeyMap(Layout!);
            set => _keyMap = value;
        }
        private static Dictionary<string, int>? _keyMap;

        private static string? Preset
        {
            get => _preset ??= DetectPreset();
            set
            {
                _preset = value;
                Binds = null;
            }
        }
        private static string? _preset;

        private static Dictionary<string, List<string>>? Binds
        {
            get => _binds ??= ReadBinds(DetectBindsFile(Preset));
            set => _binds = value;
        }
        private static Dictionary<string, List<string>>? _binds;

        public static string VERSION = "4.1";

        public static string VA_DisplayName() => $"bindED Plugin v{VERSION}-alterNERDtive";

        public static string VA_DisplayInfo() => "bindED Plugin\r\n\r\n2016 VoiceAttack.com\r\n2020–2021 alterNERDtive";

        public static Guid VA_Id() => new Guid("{524B4B9A-3965-4045-A39A-A239BF6E2838}");

        public static void VA_Init1(dynamic vaProxy)
        {
            _VA = vaProxy;
            _VA.TextVariableChanged += new Action<string, string, string, Guid?>(TextVariableChanged);
            _pluginPath = Path.GetDirectoryName(_VA.PluginPath());

            _VA.SetText("bindED.version", VERSION);
            _VA.SetText("bindED.fork", "alterNERDtive");

            try
            {
                LoadBinds(Binds);
            }
            catch (Exception e)
            {
                LogError(e.Message);
            }
            finally
            {
                BindsWatcher.EnableRaisingEvents = true;
                MapWatcher.EnableRaisingEvents = true;
            }
        }

        public static void VA_Invoke1(dynamic vaProxy)
        {
            _VA = vaProxy;
            try
            {
                string context = _VA.Context.ToLower();
                if (context == "diagnostics")
                {
                    LogInfo($"current keybord layout: {Layout}");
                    LogInfo($"current preset: {Preset}");
                    LogInfo($"detected binds file: {(new FileInfo(DetectBindsFile(Preset))).Name}");
                    LogInfo($"detected binds file (full path): {DetectBindsFile(Preset)}");
                }
                else if (context == "listbinds")
                {
                    ListBinds(Binds, _VA.GetText("bindED.separator") ?? "\r\n");
                }
                else if (context == "loadbinds")
                {
                    // force reset everything
                    Layout = null;
                    Preset = null;
                    if (!String.IsNullOrWhiteSpace(_VA.GetText("~bindsFile")))
                    {
                        Binds = ReadBinds(Path.Combine(_bindingsDir, _VA.GetText("~bindsFile")));
                    }
                    LoadBinds(Binds);
                }
                else if (context == "missingbinds")
                {
                    MissingBinds(Binds);
                }
                else
                {
                    LogError($"Invalid plugin context {context}.");
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
            if (name == "bindED.layout#")
            {
                LogInfo($"Keyboard layout changed to '{to}', reloading …");
                Layout = to;
                try
                {
                    LoadBinds(Binds);
                }
                catch (Exception e)
                {
                    LogError(e.Message);
                }
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

        public static void ListBinds(Dictionary<string, List<string>>? binds, string separator)
        {
            _VA!.SetText("~bindED.bindsList", string.Join(separator, binds!.Keys));
            LogInfo("List of Elite binds saved to TXT variable '~bindED.bindsList'.");
        }

        private static void LoadBinds(Dictionary<string, List<string>>? binds)
        {
            foreach (KeyValuePair<string, List<string>> bind in binds!)
            {
                string value = string.Empty;
                bool valid = true;
                if (bind.Value.Count == 0)
                {
                    //LogInfo($"No keyboard bind for '{bind.Key}' found, skipping …");
                }
                else
                {
                    foreach (string key in bind.Value)
                    {
                        if (KeyMap!.ContainsKey(key))
                        {
                            value += $"[{KeyMap[key]}]";
                        }
                        else
                        {
                            valid = false;
                            LogError($"No valid key code for '{key}' found, skipping bind for '{bind.Key}' …");
                        }
                    }
                    if (valid)
                    {
                        _VA!.SetText(bind.Key, value);
                    }
                }
            }

            LogInfo($"Elite binds '{(String.IsNullOrWhiteSpace(_VA!.GetText("~bindsFile")) ?  Preset : _VA!.GetText("~bindsFile"))}' for layout '{Layout}' loaded successfully.");
        }

        private static void MissingBinds(Dictionary<string, List<string>>? binds)
        {
            List<string> missing = new List<string>(256);
            foreach (KeyValuePair<string, List<string>> bind in binds!)
            {

                if (bind.Value.Count == 0)
                {
                    missing.Add(bind.Key);
                }
            }
            if (missing.Count > 0)
            {
                _VA!.SetText("~bindED.missingBinds", string.Join("\r\n", missing));
                _VA!.SetBoolean("~bindED.missingBinds", true);
                LogInfo("List of missing Elite binds saved to TXT variable '~bindED.missingBinds'.");
            }
            else
            {
                LogInfo($"No missing keyboard binds found.");
            }
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
            return File.ReadAllLines(startFile).First();
        }

        private static string DetectBindsFile(string? preset)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(_bindingsDir);
            FileInfo[] bindFiles = dirInfo.GetFiles()
                .Where(i => Regex.Match(i.Name, $@"^{preset}\.[34]\.0\.binds$").Success)
                .OrderByDescending(p => p.Name).ToArray();

            if (bindFiles.Count() == 0)
            {
                bindFiles = dirInfo.GetFiles($"{preset}.binds");
                if (bindFiles.Count() == 0)
                {
                    throw new FileNotFoundException($"No bindings file found for preset '{preset}'. If this is a default preset, please change anything in Elite’s controls options.");
                }
            }

            return bindFiles[0].FullName;
        }

        private static Dictionary<string, List<string>> ReadBinds(string file)
        {
            XElement rootElement;

            rootElement = XElement.Load(file);

            Dictionary<string, List<string>> binds = new Dictionary<string, List<string>>(512);
            if (rootElement != null)
            {
                foreach (XElement c in rootElement.Elements().Where(i => i.Elements().Count() > 0))
                {
                    List<string> keys = new List<string>();
                    foreach (var element in c.Elements().Where(i => i.HasAttributes))
                    {
                        if (element.Name == "Primary")
                        {
                            if (element.Attribute("Device").Value == "Keyboard" && !String.IsNullOrWhiteSpace(element.Attribute("Key").Value) && element.Attribute("Key").Value.StartsWith("Key_"))
                            {
                                foreach (var modifier in element.Elements().Where(i => i.Name.LocalName == "Modifier"))
                                {
                                    keys.Add(modifier.Attribute("Key").Value);
                                }
                                keys.Add(element.Attribute("Key").Value);
                            }
                        }
                        if (keys.Count == 0 && element.Name == "Secondary") //nothing found in primary... look in secondary
                        {
                            if (element.Attribute("Device").Value == "Keyboard" && !String.IsNullOrWhiteSpace(element.Attribute("Key").Value) && element.Attribute("Key").Value.StartsWith("Key_"))
                            {
                                foreach (var modifier in element.Elements().Where(i => i.Name.LocalName == "Modifier"))
                                {
                                    keys.Add(modifier.Attribute("Key").Value);
                                }
                                keys.Add(element.Attribute("Key").Value);
                            }
                        }
                    }
                    binds.Add($"ed{c.Name.LocalName}", keys);
                }
            }
            return binds;
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
                    // Going by name only is a bit naïve given we’re watching 2
                    // separate directories, but hey … worst case if something
                    // is doing unintended things is unnecessarily reloading the
                    // binds.
                    if (name == $"EDMap-{Layout!.ToLower()}.txt")
                    {
                        LogInfo($"Key map for layout '{Layout}' has changed, reloading …");
                        KeyMap = null;
                        LoadBinds(Binds);
                    }
                    else if (name == "StartPreset.start")
                    {
                        LogInfo("Controls preset has changed, reloading …");
                        Preset = null;
                        LoadBinds(Binds);
                    }
                    else if (Regex.Match(name, $@"{Preset}(\.[34]\.0)?\.binds$").Success)
                    {
                        LogInfo($"Bindings file '{name}' has changed, reloading …");
                        Binds = null;
                        LoadBinds(Binds);
                        
                        // copy Odyssey -> Horizons
                        if (name == $"{Preset}.4.0.binds" && !_VA!.GetBoolean("bindED.disableHorizonsSync#"))
                        {
                            File.WriteAllText(
                                Path.Combine(_bindingsDir, $"{Preset}.3.0.binds"),
                                File.ReadAllText(Path.Combine(_bindingsDir, name))
                                    .Replace("MajorVersion=\"4\" MinorVersion=\"0\">", "MajorVersion=\"3\" MinorVersion=\"0\">")
                                );
                        }
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
