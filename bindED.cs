// <copyright file="bindED.cs" company="alterNERDtive">
// Copyright 2020–2022 alterNERDtive.
//
// This file is part of bindED VoiceAttack plugin.
//
// bindED VoiceAttack plugin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// bindED VoiceAttack plugin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with bindED VoiceAttack plugin.  If not, see &lt;https://www.gnu.org/licenses/&gt;.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

using alterNERDtive.Yavapf;
using VoiceAttack;

namespace bindEDplugin
{
    /// <summary>
    /// This VoiceAttack plugin reads Elite Dangerous .binds files for keyboard
    /// bindings and makes them available in VoiceAttack variables.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "historic, grandfathered in")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "historic, grandfathered in")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "historic, grandfathered in")]
    public class bindEDPlugin : VoiceAttackPlugin
    {
        private static readonly bindEDPlugin Plugin;

        private static readonly string BindingsDir = Path.Combine(
            Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
            @"Frontier Developments\Elite Dangerous\Options\Bindings");

        private static readonly Dictionary<string, int> FileEventCount = new ();

        private static string? pluginPath;
        private static FileSystemWatcher? bindsWatcher;
        private static FileSystemWatcher? mapWatcher;
        private static string? layout;
        private static Dictionary<string, int>? keyMap;
        private static string? preset;
        private static Dictionary<string, List<string>>? binds;

        static bindEDPlugin()
        {
            Plugin = new ()
            {
                Name = "bindED",
                Version = "4.2.3",
                Info = "bindED Plugin\n\n2016 VoiceAttack.com\n2020–2022 alterNERDtive",
                Guid = "{524B4B9A-3965-4045-A39A-A239BF6E2838}",
            };
        }

        private static FileSystemWatcher BindsWatcher
        {
            get
            {
                if (bindsWatcher == null)
                {
                    bindsWatcher = new FileSystemWatcher(BindingsDir);
                    bindsWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                    bindsWatcher.Changed += (source, eventArgs) => { FileChangedHandler(eventArgs.Name); };
                    bindsWatcher.Created += (source, eventArgs) => { FileChangedHandler(eventArgs.Name); };
                    bindsWatcher.Renamed += (source, eventArgs) => { FileChangedHandler(eventArgs.Name); };
                }

                return bindsWatcher!;
            }
        }

        private static FileSystemWatcher MapWatcher
        {
            get
            {
                if (mapWatcher == null)
                {
                    mapWatcher = new FileSystemWatcher(pluginPath);
                    mapWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                    mapWatcher.Changed += (source, eventArgs) => { FileChangedHandler(eventArgs.Name); };
                    mapWatcher.Created += (source, eventArgs) => { FileChangedHandler(eventArgs.Name); };
                    mapWatcher.Renamed += (source, eventArgs) => { FileChangedHandler(eventArgs.Name); };
                }

                return mapWatcher!;
            }
        }

        private static string? Layout
        {
            get => layout ??= Plugin.Get<string>("bindED.layout#") ?? "en-us";
            set
            {
                layout = value;
                KeyMap = null;
            }
        }

        private static Dictionary<string, int>? KeyMap
        {
            get => keyMap ??= LoadKeyMap(Layout!);
            set => keyMap = value;
        }

        private static string? Preset
        {
            get => preset ??= DetectPreset();
            set
            {
                preset = value;
                Binds = null;
            }
        }

        private static Dictionary<string, List<string>>? Binds
        {
            get => binds ??= ReadBinds(DetectBindsFile(Preset!));
            set => binds = value;
        }

        /// <summary>
        /// The plugin’s display name, as required by the VoiceAttack plugin API.
        /// </summary>
        /// <returns>The display name.</returns>
        public static string VA_DisplayName() => Plugin.VaDisplayName();

        /// <summary>
        /// The plugin’s description, as required by the VoiceAttack plugin API.
        /// </summary>
        /// <returns>The description.</returns>
        public static string VA_DisplayInfo() => Plugin.VaDisplayInfo();

