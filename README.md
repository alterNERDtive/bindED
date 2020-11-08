# bindED

This VoiceAttack plugin reads keybindings in Elite:Dangerous and stores them as
VoiceAttack variables. It has originally been written by Gary (the developer of
VoiceAttack) [and published on the VoiceAttack
forums](https://forum.voiceattack.com/smf/index.php?topic=564.0).

You can find the [original README here](ReadMe.txt).

I have taken the original source code and added automatic detection of the
correct bindings file and support for non-US keyboard layouts (see below for
details).

## Usage

### Reading Bindings into VoiceAttack

Invoke the `loadbinds` plugin context. That will populate a bunch of variables
named `ed<name in the bindings file>` for you that you can use in commands
instead of hard-wiring key presses, enabling you to share profiles with other
players more easily.

### Saving the List of Variables

Invoke the `listbinds` plugin context (make sure to tick “Wait for the plugin
function to finish before continuing”!). That will set a variable
`~bindED.bindsList` containing all the variable names that you can e.g. write to
a file.

By default the list is separated by `\r\n`, you can override that behaviour by
setting `bindED.separator` before invoking the plugin.

## Automatic Bindings Detection

Elite creates a file `StartPreset.start` in the Bindings directory that contains
the name of the currently active profile. The plugin now reads that file and
loads the correct profile automatically.

Should the file for some reason a) not exist, b) be empty or c) contain an
invalid preset name the old default is used: the most recently changed `.binds`
file.

You can still manually provide a bindings file to use instead by setting it as
the plugin context, as before.

## Support for non-US Keyboard Layouts

If you are using any non-US layout you might have noticed that some binds don’t
work. Elite internally uses keycode values (a number assigned to each key on the
keyboard) for its bindings but for some reason both displays and saves them as
keysyms (the label on the key), according to the UK QWERTY keyboard layout. That
means VoiceAttack can’t just send the keysym it reads from a binding, it has to
translate it into the corresponding keycode.

The original plugin contained a `EDMap.txt` file that contains infomration on
that conversion _for the US keyboard layout_. If you are using any other layout
that information will be incorrect for any symbols that are on a different key
than they are on the US layout.

I have added the option to use maps for other keyboard layouts. In order to do
so you will have to set a text variable in VoiceAttack called `bindED.layout` to
the layout you want to use before invoking the plugin. If the variable is not
set it will defaut to “en-us”, leaving the original behaviour intact.

I have included a map file for [Neo2](https://neo-layout.org)
(`EDMap-de-neo2.txt`) which is the layout that I am using personally. If you are
on a different layout, you will have to create a corresponding map file yourself
or prod me to add it. E.g. for the french AZERTY it would be `EDMap-fr-fr.text`
and set `bindED.layout` to “fr-fr”. For US Dvorak, `EDmap-en-us-dvorak` and
“en-us-dvorak”. You can see where this is going.

For more information on [creating new supported keyboard layouts see the
Wiki](https://github.com/alterNERDtive/bindED/wiki/Keyboard-Layouts).