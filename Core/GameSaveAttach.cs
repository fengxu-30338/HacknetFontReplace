using System.Xml.Linq;
using Pathfinder.Event;
using Pathfinder.Event.Saving;
using Pathfinder.Replacements;
using Pathfinder.Util;
using Pathfinder.Util.XML;
using static Pathfinder.Replacements.SaveLoader;

namespace HacknetFontReplace.Core
{
    public class GameSaveAttach
    {
        public const string ActiveFontGroupKey = "ActiveFontGroup";

        public static void Init()
        {
            EventManager<SaveEvent>.AddHandler(OnGameSave);
            SaveLoader.RegisterExecutor<FontReplaceSaveLoaderExecutor>($"HacknetSave.{nameof(HacknetFontReplace)}", ParseOption.ParseInterior);
        }

        public static void OnGameSave(SaveEvent e)
        {
            var element = new XElement(nameof(HacknetFontReplace));
            element.SetAttributeValue(ActiveFontGroupKey, GameFontReplace.FontConfig.ActiveFontGroup);
            e.Save.AddFirst(element);
        }
    }

    public class FontReplaceSaveLoaderExecutor : SaveExecutor
    {
        public override void Execute(EventExecutor exec, ElementInfo info)
        {
            var activeFontGroup = info.Attributes.GetString(GameSaveAttach.ActiveFontGroupKey);
            if (!string.IsNullOrWhiteSpace(activeFontGroup))
            {
                GameFontReplace.FontConfig.ActiveFontGroup = activeFontGroup;
            }
        }
    }
}