        /// <summary>
        /// The plugin’s GUID, as required by the VoiceAttack plugin API.
        /// </summary>
        /// <returns>The GUID.</returns>
        public static Guid VA_Id() => Plugin.VaId();

        /// <summary>
        /// The Init method, as required by the VoiceAttack plugin API.
        /// Runs when the plugin is initially loaded.
        /// </summary>
        /// <param name="vaProxy">The VoiceAttack proxy object.</param>
        public static void VA_Init1(dynamic vaProxy) => Plugin.VaInit1(vaProxy);

        /// <summary>
        /// The Invoke method, as required by the VoiceAttack plugin API.
        /// Runs whenever a plugin context is invoked.
        /// </summary>
        /// <param name="vaProxy">The VoiceAttack proxy object.</param>
        public static void VA_Invoke1(dynamic vaProxy) => Plugin.VaInvoke1(vaProxy);

        /// <summary>
        /// The Exit method, as required by the VoiceAttack plugin API.
        /// Runs when VoiceAttack is shut down.
        /// </summary>
        /// <param name="vaProxy">The VoiceAttack proxy object.</param>
        public static void VA_Exit1(dynamic vaProxy) => Plugin.VaExit1(vaProxy);

        /// <summary>
        /// The StopCommand method, as required by the VoiceAttack plugin API.
        /// Runs whenever all commands are stopped using the “Stop All Commands”
        /// button or action.
        /// </summary>
        public static void VA_StopCommand() => Plugin.VaStopCommand();

        /// <summary>
        /// Loads the current binds, enables watching the binds directory for
        /// changes.
        /// </summary>
        /// <param name="vaProxy">The <see cref="VoiceAttackInitProxyClass"/>
        /// proxy object.</param>
        [Init]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required by plugin API")]
        public static void Init(VoiceAttackInitProxyClass vaProxy)
        {
            Plugin.Log.LogLevel = LogLevel.INFO;
            pluginPath = Path.GetDirectoryName(Plugin.Proxy.PluginPath());
            Plugin.Set<string>("bindED.fork", "alterNERDtive");

            try
            {
                LoadBinds(Binds);
            }
            catch (Exception e)
            {
                Plugin.Log.Error(e.Message);
            }
            finally
            {
                BindsWatcher.EnableRaisingEvents = true;
                MapWatcher.EnableRaisingEvents = true;
            }
        }

