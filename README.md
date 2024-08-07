# BepInEx Utility Plugins
Various universal BepInEx utility plugins for Unity games running on mono. If the game is compiled with IL2CPP, use [BepInEx.Utility.IL2CPP](https://github.com/BepInEx/BepInEx.Utility.IL2CPP) instead.

You need to have the latest version of [BepInEx 5.x](https://github.com/BepInEx/BepInEx) installed for the plugins to work.

#### How to use
- Install the latest verion [BepInEx 5.x](https://github.com/BepInEx/BepInEx).
- Download the [latest release](https://github.com/BepInEx/BepInEx.Utility/releases) of the plugin you want.
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

## CatchUnityEventExceptions
Makes sure all event handlers subscribed to commonly used UnityEngine events are executed, even if some of them crash.
#### Explanation
If any event handler that has been subscribed to UnityEngine events like "SceneManager.sceneLoaded" crashes, no other event handlers will be executed after this one. This can cause very hard to debug bugs, for example: Plugin A's handler crashes, which causes Plugin B's handler to not run. B's handler was supposed to initialize some fields before other code runs, but it could not do it, so now the code that expected these fields to be initialized will behave in an unexpected way.

## SuppressGetTypesErrorsPatcher
A patcher that hooks Assembly.GetTypes() and handles ReflectionTypeLoadException. Useful when game code is using Assembly.GetTypes() without handling the exception, and it crashes on plugin assemblies that have types that can't be loaded.

## OptimizeIMGUI
Reduce unnecessary GC allocations of Unity's IMGUI (OnGUI) interface system. It fixes the passive GC allocations that happen every frame caused by using any OnGUI code at all, and reduces GC allocations for OnGUI code. 

**Warning:** This might cause some GUILayout elements to be drawn differently, especially when using heavily layered GUILayoyut.ExpandHeight / GUILayout.ExpandWidth ([more info](https://github.com/BepInEx/BepInEx.Utility/issues/6)).

## ResourceUnloadOptimizations
Improves loading times and reduces or eliminates stutter in games that abuse Resources.UnloadUnusedAssets and/or GC.Collect.

## IMGUITextCursorFix
Fixes a bug that in some Unity versions prevents the IMGUI text editor from keeping the cursor visible in overflowing text.
