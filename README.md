# bindED

This VoiceAttack plugin reads keybindings in Elite:Dangerous and stores them as
VoiceAttack variables. It has originally been written by Gary (the developer of
VoiceAttack) [and published on the VoiceAttack
forums](https://forum.voiceattack.com/smf/index.php?topic=564.0).

You can find the [original README here](ReadMe.txt).

I have taken the original source code and added automatic detection of the
correct bindings file and support for non-US keyboard layouts (see below for
details).

## Installing

Grab `bindEDplugin.zip` from the [release
page](https://github.com/alterNERDtive/bindED/releases/latest) and extract it
into your VoiceAttack�s `Apps` directory.

## Migrating from the Old Plugin

If you use this as a drop-in replacement for the initial version all commands
invoking the plugin will throw an error message. Gary has asked me to change the
plugin�s GUID, and the plugin with the old one will no longer be found.

_That is irrelevant in basically all cases and can safely be ignored_. Binds
will be read automatically when VoiceAttack starts, and when they change.

## Usage

Before starting VoiceAttack with the plugin installed, make sure that you have
loaded the game at least once! That will create the directory structure the
plugin is going to read from. You also need to have changed _any_ key in
controls options. You will probably have made some changes anyway.

For Horizons players, that�s it! When VoiceAttack loads, bindED will
automatically detect your bindings. It will also keep a watchful eye on Elite�s
bindings folder and reload them when there is a change!

For Odyssey players, there is an additional caveat: you have to use the same
preset for all 4 sections (general, ship, SRV, foot). Sadly it�s not apparent
from the files which binds belong to which.

If something goes awry, you can still manually call the `loadbinds` plugin
context to force a refresh.

If you are not using a US QWERTY keyboard layout, see below.

### Horizons vs. Odyssey

**Note**: If you do not own Odyssey, everything will work just as before!

Sadly for the time being Odyssey and Horizons will basically be separate games.
That also means they have separate binds files.

BindED will by default always use the last edited file, be that the base preset,
Horizons or Odyssey.

To keep hassle to a minimum, the recommended way to change binds is to do it
from Odyssey. When a change to the Odyssey file is detected, the plugin will
by default overwrite Horizons� binds with it. To prevent that and keep entirely
separate binds, you can set `bindED.disableHorizonsSync#` (yes, including the
pound sign) to `true` in your VoiceAttack profile.

### Specifying a Binds File to Load

This should generally not be necessary!

You can set the text variable `~bindsFile` to a specific file name (e.g.
`custom.3.0.binds`) before executing the `loadbinds` context to have that
specific binds file loaded.

Make sure to only use the _file name_ of an existing binds file, do _not_
specify the full path.

This should be a last resort effort for when the game introduces changes that
break the plugin�s auto detection.

## Support for non-US Keyboard Layouts

Shipped layouts:
* en-US
* en-GB
* de-neo2

If you are using any non-US layout you might have noticed that some binds don�t
work. Elite internally uses keycode values (a number assigned to each key on the
keyboard) for its bindings but for some reason both displays and saves them as
keysyms (the label on the key), according to the UK QWERTY keyboard layout. That
means VoiceAttack can�t just send the keysym it reads from a binding, it has to
translate it into the corresponding keycode.

The original plugin contained a `EDMap.txt` file that contains information on
that conversion _for the US keyboard layout_. If you are using any other layout
that information will be incorrect for any symbols that are on a different key
than they are on the US layout.

I have added the option to use maps for other keyboard layouts. In order to do
so you will have to set a text variable in VoiceAttack called `bindED.layout#`
to the layout you want to use. BindED will be notified of the variable changing
and reload your bindings with the appropriate key map. If the variable is not
set it will defaut to �en-us�, leaving the original behaviour intact.

I have included a map file for [Neo2](https://neo-layout.org)
(`EDMap-de-neo2.txt`) which is the layout that I am using personally. If you are
on a different layout, you will have to create a corresponding map file yourself
or prod me to add it. E.g. for the french AZERTY it would be `EDMap-fr-fr.text`
and set `bindED.layout#` to �fr-fr�. For US Dvorak, `EDmap-en-us-dvorak` and
�en-us-dvorak�. You can see where this is going.

For more information on [creating new supported keyboard layouts see the
Wiki](https://github.com/alterNERDtive/bindED/wiki/Keyboard-Layouts).

## Troubleshooting

If you run into any kinds of trouble with missing bindings the first step should
be to import and load the included `bindED-reports` profile. It will generate
both a list of bind names used by Elite and a report of binds that do not have a
keyboard shortcut assigned, and put them on your Desktop.

Need help beyond that? Please [file an
issue](https://github.com/alterNERDtive/bindED/issues/new) or [hop into
Discord](https://discord.gg/YeXh2s5UC6).