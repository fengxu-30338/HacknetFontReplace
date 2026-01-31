using Hacknet.Extensions;
using HacknetFontReplace.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FontStashSharp;
using Pathfinder.Util;
using Pathfinder.Util.XML;

namespace HacknetFontReplace.Core
{
    public class FontReplaceConfig
    {
        private FontReplaceConfig(){}

        /// <summary>
        /// 大字体；如Display面板中的连接到xxx字样
        /// </summary>
        public int LargeFontSize { get; private set; } = 34;

        /// <summary>
        /// 小字体；如您是本系统管理员字样
        /// </summary>
        public int SmallFontSize { get; private set; } = 20;

        /// <summary>
        /// UI字体大小
        /// </summary>
        public int UIFontSize { get; private set; } = 18;

        /// <summary>
        /// 左上角Ram模块，AppBar等字样字体大小
        /// </summary>
        public int DetailFontSize { get; private set; } = 14;

        /// <summary>
        /// 修改字体大小时的间隔
        /// </summary>
        public int ChangeFontSizeInterval { get; private set; } = 2;

        /// <summary>
        /// 是否开启多色字体解析
        /// </summary>
        public bool OpenMultiColorFontParse { get; private set; } = false;

        /// <summary>
        /// 组名->list(字体路径列表)
        /// </summary>
        public Dictionary<string, List<string>> FontGroups = new Dictionary<string, List<string>>();

        
        private string _activeFontGroup = string.Empty;
        /// <summary>
        /// 当前激活使用的字体组
        /// </summary>
        public string ActiveFontGroup
        {
            get => _activeFontGroup;
            set
            {
                if (!FontGroups.ContainsKey(value))
                {
                    throw new InvalidOperationException($"Not Find Font Group: '{value}'");
                }

                if (value != _activeFontGroup)
                {
                    _activeFontGroup = value;
                    OnActiveFontGroupChanged?.Invoke(value);
                }
            }
        }

        //===================================================================================================================

        public event Action<string> OnActiveFontGroupChanged; 

        private readonly Dictionary<string, FontSystem> _fontSystems = new Dictionary<string, FontSystem>();

        public FontSystem GetActiveFontSystem()
        {
            if (_fontSystems.TryGetValue(ActiveFontGroup, out var fontSystem))
            {
                return fontSystem;
            }

            if (!FontGroups.TryGetValue(ActiveFontGroup, out var fontFileList))
            {
                throw new InvalidOperationException($"Not Find Font Group: '{ActiveFontGroup}'");
            }

            if (!fontFileList.Any())
            {
                throw new InvalidOperationException($"Font Group[{ActiveFontGroup}] Not Include Any Font File");
            }

            fontSystem = new FontSystem();
            fontFileList.ForEach(filepath => fontSystem.AddFont(File.ReadAllBytes(filepath)));
            _fontSystems[ActiveFontGroup] = fontSystem;

            return fontSystem;
        }

