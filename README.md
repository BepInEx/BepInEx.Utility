## On-screen Message Display plugin for BepInEx 5
A simple plugin that shows any log entries marked as "Message" on screen. Plugins generally use the "Message" log level for things that they want the user to read.

## How to use
- Install a build of BepInEx 5 from at least 26/09/2019 (older won't work).
- Download latest release from the Releases tab above.
- Place the .dll inside your BepInEx\Plugins folder.

## How to make my mod compatible?
Use the `Logger` of your plugin and call its `LogMessage` method or `Log` method and pass in `LogLevel.Message` as a parameter. You don't have to reference this plugin, and everything will work fine if this plugin doesn't exist.

Please avoid abusing the messages! Only show short and clear messages that the user is likely to understand and find useful. Avoid showing many messages in a short succession.
