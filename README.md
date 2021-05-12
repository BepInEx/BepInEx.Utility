# BepInEx Utility Plugins
Various BepInEx utility plugins for Unity games.

You need to have at least [BepInEx 5.1](https://github.com/BepInEx/BepInEx) installed for the plugins to work.

#### How to use
- Install [BepInEx 5.1](https://github.com/BepInEx/BepInEx) or higher.
- Download [latest release](https://github.com/BepInEx/BepInEx.Utility/releases) of the plugin.
- Place the .dll inside your BepInEx\Plugins folder.

## EnableFullScreenToggle
Enables toggling full screen with alt+enter on games with it disabled.

## EnableResize
Enable window resizing. Must be enabled in the plugin config either by editing the plugin's .cfg file or by using [ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager)

## InputHotkeyBlock
Prevents plugin hotkeys from triggering while typing in an input field.

## MessageCenter
A simple plugin that shows any log entries marked as "Message" on screen. Plugins generally use the "Message" log level for things that they want the user to read.

#### How to make my mod compatible?
Use the `Logger` of your plugin and call its `LogMessage` method or `Log` method and pass in `LogLevel.Message` as a parameter. You don't have to reference this plugin, and everything will work fine if this plugin doesn't exist.

Please avoid abusing the messages! Only show short and clear messages that the user is likely to understand and find useful. Avoid showing many messages in a short succession.

## MuteInBackground
Adds an option to mute a game when it loses focus, i.e. when alt-tabbed. Must be enabled in the plugin config either by editing the plugin's .cfg file or by using [ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager)

## SuppressGetTypesErrorsPatcher
A patcher that hooks Assembly.GetTypes() and handles ReflectionTypeLoadException. Useful when game code is using Assembly.GetTypes() without handling the exception, and it crashes on plugin assemblies that have types that can't be loaded.

## OptimizeIMGUI
Reduce unnecessary GC allocations of Unity's IMGUI (OnGUI) interface system. It fixes the passive GC allocations that happen every frame caused by using any OnGUI code at all, and reduces GC allocations for OnGUI code. 