        private string FormatFontGroups()
        {
            var sb = new StringBuilder();
            foreach (var keyValuePair in FontGroups)
            {
                sb.AppendLine($"{keyValuePair.Key} => [{string.Join(",", keyValuePair.Value)}]");
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return
                $"{nameof(FontGroups)}:\n {FormatFontGroups()} \n" +
                $"{nameof(LargeFontSize)}: {LargeFontSize}, " +
                $"{nameof(SmallFontSize)}: {SmallFontSize}, " +
                $"{nameof(UIFontSize)}: {UIFontSize}, " +
                $"{nameof(DetailFontSize)}: {DetailFontSize}, " +
                $"{nameof(ChangeFontSizeInterval)}: {ChangeFontSizeInterval}, " +
                $"{nameof(OpenMultiColorFontParse)}: {OpenMultiColorFontParse}, " +
                $"{nameof(ActiveFontGroup)}: {ActiveFontGroup}";
        }


        private static string GetSearchFolder()
        {
            if (ExtensionLoader.ActiveExtensionInfo != null)
            {
                return Path.Combine(ExtensionLoader.ActiveExtensionInfo.FolderPath, "Plugins/Font");
            }

            var dir = Path.GetDirectoryName(AssemblyPathHelper.GetCleanAssemblyPath(typeof(FontReplaceConfig).Assembly));
            if (string.IsNullOrEmpty(dir))
            {
                throw new InvalidOperationException("Get FontConfig Folder Failed");
            }

            return Path.Combine(dir, "Font");
        }

        private static FontReplaceConfig ParseConfig(string xmlText)
        {
            var config = new FontReplaceConfig();
            var eventExecutor = new EventExecutor(xmlText, false);
            eventExecutor.RegisterExecutor("HacknetFontReplace.LargeFontSize", (exec, info) =>
            {
                config.LargeFontSize = info.Content.TryGetAsInt(config.LargeFontSize);
            }, ParseOption.ParseInterior);

            eventExecutor.RegisterExecutor("HacknetFontReplace.SmallFontSize", (exec, info) =>
            {
                config.SmallFontSize = info.Content.TryGetAsInt(config.SmallFontSize);
            }, ParseOption.ParseInterior);

            eventExecutor.RegisterExecutor("HacknetFontReplace.UIFontSize", (exec, info) =>
            {
                config.UIFontSize = info.Content.TryGetAsInt(config.UIFontSize);
            }, ParseOption.ParseInterior);

            eventExecutor.RegisterExecutor("HacknetFontReplace.DetailFontSize", (exec, info) =>
            {
                config.DetailFontSize = info.Content.TryGetAsInt(config.DetailFontSize);
            }, ParseOption.ParseInterior);

            eventExecutor.RegisterExecutor("HacknetFontReplace.ChangeFontSizeInterval", (exec, info) =>
            {
                config.ChangeFontSizeInterval = info.Content.TryGetAsInt(config.ChangeFontSizeInterval);
            }, ParseOption.ParseInterior);

            eventExecutor.RegisterExecutor("HacknetFontReplace.OpenMultiColorFontParse", (exec, info) =>
            {
                config.OpenMultiColorFontParse = info.Content.TryGetAsBool(config.OpenMultiColorFontParse);
            }, ParseOption.ParseInterior);

            eventExecutor.RegisterExecutor("HacknetFontReplace.FontGroups", (exec, info) =>
            {
                foreach (var child in info.Children.Where(childInfo => childInfo.Name == "FontGroup"))
                {
                    var groupName = child.Attributes.GetString("Name");
                    if (!groupName.HasContent())
                    {
                        continue;
                    }

                    var fontPathList = child.Children.Where(subInfo => subInfo.Name == "FontPath" && subInfo.Content.HasContent())
                        .Select(s => Path.Combine(ExtensionLoader.ActiveExtensionInfo.FolderPath, s.Content))
                        .ToList();

                    if (!fontPathList.Any())
                    {
                        throw new InvalidOperationException($"Font Group[{groupName}] Not Include Any Font File");
                    }

                    foreach (var fontPath in fontPathList)
                    {
                        if (!File.Exists(fontPath))
                        {
                            throw new FileNotFoundException($"FontFile:'{fontPath}' Not Find");
                        }
                    }

                    config.FontGroups[groupName] = fontPathList;
                }
            }, ParseOption.ParseInterior);


            eventExecutor.RegisterExecutor("HacknetFontReplace.ActiveFontGroup", (exec, info) =>
            {
                if (info.Content.HasContent())
                {
                    config.ActiveFontGroup = info.Content.Trim();
                }
            }, ParseOption.ParseInterior);

            eventExecutor.Parse();

            return config;
        }


        public static FontReplaceConfig Load()
        {
            var fontDir = GetSearchFolder();
            if (!Directory.Exists(fontDir))
            {
                throw new DirectoryNotFoundException($"Font Replace Config Folder:'{fontDir}' Not Exist");
            }

            var fontConfigFile = Path.Combine(fontDir, "HacknetFontReplace.config.xml");
            if (!File.Exists(fontConfigFile))
            {
                throw new FileNotFoundException($"Font Replace Config File:'{fontConfigFile}' Not Find");
            }

            return ParseConfig(File.ReadAllText(fontConfigFile));
        }

    }
}
