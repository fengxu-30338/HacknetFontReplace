using Hacknet.Extensions;
using HacknetFontReplace.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FontStashSharp;
using Hacknet;
using Microsoft.Xna.Framework.Graphics;
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
        public Dictionary<string, List<string>> FontGroups { get; } = new Dictionary<string, List<string>>();

        /// <summary>
        /// img key -> 路径
        /// </summary>
        public Dictionary<string, string> ImgPathDict { get; } = new Dictionary<string, string>();

        
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

        private readonly Dictionary<string, Texture2D> _imgTexture2Ds = new Dictionary<string, Texture2D>();

        public Texture2D GetImgTexture(string imgKey)
        {
            if (_imgTexture2Ds.TryGetValue(imgKey, out var texture))
            {
                return texture;
            }

            if (!ImgPathDict.TryGetValue(imgKey, out var imgPath))
            {
                throw new InvalidOperationException($"Not Find Img Key: '{imgKey}'");
            }

            if (!File.Exists(imgPath))
            {
                throw new InvalidOperationException($"Img File Not Found: '{imgPath}'");
            }

            using (var fs = File.OpenRead(imgPath))
            {
                texture = Texture2D.FromStream(Game1.singleton.spriteBatch.GraphicsDevice, fs);
                _imgTexture2Ds[imgKey] = texture;
            }

            return texture;
        }

        public FontSystem GetFontSystem(string fontGroup)
        {
            if (_fontSystems.TryGetValue(fontGroup, out var fontSystem))
            {
                return fontSystem;
            }

            if (!FontGroups.TryGetValue(fontGroup, out var fontFileList))
            {
                throw new InvalidOperationException($"Not Find Font Group: '{fontGroup}'");
            }

            if (!fontFileList.Any())
            {
                throw new InvalidOperationException($"Font Group[{fontGroup}] Not Include Any Font File");
            }

            fontSystem = new FontSystem();
            fontFileList.ForEach(filepath => fontSystem.AddFont(File.ReadAllBytes(filepath)));
            _fontSystems[fontGroup] = fontSystem;

            return fontSystem;
        }

        public FontSystem GetActiveFontSystem() => GetFontSystem(ActiveFontGroup);

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

            eventExecutor.RegisterExecutor("HacknetFontReplace.Images.Image", (exec, info) =>
            {
                var key = info.Attributes.GetString("Key");
                var path = info.Content;
                if (!key.HasContent())
                {
                    throw new InvalidOperationException("Image key is missing");
                }

                if (!path.HasContent())
                {
                    throw new InvalidOperationException("Image path is missing");
                }

                path = Path.Combine(ExtensionLoader.ActiveExtensionInfo.FolderPath, path);
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"ImageFile:'{path}' Not Find");
                }

                config.ImgPathDict[key] = path;
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