        /// <summary>
        /// Logs some diagnostics regarding the current state of the plugin.
        /// </summary>
        /// <param name="vaProxy">The <see cref="VoiceAttackInvokeProxyClass"/>
        /// proxy object.</param>
        [Context("diagnostics")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required by plugin API")]
        public static void DiagnosticsContext(VoiceAttackInvokeProxyClass vaProxy)
        {
            Plugin.Log.Info($"Current keybord layout: {Layout}");
            Plugin.Log.Info($"Current preset: {Preset}");
            Plugin.Log.Info($"Detected binds file: {new FileInfo(DetectBindsFile(Preset!)).Name}");
            Plugin.Log.Info($"Detected binds file (full path): {DetectBindsFile(Preset!)}");
        }

        /// <summary>
        /// Lists all currently loaded binds.
        /// </summary>
        /// <param name="vaProxy">The <see cref="VoiceAttackInvokeProxyClass"/>
        /// proxy object.</param>
        [Context("listbinds")]
        public static void ListBindsContext(VoiceAttackInvokeProxyClass vaProxy)
        {
            vaProxy.Set<string>("~bindED.bindsList", string.Join(vaProxy.Get<string>("bindED.separator") ?? "\n", Binds!.Keys));
            Plugin.Log.Info("List of Elite binds saved to TXT variable '~bindED.bindsList'.");
        }

        /// <summary>
        /// Forces a reload of all binds.
        /// </summary>
        /// <param name="vaProxy">The <see cref="VoiceAttackInvokeProxyClass"/>
        /// proxy object.</param>
        [Context("loadbinds")]
        public static void LoadBindsContext(VoiceAttackInvokeProxyClass vaProxy)
        {
            // force reset everything
            Layout = null;
            Preset = null;
            if (!string.IsNullOrWhiteSpace(vaProxy.Get<string>("~bindsFile")))
            {
                Binds = ReadBinds(Path.Combine(BindingsDir, vaProxy.Get<string>("~bindsFile")));
            }

            LoadBinds(Binds);
        }

        /// <summary>
        /// Reports missing binds.
        /// </summary>
        /// <param name="vaProxy">The <see cref="VoiceAttackInvokeProxyClass"/>
        /// proxy object.</param>
        [Context("missingbinds")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required by plugin API")]
        public static void MissingBindsContext(VoiceAttackInvokeProxyClass vaProxy)
        {
            List<string> missing = new (256);
            foreach (KeyValuePair<string, List<string>> bind in Binds!)
            {
                if (bind.Value.Count == 0)
                {
                    missing.Add(bind.Key);
                }
            }

            if (missing.Count > 0)
            {
                vaProxy.Set<string>("~bindED.missingBinds", string.Join("\n", missing));
                vaProxy.Set<bool>("~bindED.missingBinds", true);
                Plugin.Log.Info("List of missing Elite binds saved to TXT variable '~bindED.missingBinds'.");
            }
            else
            {
                Plugin.Log.Info($"No missing keyboard binds found.");
            }
        }

        /// <summary>
        /// Gives an explicit error message if the plugin is invoked without a
        /// plugin context.
        /// </summary>
        /// <param name="vaProxy">The <see cref="VoiceAttackInvokeProxyClass"/>
        /// proxy object.</param>
        [Context("")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required by plugin API")]
        public static void EmptyContext(VoiceAttackInvokeProxyClass vaProxy)
        {
            Plugin.Log.Error("Empty plugin context. You generally do not need to invoke the plugin manually.");
        }

        /// <summary>
        /// Handles changes to the keyboard layout.
        /// </summary>
        /// <param name="name">The variable name (not used).</param>
        /// <param name="from">The old keyboard layout.</param>
        /// <param name="to">The new keyboard layout.</param>
        [String("bindED.layout#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required by plugin API")]
        public static void LayoutChanged(string name, string from, string to)
        {
            Plugin.Log.Info($"Keyboard layout changed to '{to}', reloading …");
            Layout = to;
            try
            {
                LoadBinds(Binds);
            }
            catch (Exception e)
            {
                Plugin.Log.Error(e.Message);
            }
        }

        private static void LoadBinds(Dictionary<string, List<string>>? binds)
        {
            foreach (KeyValuePair<string, List<string>> bind in binds!)
            {
                string value = string.Empty;
                bool valid = true;
                if (bind.Value.Count == 0)
                {
                    // LogInfo($"No keyboard bind for '{bind.Key}' found, skipping …");
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
                            Plugin.Log.Warn($"No valid key code for '{key}' found, skipping bind for '{bind.Key}' …");
                        }
                    }

                    if (valid)
                    {
                        Plugin.Set<string>(bind.Key, value);
                    }
                }
            }

            //FIXXME: suppress warning for possible race condition
            Plugin.Log.Info($"Elite binds '{(string.IsNullOrWhiteSpace(Plugin.Get<string>("~bindsFile")) ? Preset : Plugin.Get<string>("~bindsFile"))}' for layout '{Layout}' loaded successfully.");
        }

        private static Dictionary<string, int> LoadKeyMap(string layout)
        {
            string mapFile = Path.Combine(pluginPath, $"EDMap-{layout.ToLower()}.txt");
            if (!File.Exists(mapFile))
            {
                throw new FileNotFoundException($"No map file for layout '{layout}' found.");
            }

            Dictionary<string, int> map = new (256);
            foreach (string line in File.ReadAllLines(mapFile, System.Text.Encoding.UTF8))
            {
                string[] arItem = line.Split(";".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries);
                if ((arItem.Count() == 2) && (!string.IsNullOrWhiteSpace(arItem[0])) && (!map.ContainsKey(arItem[0])))
                {
                    if (ushort.TryParse(arItem[1], out ushort iKey))
                    {
                        if (iKey > 0 && iKey < 256)
                        {
                            map.Add(arItem[0].Trim(), iKey);
                        }
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
            string startFile = Path.Combine(BindingsDir, "StartPreset.4.start");
            if (!File.Exists(startFile))
            {
                startFile = Path.Combine(BindingsDir, "StartPreset.start");
                if (!File.Exists(startFile))
                {
                    throw new FileNotFoundException("No 'StartPreset.start' file found. Please run Elite: Dangerous at least once, then restart VoiceAttack.");
                }
            }

            IEnumerable<string> presets = File.ReadAllLines(startFile).Distinct();
            if (presets.Count() > 1)
            {
                Plugin.Log.Warn($"You have selected multiple control presets ('{string.Join("', '", presets)}'). "
                    + $"Only binds from '{presets.First()}' will be used. Please refer to the documentation for more information.");
            }

            return presets.First();
        }

        private static string DetectBindsFile(string preset)
        {
            DirectoryInfo dirInfo = new (BindingsDir);
            FileInfo[] bindFiles = dirInfo.GetFiles()
                .Where(i => Regex.Match(i.Name, $@"^{Regex.Escape(preset)}\.[34]\.0\.binds$").Success)
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

            Dictionary<string, List<string>> binds = new (512);
            if (rootElement != null)
            {
                foreach (XElement c in rootElement.Elements().Where(i => i.Elements().Count() > 0))
                {
                    List<string> keys = new ();
                    foreach (var element in c.Elements().Where(i => i.HasAttributes))
                    {
                        if (element.Name == "Primary")
                        {
                            if (element.Attribute("Device").Value == "Keyboard"
                                && !string.IsNullOrWhiteSpace(element.Attribute("Key").Value) && element.Attribute("Key").Value.StartsWith("Key_"))
                            {
                                foreach (var modifier in element.Elements().Where(i => i.Name.LocalName == "Modifier"))
                                {
                                    keys.Add(modifier.Attribute("Key").Value);
                                }

                                keys.Add(element.Attribute("Key").Value);
                            }
                        }

                        if (keys.Count == 0 && element.Name == "Secondary")
                        { // nothing found in primary... look in secondary
                            if (element.Attribute("Device").Value == "Keyboard"
                                && !string.IsNullOrWhiteSpace(element.Attribute("Key").Value) && element.Attribute("Key").Value.StartsWith("Key_"))
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
            if (FileEventCount.ContainsKey(name))
            {
                FileEventCount[name] += 1;
            }
            else
            {
                FileEventCount.Add(name, 1);
            }

            if (FileEventCount[name] % 2 == 0)
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
                        Plugin.Log.Info($"Key map for layout '{Layout}' has changed, reloading …");
                        KeyMap = null;
                        LoadBinds(Binds);
                    }
                    else if (name == "StartPreset.start")
                    {
                        Plugin.Log.Info("Controls preset has changed, reloading …");
                        Preset = null;
                        LoadBinds(Binds);
                    }
                    else if (Regex.Match(name, $@"{Preset}(\.[34]\.0)?\.binds$").Success)
                    {
                        Plugin.Log.Info($"Bindings file '{name}' has changed, reloading …");
                        Binds = null;
                        LoadBinds(Binds);

                        // copy Odyssey -> Horizons
                        if (name == $"{Preset}.4.0.binds" && !Plugin.Get<bool>("bindED.disableHorizonsSync#"))
                        {
                            File.WriteAllText(
                                Path.Combine(BindingsDir, $"{Preset}.3.0.binds"),
                                File.ReadAllText(Path.Combine(BindingsDir, name))
                                    .Replace("MajorVersion=\"4\" MinorVersion=\"0\">", "MajorVersion=\"3\" MinorVersion=\"0\">"));
                        }
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.Error(e.Message);
                }
            }
        }
    }
}
