# HacknetFontReplace
Hacknet Mod Font Switch Support

[Chinese Version](README.md)


## Prerequisites

You need to install [Pathfinder](https://github.com/Arkhist/Hacknet-Pathfinder) before using this mod


## Usage

Extract the Release package and copy all files to the `Extension Root/Plugins` directory


## Configuration File

The configuration file is located at `Extension Root/Plugins/Font/HacknetFontReplace.config.xml`

Place all font files needed by the project in the directory: `Extension Root/Plugins/Font`

```xml
<?xml version="1.0" encoding="utf-8"?>
<HacknetFontReplace>
	<!-- Large font size; such as "Connect to xxx" text in the Display panel -->
	<LargeFontSize>34</LargeFontSize>
	<!-- Small font size; such as "You are the system administrator" text -->
	<SmallFontSize>20</SmallFontSize>
	<!-- UI font size -->
	<UIFontSize>18</UIFontSize>
	<!-- Font size for Ram module in top-left corner, AppBar, etc. -->
	<DetailFontSize>14</DetailFontSize>
	<!-- Incremental change when modifying font size settings -->
	<ChangeFontSizeInterval>2</ChangeFontSizeInterval>
	<!-- Whether to enable multi-color font parsing -->
	<OpenMultiColorFontParse>false</OpenMultiColorFontParse>
	<!-- Define font groups -->
	<FontGroups>
		<!-- Font paths defined first are loaded with higher priority -->
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
		You can switch the active font group via Action in the extension: <ChangeActiveFontGroup Name="desc" />
		The setting will persist after saving and reloading the game
	-->
	<ActiveFontGroup>default</ActiveFontGroup>
</HacknetFontReplace>
```



## Special Font Features

### Multi-color Fonts

You can achieve multi-color text using the following syntax:

```tex
涉嫌摇{color: Red}篮这是我们最后一次合作了{/}，{color: Blue}喝杯{color: 0 241 162}咖啡{/}提提神吧。{/}
{color: Green}现在想起来，我们之前干的都让我有点{color: Red}提心吊胆。{/}{/}
准备好了就发{color: Yellow}邮件{/}给我。
```

The effect is as follows:

![](img/font.jpg)

> **Usage Rules**

1. First, you need to enable multi-color font support in the configuration file: OpenMultiColorFontParse=true
2. Wrap the text you want to render in {}{/} tags like XML tags
3. A pair of {}{/} tags cannot span multiple lines, otherwise it will be invalid (see the notes below for game-specific reasons)
4. Currently, only the `color` attribute is supported inside the tags, with two ways to write the attribute value:
   - Directly write the color name, which must exist and start with a capital letter, such as: Red, Green, etc.
   - Write in rgb or rgba format, separated by spaces (**commas or other separators are not allowed**)
5. Tags can also be written in all files that define text content, such as email definitions in XML or in code
6. Tags can be nested, and the text wrapped by inner tags will automatically inherit the color of the outer text, while you can also set the color of the inner text separately


### Local Font Groups

You can achieve local display of different fonts using the following syntax:

```tex
123邮件内容邮件内容{color: Red, fontGroup: desc}aa11 223{/}邮件内容邮件内容123
邮件内容邮件内容邮件内容邮件内容邮件{fontGroup: desc}665 14{/}内容邮件内容邮件内容
邮件内容邮件内容邮件内容邮件内容邮件内容邮件内容邮件内容
{color: Red, fontGroup: desc}邮件内容邮件内容邮件内容邮件内容邮件内容邮件内容{color: Blue}邮件  23a   内容{/}你好{/}
```

The effect is as follows:

![](img/fontGroup.png)

The usage is similar to multi-color fonts. Define the font group name you want to display in {fontGroup: name}.

The group name is defined in the `FontGroup` tag in the [Configuration File](#configuration-file).

```xml
<?xml version="1.0" encoding="utf-8"?>
<HacknetFontReplace>
	<!-- ...omitted some configurations... -->
    
	<!-- Define font groups -->
	<FontGroups>
		<!-- Font paths defined first are loaded with higher priority -->
		<FontGroup Name="default">
			<FontPath>Plugins/Font/HarmonyOS_SansSC_Regular.ttf</FontPath>
		</FontGroup>

		<FontGroup Name="desc">
			<FontPath>Plugins/Font/SegoeKeycaps.ttf</FontPath>
			<FontPath>Plugins/Font/HarmonyOS_SansSC_Regular.ttf</FontPath>
		</FontGroup>
	</FontGroups>
</HacknetFontReplace>
```

Note that the format must strictly follow the above writing, do not add double quotes arbitrarily, **it is not JSON format**


### **Notes**

In some parts of Hacknet, text is automatically split into multiple lines. For example, if you define text in an email as `{}准备好了就发给我。{/}`, it may be split into two lines in the game as follows:

`{}准备好了就发`

`给我。{/}`

In this case, it will be rendered twice, causing the tag parsing to fail. You need to modify the above text to the following parts to render normally:

`{}准备好了就发{}`

`{}给我。{/}`


## Action

Added the ChangeActiveFontGroup tag, which allows dynamic switching of font groups during gameplay, for example:

```xml
<Instantly Delay="5">
    <ChangeActiveFontGroup Name="desc" />
</Instantly>
```


## Editor Tips

For better user experience, I recommend using Visual Studio Code editor, as it supports syntax highlighting and intelligent prompts for XML files.

You can install the following plugins in Visual Studio Code for a better translation experience:

- XML Tools: Provides syntax highlighting and intelligent prompts for XML files
- [HacknetExtensionHelper](https://marketplace.visualstudio.com/items?itemName=fengxu30338.hacknetextensionhelper): Provides intelligent prompts related to Hacknet extensions

If the version of the HacknetExtensionHelper plugin you installed is greater than or equal to `0.3.3`, you can reference this Mod's [hint file](.EditorHints/HacknetFontReplace.xml) in the Hacknet-EditorHint.xml file in the extension root directory using the `Include` tag

```xml
<!-- Hacknet-EditorHint.xml in the extension root directory -->
<HacknetEditorHint>
    <Include path=".EditorHints/HacknetFontReplace.xml" />
</HacknetEditorHint>
```



## About

If you use this mod, please indicate the source in your mod description.