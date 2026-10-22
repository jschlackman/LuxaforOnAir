# Luxafor On Air

Windows app to set [Luxafor RGB lights](https://luxafor.com/products/) according to whether your microphone is in use by other applications. Allows you to use your Luxafor lights as an 'on air' indicator to show when you are taking part in a web meeting and should not be disturbed, regardless of the meeting software you are using.

## Quick Start

* Check the Requirements below.
* Download the latest release from the [releases page](https://github.com/jschlackman/LuxaforOnAir/releases) and install.
* Run **Luxafor On Air** from the new Start Menu shortcut.

## Requirements

* **Windows 10 version 1903 (May 2019 Update)** or later is required (or any version of Windows 11). Earlier versions of Windows do not have the 'microphone in use' notification feature that this app relies on.
* **[.NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/thank-you/net472-web-installer)** runtime (or later) is required, but this is installed by default with all versions of Windows currently supported by Microsoft.
* The official [Luxafor software](https://luxafor.com/download/) is **not** required, but if it is installed it should not be run at the same time as Luxafor On Air. The official software will override color changes made by this app.

## Development & Contributing

Luxafor On Air is developed using [Visual Studio Community](https://visualstudio.microsoft.com/vs/community/). The installer is authored using the freeware edition of [Advanced Installer](https://www.advancedinstaller.com/).

When contributing code, you are giving permission to use it as-is and you must make sure you are allowed to distribute it. By submitting a change, you agree to put your changes and new code under the same license as the rest of the project. Authors should be explicitly aware that the burden is on them to ensure no code is submitted to the project that infringes on other licensed work.

**LLM (AI) usage is permitted for contributions,** with the following conditions:

* Pull requests must be made by a human; requests made independently by agents will be closed without being merged.

* You should mark any LLM co-authored work as such. Do not attempt to pass off such contributions as wholly your own work.

* LLM-generated changes must be reviewed by a human the same as you would review the work of a junior developer working under you.

* Your LLM should be used as an assistant to your work that you use to offload select tasks; you should avoid contributing work that was substantially outsourced to an LLM as a substitute for your own knowledge or expertise.

* **In short: if you don't fully understand the code you are contributing, please do not contribute.**

* Some flexibility may be applied to the above conditions depending on the size and nature of the change. For example, a small bugfix may be accepted even if it is almost entirely LLM-generated, while a new feature or redesign should be primarily human-authored.

## Acknowledgements

This software uses functionality from the following libraries:

* [LuxaforSharp](https://github.com/Duncan-Idaho/LuxaforSharp) by [Edouard Paumier](https://github.com/Duncan-Idaho)
* [HidLibrary](https://github.com/mikeobrien/HidLibrary) by [Mike O'Brien](https://github.com/mikeobrien), [Austin Mullins](https://github.com/amullins83), and other contributors.
