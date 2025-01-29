using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: System.Windows.ThemeInfo(System.Windows.ResourceDictionaryLocation.None, System.Windows.ResourceDictionaryLocation.SourceAssembly)]

// [MANDATORY] The following GUID is used as a unique identifier of the plugin. Generate a fresh one for your plugin!
[assembly: Guid("00eec1ff-31fd-47b4-bbff-1a71b63b0330")]

// [MANDATORY] The assembly versioning
//Should be incremented for each new release build of a plugin
[assembly: AssemblyVersion("1.0.1.0")]
[assembly: AssemblyFileVersion("1.0.1.0")]

// [MANDATORY] The name of your plugin
[assembly: AssemblyTitle("Alpaca")]
// [MANDATORY] A short description of your plugin
[assembly: AssemblyDescription("A plugin to host all N.I.N.A. devices as Alpaca Devices to be accessed from other applications")]

// The following attributes are not required for the plugin per se, but are required by the official manifest meta data

// Your name
[assembly: AssemblyCompany("Stefan Berg @isbeorn")]
// The product name that this plugin is part of
[assembly: AssemblyProduct("Alpaca")]
[assembly: AssemblyCopyright("Copyright © 2025 Stefan Berg @isbeorn")]

// The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.2.0.1001")]

// The license your plugin code is using
[assembly: AssemblyMetadata("License", "MPL-2.0")]
// The url to the license
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
// The repository where your pluggin is hosted
[assembly: AssemblyMetadata("Repository", "https://bitbucket.org/Isbeorn/nina.plugin.alpaca/")]

// The following attributes are optional for the official manifest meta data

//[Optional] Your plugin homepage URL - omit if not applicaple
[assembly: AssemblyMetadata("Homepage", "https://www.patreon.com/stefanberg")]

//[Optional] Common tags that quickly describe your plugin
[assembly: AssemblyMetadata("Tags", "Alpaca")]

//[Optional] A link that will show a log of all changes in between your plugin's versions
[assembly: AssemblyMetadata("ChangelogURL", "https://bitbucket.org/Isbeorn/nina.plugin.alpaca/src/master/CHANGELOG.md")]

//[Optional] The url to a featured logo that will be displayed in the plugin list next to the name
[assembly: AssemblyMetadata("FeaturedImageURL", "https://bitbucket.org/Isbeorn/nina.plugin.alpaca/downloads/alpaca.png")]
//[Optional] A url to an example screenshot of your plugin in action
[assembly: AssemblyMetadata("ScreenshotURL", "")]
//[Optional] An additional url to an example example screenshot of your plugin in action
[assembly: AssemblyMetadata("AltScreenshotURL", "")]
//[Optional] An in-depth description of your plugin
[assembly: AssemblyMetadata("LongDescription", @"
This plugin exposes all N.I.N.A. devices as Alpaca devices over the network, enabling control of your equipment from any ASCOM/Alpaca-compliant software or client. Each device is tightly integrated with N.I.N.A.'s interface abstractions, ensuring seamless operation and access to N.I.N.A.'s advanced features.

Additionally, exposing devices via Alpaca allows native or in-process drivers in N.I.N.A. to be shared with other applications.

Tip: For applications that cannot natively connect to Alpaca devices, you can use the ASCOM Chooser to make Alpaca drivers accessible as ASCOM drivers. See [Alpaca through the Chooser](https://ascom-standards.org/Help/Platform/html/e3870a2f-582a-4ab4-b37f-e9b1c37a2030.htm) in the ASCOM documentation for setup instructions.

")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
// [Unused]
[assembly: AssemblyConfiguration("")]
// [Unused]
[assembly: AssemblyTrademark("")]
// [Unused]
[assembly: AssemblyCulture("")]