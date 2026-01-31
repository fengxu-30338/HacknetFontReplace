using Pathfinder.Action;
using Pathfinder.Util;

namespace HacknetFontReplace.Core.Actions
{
    public class ChangeActiveFontGroup : PathfinderAction
    {
        [XMLStorage]
        public string Name { get; set; }

        public override void Trigger(object os_obj)
        {
            GameFontReplace.FontConfig.ActiveFontGroup = Name;
            HacknetFontReplacePlugin.Logger.LogInfo($"Change Font Group: '{Name}'");
        }
    }
}
