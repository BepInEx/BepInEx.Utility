# BepInEx Utility Plugins
Various BepInEx utility plugins for Unity games.

You need to have at least [BepInEx 5.x](https://github.com/BepInEx/BepInEx) installed for the plugins to work.

#### How to use
- Install [BepInEx 5.x](https://github.com/BepInEx/BepInEx).
- Download [latest release](https://github.com/BepInEx/BepInEx.Utility/releases) of the plugin.
- Place the .dll inside your BepInEx\Plugins folder.

## MessageCenter
A simple plugin that shows any log entries marked as "Message" on screen. Plugins generally use the "Message" log level for things that they want the user to read.

#### How to make my mod compatible?
Use the `Logger` of your plugin and call its `LogMessage` method or `Log` method and pass in `LogLevel.Message` as a parameter. You don't have to reference this plugin, and everything will work fine if this plugin doesn't exist.

Please avoid abusing the messages! Only show short and clear messages that the user is likely to understand and find useful. Avoid showing many messages in a short succession.

## MuteInBackground
Adds an option to mute a game when it loses focus, i.e. when alt-tabbed. Must be enabled in the plugin config either by editing the plugin's .cfg file or by using [ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager)
