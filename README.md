# Luxafor On Air
Windows app to set [Luxafor RGB lights](https://luxafor.com/products/) according to whether your microphone is in use by other applications. Allows you to use your Luxafor lights as an 'on air' indicator to show when you are taking part in a web meeting and should not be disturbed, regardless of the meeting software you are using.

## Quick Start

* Check the Requirements below.
* Download the latest release from the [releases page](https://github.com/jschlackman/LuxaforOnAir/releases) and install.
* Run **Luxafor On Air** from the new Start Menu shortcut.

## Requirements

* **Windows 10 version 1903 (May 2019 Update)** or later is required (or any version of Windows 11). Earlier versions of Windows do not have the 'microphone in use' notification feature that this app relies on.
* **[.NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/thank-you/net472-web-installer)** runtime (or later) is required, but this is installed by default with all versions of Windows 10 and 11 currently supported by Microsoft.
* The official [Luxafor software](https://luxafor.com/download/) is **not** required, but if it is installed it should not be run at the same time as Luxafor On Air. The official software will override color changes made by this app.

## Development
Luxafor On Air is developed using [Visual Studio Community](https://visualstudio.microsoft.com/vs/community/). The installer is authored using the freeware edition of [Advanced Installer](https://www.advancedinstaller.com/).

This software uses functionality from the following libraries:
* [LuxaforSharp](https://github.com/Duncan-Idaho/LuxaforSharp) by [Edouard Paumier](https://github.com/Duncan-Idaho)
* [HidLibrary](https://github.com/mikeobrien/HidLibrary) by [Mike O'Brien](https://github.com/mikeobrien), [Austin Mullins](https://github.com/amullins83), and other contributors.
