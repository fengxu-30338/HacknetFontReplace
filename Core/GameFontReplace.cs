using System;
using FontStashSharp;
using Hacknet;
using Hacknet.Localization;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using HacknetFontReplace.Core.Parser;

namespace HacknetFontReplace.Core
{
    [HarmonyPatch]
    public static class GameFontReplace
    {
        private static bool defaultInstalled = false;
        private static readonly Dictionary<SpriteFont, DynamicSpriteFont> defaultFontMap = new Dictionary<SpriteFont, DynamicSpriteFont>();
        private static readonly Dictionary<SpriteFont, DynamicSpriteFont> localeFontMap = new Dictionary<SpriteFont, DynamicSpriteFont>();
        private static FontReplaceConfig fontReplaceConfig;
        private static readonly SpecialTextParser specialTextParser = new SpecialTextParser();
        private static readonly SpecialTextCache specialTextCache = new SpecialTextCache(300);
        private const int AddToCacheMinLength = 5;

        private static FontSystem fontSystem => fontReplaceConfig.GetActiveFontSystem();
        public static FontReplaceConfig FontConfig => fontReplaceConfig;

        public static void Init()
        {
            Interlocked.MemoryBarrier();
            fontReplaceConfig = FontReplaceConfig.Load();
            fontReplaceConfig.OnActiveFontGroupChanged += _ =>
            {
                FixDefaultFont(true);
                FixLocaleFont(GuiData.ActiveFontConfig);
            };
            FixDefaultFont();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Game1), nameof(Game1.LoadContent))]
        private static void PostFixGameLoadContent()
        {
            Thread.CurrentThread.Name = "MAIN";
            FixDefaultFont();
            FixLocaleFont(GuiData.ActiveFontConfig);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GuiData), nameof(GuiData.ActivateFontConfig), typeof(GuiData.FontCongifOption))]
        private static void FixLocaleFont(GuiData.FontCongifOption config)
        {
            var baseChangeFontSizeInterval = config.name == "default" ? 0 : (config.name == "medium" ? fontReplaceConfig.ChangeFontSizeInterval : fontReplaceConfig.ChangeFontSizeInterval * 2);
            ClearLocaleFontSystemDic();
            config.tinyFontCharHeight = fontReplaceConfig.UIFontSize;
            if (config.detailFont != null)
            {
                localeFontMap[config.detailFont] = fontSystem.GetFont(fontReplaceConfig.DetailFontSize);
            }

            if (config.smallFont != null)
            {
                localeFontMap[config.smallFont] = fontSystem.GetFont(fontReplaceConfig.SmallFontSize + baseChangeFontSizeInterval);
            }

            if (config.tinyFont != null)
            {
                localeFontMap[config.tinyFont] = fontSystem.GetFont(fontReplaceConfig.UIFontSize + baseChangeFontSizeInterval);
            }

            if (config.bigFont != null)
            {
                localeFontMap[config.bigFont] = fontSystem.GetFont(fontReplaceConfig.LargeFontSize);
            }
            
            GuiData.ActiveFontConfig.tinyFontCharHeight = fontReplaceConfig.UIFontSize + baseChangeFontSizeInterval;
            UpdateLocaleFontSystemDic();
        }

        private static void ClearLocaleFontSystemDic()
        {
            var keys = localeFontMap.Keys.ToList();
            keys.ForEach(font =>
            {
                defaultFontMap.Remove(font);
                localeFontMap.Remove(font);
            });
        }

        private static void UpdateLocaleFontSystemDic()
        {
            var keys = localeFontMap.Keys.ToList();
            keys.ForEach(font =>
            {
                defaultFontMap[font] = localeFontMap[font];
            });
        }


        private static void FixDefaultFont(bool forceFix = false)
        {
            if (GuiData.font != null && (!defaultInstalled || forceFix))
            {
                // 连接到xxx字样
                defaultFontMap[GuiData.font] = fontSystem.GetFont(fontReplaceConfig.LargeFontSize);
                // 您是本系统管理员字样
                defaultFontMap[GuiData.smallfont] = fontSystem.GetFont(fontReplaceConfig.SmallFontSize);
                // UI等字样
                defaultFontMap[GuiData.tinyfont] = fontSystem.GetFont(fontReplaceConfig.UIFontSize);
                // appbar,ram模块等字样
                defaultFontMap[GuiData.detailfont] = fontSystem.GetFont(fontReplaceConfig.DetailFontSize);
                defaultInstalled = true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SpriteFont), nameof(SpriteFont.MeasureString), typeof(string))]
        private static bool PrefixMeasureString(SpriteFont __instance, string text, ref Vector2 __result)
        {
            if (!defaultFontMap.TryGetValue(__instance, out var dynamicSpriteFont))
            {
                return true;
            }

            if (!CheckNeedHandleSpecialText(text))
            {
                __result = dynamicSpriteFont.MeasureString(text);
            }
            else
            {
                HandleSpecialText(text, out var parserResult);
                __result = dynamicSpriteFont.MeasureString(parserResult.Text);
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SpriteFont), nameof(SpriteFont.MeasureString), typeof(StringBuilder))]
        private static bool PrefixMeasureStringBuilder(SpriteFont __instance, StringBuilder text, ref Vector2 __result)
        {
            if (!defaultFontMap.TryGetValue(__instance, out var dynamicSpriteFont))
            {
                return true;
            }

            var content = text.ToString();

            if (!CheckNeedHandleSpecialText(content))
            {
                __result = dynamicSpriteFont.MeasureString(text);
            }
            else
            {
                HandleSpecialText(content, out var parserResult);
                __result = dynamicSpriteFont.MeasureString(parserResult.Text);
            }

            return false;
        }

        private static bool CheckNeedHandleSpecialText(string text)
        {
            return fontReplaceConfig.OpenMultiColorFontParse && !string.IsNullOrWhiteSpace(text) &&
                   text.StartsWith(SpecialTextParser.FirstChar);
        }

        private static void HandleSpecialText(string text, out SpecialFontParserResult parserResult, bool addToCache = false)
        {
            if (specialTextCache.TryGetSpecialTextResult(text, out parserResult))
            {
                return;
            }

            if (parserResult != null) return;
            parserResult = specialTextParser.ParseText(text);
            if (text.Length >= AddToCacheMinLength && addToCache)
            {
                specialTextCache.AddToCache(text, parserResult);
            }
        }

        private static DynamicSpriteFont GetDynamicSpriteFont(string fontGroup, SpriteFont spriteFont)
        {
            var fontSys = fontReplaceConfig.GetFontSystem(fontGroup);
            if (!defaultFontMap.TryGetValue(spriteFont, out var dynamicSpriteFont))
            {
                return null;
            }

            return fontSys.GetFont(dynamicSpriteFont.FontSize);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.DrawString), 
            typeof(SpriteFont), typeof(string), typeof(Vector2), typeof(Color), typeof(float), 
            typeof(Vector2), typeof(Vector2), typeof(SpriteEffects), typeof(float))]
        private static bool PreFixDawString(SpriteBatch __instance,
            SpriteFont spriteFont,
            string text,
            Vector2 position,
            Color color,
            float rotation,
            Vector2 origin,
            Vector2 scale,
            SpriteEffects effects,
            float layerDepth)
        {
            if (!defaultFontMap.TryGetValue(spriteFont, out var dynamicSpriteFont))
            {
                return true;
            }

            if (!CheckNeedHandleSpecialText(text))
            {
                __instance.DrawString(dynamicSpriteFont, text, position, color, rotation, origin, scale, layerDepth);
                return false;
            }

            specialTextParser.DefaultCharProp.color = color;
            specialTextParser.DefaultCharProp.fontGroup = fontReplaceConfig.ActiveFontGroup;
            HandleSpecialText(text, out var parserResult, true);
            if (!parserResult.IsSuccess || !parserResult.IsSpecial)
            {
                __instance.DrawString(dynamicSpriteFont, parserResult.Text, position, color, rotation, origin, scale, layerDepth);
                return false;
            }

            if (parserResult.DrawTexts.Count == 1)
            {
                var drawText = parserResult.DrawTexts[0];
                var dsf = GetDynamicSpriteFont(drawText.FontGroup, spriteFont);
                if (dsf == null)
                {
                    return true;
                }

                drawText.ApplyFont(dsf, position);
                __instance.DrawString(dsf, drawText.Text, position, drawText.Colors, rotation, origin, scale, layerDepth);
                return false;
            }

            var pos = position;
            foreach (var drawText in parserResult.DrawTexts)
            {
                var dsf = GetDynamicSpriteFont(drawText.FontGroup, spriteFont);
                if (dsf == null)
                {
                    __instance.DrawString(dynamicSpriteFont, drawText.Text, position, color, rotation, origin, scale, layerDepth);
                    continue;
                }
                var size = dsf.MeasureString(drawText.Text, scale);
                drawText.ApplyFont(dsf, position);
                __instance.DrawString(dsf, drawText.Text, pos, drawText.Colors, rotation, origin, scale, layerDepth);
                pos.X += size.X;
            }

            return false;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocalizedFileLoader), nameof(LocalizedFileLoader.SafeFilterString))]
        private static bool PrefixSafeFilterString(string data, ref string __result)
        {
            __result = data;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Hacknet.Utils), nameof(Hacknet.Utils.CleanStringToLanguageRenderable))]
        private static bool PrefixCleanStringToLanguageRenderable(string input, ref string __result)
        {
            input = input.Replace("\t", "    ");
            StringBuilder stringBuilder = new StringBuilder();
            string source = "\r\n";
            for (int index = 0; index < input.Length; ++index)
            {
                if (source.Contains<char>(input[index]))
                    stringBuilder.Append(input[index]);
                else
                    stringBuilder.Append(input[index]);
            }
            __result = stringBuilder.ToString();
            return false;
        }

        /// <summary>
        /// 使 “您是系统管理员字样居中，获取其Y值”
        /// </summary>
        /// <returns></returns>
        private static float GetDoConnectHeaderY(ref Rectangle rect, ref Vector2 measure)
        {
            return rect.Y + rect.Height / 2 - measure.Y / 2;
        }

        [HarmonyILManipulator]
        [HarmonyPatch(typeof(DisplayModule), nameof(DisplayModule.doConnectHeader))]
        private static void FixDoConnectHeader(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.Before,
                x => x.MatchLdfld(typeof(Rectangle), nameof(Rectangle.Y)),
                x => x.MatchConvR4(),
                x => x.MatchNewobj(AccessTools.Constructor(typeof(Vector2), new []{ typeof(float), typeof(float) }))
            );

            c.RemoveRange(2);
            // load measure
            c.Emit(OpCodes.Ldloca, 4);
            c.Emit(OpCodes.Call, AccessTools.Method(typeof(GameFontReplace), nameof(GetDoConnectHeaderY)));

            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(2f),
                x => x.MatchSub(),
                x => x.MatchStfld(typeof(Vector2), nameof(Vector2.Y))
            );
            c.RemoveRange(1);
            c.Emit(OpCodes.Ldc_R4, 0.5f);
        }

        /// <summary>
        /// 计算右上角文字居中位置的Y值
        /// </summary>
        private static float GetLocationTextY(ref Vector2 measure)
        {
            var rect = OS.currentInstance.topBar;
            return rect.Y + rect.Height / 2 - measure.Y / 2 + 0.5f;
        }

        [HarmonyILManipulator]
        [HarmonyPatch(typeof(OS), nameof(OS.drawModules))]
        private static void FixDrawModules(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.Before,
                x => x.MatchLdfld(typeof(Vector2), nameof(Vector2.Y)),
                x => x.MatchLdcR4(3f),
                x => x.MatchSub()
            );

            c.RemoveRange(3);
            c.Emit(OpCodes.Nop);
            // load measure
            c.Emit(OpCodes.Ldloca, 2);
            c.Emit(OpCodes.Call, AccessTools.Method(typeof(GameFontReplace), nameof(GetLocationTextY)));
        }

        private static float GetIntroTextModuleFontHeight()
        {
            return fontReplaceConfig.SmallFontSize + 2f;
        }

        [HarmonyILManipulator]
        [HarmonyPatch(typeof(IntroTextModule), nameof(IntroTextModule.Draw))]
        private static void FixIntroTextModuleDraw(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.Before,
                x => x.MatchLdfld(typeof(Vector2), nameof(Vector2.Y)),
                x => x.MatchLdcR4(16f),
                x => x.MatchAdd()
            );
            c.Index += 1;
            c.RemoveRange(1);
            c.Emit(OpCodes.Call, AccessTools.Method(typeof(GameFontReplace), nameof(GetIntroTextModuleFontHeight)));

            c.GotoNext(MoveType.Before,
                x => x.MatchLdfld(typeof(Vector2), nameof(Vector2.Y)),
                x => x.MatchLdcR4(16f),
                x => x.MatchAdd()
            );
            c.Index += 1;
            c.RemoveRange(1);
            c.Emit(OpCodes.Call, AccessTools.Method(typeof(GameFontReplace), nameof(GetIntroTextModuleFontHeight)));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocaleActivator), nameof(LocaleActivator.ActiveLocaleIsCJK))]
        private static bool FixActiveLocaleIsCJK(ref bool __result)
        {
            __result = true;
            return false;
        }
    }
}
