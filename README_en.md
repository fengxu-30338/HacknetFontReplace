# HacknetFontReplace
Hacknet Mod Font Switching Support

[Chinese Version](README.md)



## Prerequisites

You need to install [Pathfinder](https://github.com/Arkhist/Hacknet-Pathfinder) before using this mod



## Usage

Extract the Release package and copy all files to the `Extension Root Directory/Plugins` directory



## Configuration File

The configuration file is located at `Extension Root Directory/Plugins/Font/HacknetFontReplace.config.xml`

Place all font files needed for the project in the `Extension Root Directory/Plugins/Font` directory

```xml
<?xml version="1.0" encoding="utf-8"?>
<HacknetFontReplace>
	<!--Large font; such as "Connected to xxx" text in Display panel-->
	<LargeFontSize>34</LargeFontSize>
	<!--Small font; such as "You are the system administrator" text-->
	<SmallFontSize>20</SmallFontSize>
	<!--UI font size-->
	<UIFontSize>18</UIFontSize>
	<!--Font size for top-left RAM module, AppBar, etc.-->
	<DetailFontSize>14</DetailFontSize>
	<!--Incremental change when modifying font size settings-->
	<ChangeFontSizeInterval>2</ChangeFontSizeInterval>
	<!--Whether to enable multi-color font parsing-->
	<OpenMultiColorFontParse>false</OpenMultiColorFontParse>
	<!--Define font groups-->
	<FontGroups>
		<!--Font paths defined first are loaded first-->
		<FontGroup Name="default">
			<FontPath>Plugins/Font/HarmonyOS_SansSC_Regular.ttf</FontPath>
		</FontGroup>

		<FontGroup Name="desc">
			<FontPath>Plugins/Font/SegoeKeycaps.ttf</FontPath>
			<FontPath>Plugins/Font/HarmonyOS_SansSC_Regular.ttf</FontPath>
		</FontGroup>
	</FontGroups>
	<!--
		Currently active font group
		You can switch the active font group through Action in the extension: <ChangeActiveFontGroup Name="desc" />
		After activation, it remains valid after saving the game and re-entering
	-->
	<ActiveFontGroup>default</ActiveFontGroup>
</HacknetFontReplace>
```





## Multi-color Fonts

You can use the following effects to achieve colored fonts

```tex
涉嫌摇{color: Red}篮这是我们最后一次合作了{/}，{color: Blue}喝杯{color: 0 241 162}咖啡{/}提提神吧。{/}
{color: Green}现在想起来，我们之前干的都让我有点{color: Red}提心吊胆。{/}{/}
准备好了就发{color: Yellow}邮件{/}给我。
```

The effect is as follows:

![](img/font.jpg)

> **Usage Rules**

1. First, you need to enable multi-color font support in the configuration file: OpenMultiColorFontParse=true
2. You need to wrap the text to be rendered inside {}{/} tags, just like XML tags
3. A pair of {}{/} tags cannot span multiple lines, otherwise it will be invalid (`due to game reasons, see the notes below`)
4. Currently, only the `color` attribute is supported inside tags. There are two ways to write attribute values:
   - Write the color name directly, the color must exist and the first letter must be capitalized, such as: Red, Green, etc.
   - Write in RGB or RGBA format, separated by spaces (**commas and other separators are not allowed**)

5. Tags can also be written in all files that define text content, such as email definitions in XML or in code

6. Tags can be nested. Text wrapped by inner tags automatically inherits the color of the outer text, and you can also set the color of the inner text separately

**Note**:

Some parts of Hacknet automatically split text into multiple lines. For example, if you define the text in an email as `{}准备好了就发给我。{/}`, the game may split it into two lines as follows:

`{}准备好了就发`

`给我。{/}`

At this time, it will be rendered twice, causing the tag parsing to fail. You need to correct the above text to the following parts to render normally:

`{}准备好了就发{}`

`{}给我。{/}`



## Action

Added the ChangeActiveFontGroup tag, which allows you to dynamically switch tags during the game, for example:

```xml
<Instantly Delay="5">
    <ChangeActiveFontGroup Name="desc" />
</Instantly>
```



## Editor Hints

For your translation experience, I recommend using the Visual Studio Code editor, as it supports syntax highlighting and intelligent hints for XML files.

You can install the following plugins in Visual Studio Code to get a better translation experience:

- XML Tools: Provides syntax highlighting and intelligent hints for XML files
- [HacknetExtensionHelper](https://marketplace.visualstudio.com/items?itemName=fengxu30338.hacknetextensionhelper): Provides intelligent hints related to Hacknet extensions

If the installed HacknetExtensionHelper plugin version is greater than or equal to `0.3.3`, you can reference this Mod's [hint file](.EditorHints/HacknetFontReplace.xml) through the `Include` tag in the Hacknet-EditorHint.xml file in the extension root directory

```xml
<!-- Hacknet-EditorHint.xml in the extension root directory -->
<HacknetEditorHint>
    <Include path=".EditorHints/HacknetFontReplace.xml" />
</HacknetEditorHint>
```





## About

If you use this mod, please indicate the source in the mod description.
