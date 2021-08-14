# LuxaMic
Windows 10 app to set [Luxafor RGB lights](https://luxafor.com/products/) according to whether your microphone is in use by other applications. Allows you to use your Luxafor lights as an 'on air' indicator to show when you are taking part in a web meeting and should not be disturbed, regardless of the meeting software you are using.

## Requirements

* Windows 10 is required.
* The built-in notification icon that indicates when the microphone is in use must be visible in the notification area (this is the default setting in Windows). If the user hides it in the notification overflow area, this app will not be able to detect when the microphone is in use.

## Development
LuxaMic is developed using [Visual Studio Community](https://visualstudio.microsoft.com/vs/community/).

This software uses functionality from the following libraries:
* [LuxaforSharp](https://github.com/Duncan-Idaho/LuxaforSharp) by [Edouard Paumier](https://github.com/Duncan-Idaho)
* [HidLibrary](https://github.com/mikeobrien/HidLibrary) by [Mike O'Brien](https://github.com/mikeobrien), [Austin Mullins](https://github.com/amullins83), and other contributors.
