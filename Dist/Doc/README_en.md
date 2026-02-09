# HacknetFontReplace
Font switching support for Hacknet mods

[简体中文](README.md)


## Prerequisites

You need to install [Pathfinder](https://github.com/Arkhist/Hacknet-Pathfinder) before using this mod


## Usage

Extract the Release package and copy all files to the `ExtensionRoot/Plugins` directory


## Configuration File

The configuration file is located at `ExtensionRoot/Plugins/Font/HacknetFontReplace.config.xml`

Place all font files needed by the project in the: `ExtensionRoot/Plugins/Font` directory

```xml
<?xml version="1.0" encoding="utf-8"?>
<HacknetFontReplace>
	<!-- Large font size; e.g., "Connect to xxx" text in Display panel -->
	<LargeFontSize>34</LargeFontSize>
	<!-- Small font size; e.g., "You are the system administrator" text -->
	<SmallFontSize>20</SmallFontSize>
	<!-- UI font size -->
	<UIFontSize>18</UIFontSize>
	<!-- Font size for RAM module, AppBar, etc. in the top-left corner -->
	<DetailFontSize>14</DetailFontSize>
	<!-- Incremental change when modifying font size settings -->
	<ChangeFontSizeInterval>2</ChangeFontSizeInterval>
	<!-- Whether to enable special font parsing -->
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

### Colored Fonts

You can achieve colored text effects using the following syntax:

```tex
涉嫌摇{color: Red}篮这是我们最后一次合作了{/}，{color: Blue}喝杯{color: 0 241 162}咖啡{/}提提神吧。{/}
{color: Green}现在想起来，我们之前干的都让我有点{color: Red}提心吊胆。{/}{/}
准备好了就发{color: Yellow}邮件{/}给我。
```

The effect is as follows:

![](img/font.jpg)

> **Usage Rules**

1. First, you need to enable multicolor font support in the configuration file!!!
2. Wrap the text you want to render inside {}{/} tags like XML tags
3. A pair of {}{/} tags cannot span multiple lines, otherwise it will be invalid (see the notes below for the game-specific reason)
4. Currently, only the `color` attribute is supported inside the tags. There are two ways to write the attribute value:
   - Directly write the color name, which must exist and have the first letter capitalized, e.g., Red, Green, etc.
   - Write in rgb or rgba format, separated by spaces (**commas or other separators are not allowed**)
5. Tags can also be written in all files that define text content, such as email definitions in XML or in code
6. Tags can be nested. Text wrapped in inner tags automatically inherits the color of the outer text, and you can also set the color of inner text separately


### Local Font Groups

`You need to enable special font parsing in the configuration file`

You can achieve local display of different fonts using the following syntax:

```tex
123邮件内容邮件内容{color: Red, fontGroup: desc}aa11 223{/}邮件内容邮件内容123
邮件内容邮件内容邮件内容邮件内容邮件{fontGroup: desc}665 14{/}内容邮件内容邮件内容
邮件内容邮件内容邮件内容邮件内容邮件内容邮件内容邮件内容
{color: Red, fontGroup: desc}邮件内容邮件内容邮件内容邮件内容邮件内容邮件内容{color: Blue}邮件  23a   内容{/}你好{/}
```

The effect is as follows:

![](img/fontGroup.png)

The usage is similar to colored fonts. Define the font group name to display in {fontGroup: name}.

The group name is defined in the `FontGroup` tag in the [configuration file](#configuration-file).

```xml
<?xml version="1.0" encoding="utf-8"?>
<HacknetFontReplace>
	<!-- ...omitted configuration... -->
    <!-- Whether to enable special font parsing -->
	<OpenMultiColorFontParse>true</OpenMultiColorFontParse>
    
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

Note that the format must strictly follow the above writing. Do not add double quotes arbitrarily. **This is not JSON format**


### Embedded Images

`You need to enable special font parsing in the configuration file`

You can achieve image embedding effects using the following syntax:

```tex
图片：{img: test, scale: 0.5}${/}
```

The effect is as follows:

![](./img/img.png)


The usage is similar to colored fonts. Define the image key to display in {img: key}. Each character inside {}{/} will be replaced with the image.

The key is defined in the `Image` tag in the [configuration file](#configuration-file).

```xml
<?xml version="1.0" encoding="utf-8"?>
<HacknetFontReplace>
	<!-- ...omitted configuration... -->
    <!-- Whether to enable special font parsing -->
	<OpenMultiColorFontParse>true</OpenMultiColorFontParse>
    
    <!-- Define image group -->
	<Images>
		<!-- Define image key, which is the value of img in the special font expression. The image path is a relative path based on the extension root directory -->
		<!-- <Image Key="test">Image/test.png</Image> -->
	</Images>
</HacknetFontReplace>
```


### Rotation and Scaling of Fonts/Images

`You need to enable special font parsing in the configuration file`

You can achieve image embedding effects with rotation and scaling using the following syntax:

```tex
图片：{img: test, scale: 1.5, rotate: 15}${/}
```

- img: Image key, defined in the configuration file
- scale: Scaling ratio. Excessive scaling may cause the game to calculate widths beyond limits, leading to line breaks that truncate special character syntax or other bugs. Please be cautious!
- rotate: Rotation angle. It will automatically ensure that the rotated content does not occupy space from the previous line


The effect is as follows:

![](./img/style.png)


**Note:** If you scale too much, it may cause the game to calculate font widths beyond limits, resulting in truncated font parsing. If you encounter unexpected line breaks, you should consider this possibility.

You need to enable special font parsing in the configuration file for this to take effect:

```xml
<?xml version="1.0" encoding="utf-8"?>
<HacknetFontReplace>
	<!-- ...omitted configuration... -->
    <!-- Whether to enable special font parsing -->
	<OpenMultiColorFontParse>true</OpenMultiColorFontParse>
</HacknetFontReplace>
```



### **Important Notes**

In some parts of Hacknet, text is automatically split into multiple lines. For example, if you define text in an email as `{}准备好了就发给我。{/}`, the game may split it into two lines as follows:

`{}准备好了就发`

`给我。{/}`

In this case, the tags will be parsed in two separate rendering passes, causing the parsing to fail. You need to modify the text as follows to ensure normal rendering:

`{}准备好了就发{}`

`{}给我。{/}`


## Action

Added the ChangeActiveFontGroup tag, which allows dynamic switching of font groups during gameplay. For example:

```xml
<Instantly Delay="5">
    <ChangeActiveFontGroup Name="desc" />
</Instantly>
```


## Editor Tips

For a better user experience, I recommend using Visual Studio Code editor, as it supports syntax highlighting and intelligent prompts for XML files.

You can install the following plugins in Visual Studio Code to get a better translation experience:

- XML Tools: Provides syntax highlighting and intelligent prompts for XML files
- [HacknetExtensionHelper](https://marketplace.visualstudio.com/items?itemName=fengxu30338.hacknetextensionhelper): Provides intelligent prompts related to Hacknet extensions

If you have installed HacknetExtensionHelper plugin version >= `0.3.3`, you can reference this mod's [hint file](.EditorHints/HacknetFontReplace.xml) via the `Include` tag in the `Hacknet-EditorHint.xml` file in the extension root directory

```xml
<!-- Hacknet-EditorHint.xml in the extension root directory -->
<HacknetEditorHint>
    <Include path=".EditorHints/HacknetFontReplace.xml" />
</HacknetEditorHint>
```



## About

If you use this mod, please indicate the source in your mod description.