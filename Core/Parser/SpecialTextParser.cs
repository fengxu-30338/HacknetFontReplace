using FontStashSharp;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathfinder.Util;
using Sprache;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace HacknetFontReplace.Core.Parser
{
    public class SpecialTextParser
    {
        private static readonly Parser<string> LBParser = Parse.Char('{')
            .Then(_ => Parse.WhiteSpace.Many()).Select(_ => string.Empty);

        private static readonly Parser<string> RBParser = Parse.WhiteSpace.Many()
            .Then(_ => Parse.Char('}')).Select(_ => string.Empty);

        private static readonly Parser<KeyValuePair<string, string>> PropContentParse =
            from key in Parse.LetterOrDigit.AtLeastOnce().Token().Text()
            from _ in Parse.Char(':').Token()
            from value in Parse.Char(ch => char.IsLetterOrDigit(ch) || ch == ' ' || ch == '.' || ch == '-', "prop value").AtLeastOnce().Token().Text()
            select new KeyValuePair<string, string>(key, value);

        private static readonly Parser<IEnumerable<KeyValuePair<string, string>>> MultiPropContentParse =
            from prop in PropContentParse.Optional()
            from item in (
                from _ in Parse.Char(',').Token()
                from prop2 in PropContentParse
                select prop2
            ).Many()
            select item.Concat(prop.IsDefined ? new List<KeyValuePair<string, string>> { prop.Get() } : new List<KeyValuePair<string, string>>());

        private static Parser<IEnumerable<CharInfo>> TextParser;

        private readonly Parser<IEnumerable<CharInfo>> SpecTextParser;

        private readonly Stack<CharProp> PropStack = new Stack<CharProp>();

        private bool _containsSpecial = false;

        /// <summary>
        /// 使用其来观察是否为特殊开始字符
        /// </summary>
        public static readonly string FirstChar = "";

        public CharProp DefaultCharProp { get; private set; }
        public bool IsDebug { get; set; }

        public SpecialTextParser()
        {
            DefaultCharProp = new CharProp(){color = Color.White};
            // 属性开始处解析
            var propStartParser = from lb in LBParser
                                  from item in MultiPropContentParse
                                  from rb in RBParser
                                  select GenerateCurrentProp(item);

            // 属性结束处解析
            Parser<IEnumerable<CharInfo>> propEndParse = Parse.String("{/}")
                .Select(_ =>
                {
                    var chProp = PropStack.Pop();
                    if (IsDebug)
                    {
                        Console.WriteLine($"pop: {chProp}, cout={PropStack.Count}");
                    }
                    return Array.Empty<CharInfo>();
                });

            // 普通字符解析
            var commonTextParser = Parse.CharExcept('{').Many().Select(GetCurrentCharProp);

            /* 解析流程
             * Text = (字符串 + Spec? + 字符串)*
             * Spec = {xxx}Text{/}
             */

            // 文本解析
            TextParser = commonTextParser.Then(res => SpecTextParser.Optional().Select(res2 => res2.IsDefined ? res.Concat(res2.Get()) : res)
                .Then(res2 => commonTextParser.Select(res2.Concat))
            ).Many().Select(res => res.SelectMany(item => item));

            // 特殊字符串解析
            SpecTextParser = propStartParser.Then(_ => TextParser).Then(res => propEndParse.Select(_ => res));
        }

        private IEnumerable<CharInfo> GenerateCurrentProp(IEnumerable<KeyValuePair<string, string>> props)
        {
            var chProp = (CharProp)GetNewCharProp().Clone();
            foreach (var pair in props)
            {
                if (pair.Key == nameof(CharProp.color))
                {
                    chProp.SetColor(pair.Value.Trim());
                    _containsSpecial = true;
                    continue;
                }

                if (pair.Key == nameof(CharProp.fontGroup))
                {
                    chProp.fontGroup = pair.Value.Trim();
                    _containsSpecial = true;
                    continue;
                }

                if (pair.Key == nameof(CharProp.img))
                {
                    chProp.img = pair.Value.Trim();
                    _containsSpecial = true;
                    continue;
                }

                if (pair.Key == nameof(CharProp.scale))
                {
                    chProp.scale = float.Parse(pair.Value.Trim());
                    _containsSpecial = true;
                    continue;
                }

                if (pair.Key == nameof(CharProp.rotate))
                {
                    chProp.SetRotateByDeg(float.Parse(pair.Value.Trim()));
                    _containsSpecial = true;
                    continue;
                }
            }
            PropStack.Push(chProp);

            if (IsDebug)
            {
                Console.WriteLine($"push: {chProp}, cout={PropStack.Count}");
            }

            return Array.Empty<CharInfo>();
        }

        private CharProp GetNewCharProp()
        {
            if (PropStack.Count > 0)
            {
                return PropStack.Peek();
            }

            return DefaultCharProp;
        }

        private IEnumerable<CharInfo> GetCurrentCharProp(IEnumerable<char> chars)
        {
            var curChProp = GetNewCharProp();
            if (IsDebug)
            {
                Console.WriteLine($"cur chars:{new string(chars.ToArray())}, use prop: {curChProp}");
            }
            return chars.Select(ch => new CharInfo(curChProp, ch));
        }

        private static bool CheckCharPropIsSameGroup(CharProp current, CharProp last)
        {
            // 需要绘制图像的字符单独成一组
            if (!string.IsNullOrWhiteSpace(current.img))
            {
                return false;
            }

            const float epsilon = 1e-5f;
            if (Math.Abs(current.scale - last.scale) > epsilon ||
                Math.Abs(current.rotate - last.rotate) > epsilon ||
                current.fontGroup != last.fontGroup)
            {
                return false;
            }

            return true;
        }

        private SpecialFontParserResult ParseCharInfoToResult(List<CharInfo> charInfos)
        {
            if (!_containsSpecial)
            {
                var sb = new StringBuilder();
                foreach (var charInfo in charInfos)
                {
                    sb.Append(charInfo.Char);
                }

                return new SpecialFontParserResult(true, sb.ToString(), false);
            }

            var groups = new List<Group<string, CharInfo>>();
            foreach (var charInfo in charInfos)
            {
                if (groups.Count == 0)
                {
                    var group = new Group<string, CharInfo>(charInfo.CharPropInfo.fontGroup);
                    groups.Add(group);
                    group.Elements.Add(charInfo);
                    continue;
                }

                var lastGroup = groups.Last();
                var lastCharInfo = lastGroup.First();
                if (!CheckCharPropIsSameGroup(charInfo.CharPropInfo, lastCharInfo.CharPropInfo))
                {
                    var group = new Group<string, CharInfo>(charInfo.CharPropInfo.fontGroup);
                    groups.Add(group);
                    lastGroup = group;
                }

                lastGroup.Elements.Add(charInfo);
            }

            var drawList = new List<DrawText>(groups.Count);
            foreach (var group in groups)
            {
                var curGroup = group.ToList();
                var sb = new StringBuilder();
                foreach (var charInfo in group)
                {
                    sb.Append(charInfo.Char);
                }

                var img = group.FirstOrDefault()?.CharPropInfo.img;
                var drawText = new DrawText(sb.ToString(), 
                    curGroup.Select(chInfo => chInfo.CharPropInfo.color).ToArray(), 
                        group.Key, 
                    string.IsNullOrWhiteSpace(img) ? null : GameFontReplace.FontConfig.GetImgTexture(img),
                    group.First().CharPropInfo.scale,
                    group.First().CharPropInfo.rotate
                    );
                if (IsDebug)
                {
                    Console.WriteLine("DrawText: " + drawText);
                }
                drawList.Add(drawText);
            }

            return new SpecialFontParserResult(true, drawList, _containsSpecial);
        }

        public SpecialFontParserResult ParseText(string text)
        {
            _containsSpecial = false;
            PropStack.Clear();
            if (IsDebug)
            {
                Console.WriteLine($@"开始解析：[{text}]");
            }

            try
            {
                var chInfo = TextParser.End().Parse(text).ToList();
                if (IsDebug)
                {
                    Console.WriteLine(@"======================== 解析结果 =========================");
                    foreach (var charInfo in chInfo)
                    {
                        Console.WriteLine($@"char:{charInfo.Char}, prop: {charInfo.CharPropInfo}");
                    }
                }
                
                return ParseCharInfoToResult(chInfo);
            }
            catch (Exception)
            {
                return new SpecialFontParserResult(false, text, false);
            }
        }

        private class Group<K, E> : IGrouping<K,E>
        {
            public List<E> Elements { get; } = new List<E>();

            public Group(K key)
            {
                this.Key = key;
            }

            public IEnumerator<E> GetEnumerator()
            {
                return Elements.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public K Key { get; }
        }
    }



    public class CharProp : ICloneable
    {
        public Color color { get; set; }

        public string fontGroup { get; set; }

        public float scale { get; set; } = 1f;

        public float rotate { get; set; } = 0f;

        // 非路径而是配置文件中定义的img的key,且该属性不可被继承
        public string img { get; set; }

        public object Clone()
        {
            return new CharProp() { color = this.color, fontGroup = this.fontGroup, scale = this.scale, rotate = this.rotate };
        }

        public void SetColor(string colorStr)
        {
            try
            {
                var property = typeof(Color).GetProperty(colorStr, BindingFlags.Static | BindingFlags.Public);
                if (property == null || property.PropertyType != typeof(Color))
                {
                    throw new ArgumentException($"Color has no property named {colorStr}");
                }

                this.color = (Color)property.GetValue(null);
            }
            catch (Exception)
            {
                this.color = (Color)XMLTypeConverter.ConvertToType(typeof(Color), colorStr);
            }
        }

        public void SetRotateByDeg(float degrees)
        {
            this.rotate = MathHelper.ToRadians(degrees);
        }

        public override string ToString()
        {
            return $"CharProp@{{ color={color}, fontGroup={fontGroup}, img={img}, scale={scale}, rotate={rotate} }}";
        }
    }

    public class CharInfo
    {
        public CharProp CharPropInfo { get; }

        public char Char { get; }

        public CharInfo(CharProp charProp, char ch)
        {
            this.CharPropInfo = charProp;
            this.Char = ch;
        }
    }

    public class DrawText
    {
        public string Text { get; }

        private readonly Color[] _sourceColors;
        public Color[] Colors { get; private set; }

        public string FontGroup { get; }

        public float Scale { get; }

        public float Rotate { get; }

        // 待绘制的图像的key,存在该属性时Text与Colors属性不再有效
        public Texture2D Img { get; set; }

        public DrawText(string text, Color[] colors, string fontGroup, Texture2D img, float scale, float rotate)
        {
            Text = text;
            _sourceColors = colors;
            Colors = colors;
            FontGroup = fontGroup;
            Scale = scale;
            Rotate = rotate;
            Img = img;
        }

        private void ChangeRenderColor(DynamicSpriteFont font, Vector2 position)
        {
            if (Img != null)
            {
                return;
            }
            var glyphs = font.GetGlyphs(Text, position);
            var targetColors = new List<Color>(_sourceColors.Length);
            for (var i = 0; i < glyphs.Count; i++)
            {
                var glyph = glyphs[i];
                if (glyph.Bounds.Width > 0 && glyph.Bounds.Height > 0)
                {
                    targetColors.Add(_sourceColors[i]);
                }
            }

            this.Colors = targetColors.ToArray();
        }

        public void ApplyFont(DynamicSpriteFont font, Vector2 position)
        {
            ChangeRenderColor(font, position);
        }

        public override string ToString()
        {
            return $"{nameof(DrawText)} => [{nameof(Text)}: {Text}, {nameof(FontGroup)}: {FontGroup}, {nameof(Img)}: {Img}, {nameof(Scale)}: {Scale}, {nameof(Rotate)}: {Rotate}]";
        }
    }

    public class SpecialFontParserResult
    {
        public bool IsSuccess { get; set; }

        public bool IsSpecial { get; set; }

        public string Text { get; set; }

        public List<DrawText> DrawTexts { get; set; } = new List<DrawText>();

        public DateTime AddToCacheTime { get; set; }

        public SpecialFontParserResult() { }

        public SpecialFontParserResult(bool isSuccess, List<DrawText> drawTexts, bool isSpecial)
        {
            IsSuccess = isSuccess;
            DrawTexts = drawTexts;
            IsSpecial = isSpecial;
        }

        public SpecialFontParserResult(bool isSuccess, string text, bool isSpecial)
        {
            IsSuccess = isSuccess;
            Text = text;
            IsSpecial = isSpecial;
        }
    }
}
