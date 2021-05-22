# devel

## Added

* optional `~bindsFile` parameter for the `loadbinds` plugin context: use that
  to specify a binds file instead of auto-detecting it from the currently active
  preset. (#18)

## Changed

* Updated the README to reflect that you need to load the game once, and that
  you need to have changed at least a single bind. (#19)
* Updated the README to reflect that you have to use a single preset for all
  sections if you’re playing Odyssey.

## Fixed

* Now only reading the first line of `startPreset.start` to work correctly with
  Odyssey. (#15)
* Now correctly prioritizing `.4.0.binds` > `.3.0.binds` > `.binds`. (#20)

# 4.0 (2021-05-19)

**Note**: If you do not own Odyssey, everything will work just as before!

I, too, do not own Odyssey. So while I have tried testing various things with
mock Odyssey binds files, please keep an eye out for bugs and [file an
issue](https://github.com/alterNERDtive/bindED/issues/new/) if you
encounter any. And check back for a potential 4.0.1 soon. TYVM!

**IMPORTANT**: Please backup your binds files before installing this release,
just in case. You can find them in
`%localappdata%\Frontier Developments\Elite Dangerous\Options\Bindings`.

Sadly for the time being Odyssey and Horizons will basically be separate games.
That also means they have separate binds files.

BindED will by default always use the last edited file, be that the base preset,
Horizons or Odyssey.

To keep hassle to a minimum, the recommended way to change binds is to do it
from Odyssey. When a change to the Odyssey file is detected, the plugin will
by default overwrite Horizons’ binds with it. To prevent that and keep entirely
separate binds, you can set `bindED.disableHorizonsSync#` (yes, including the
pound sign) to `true` in your VoiceAttack profile.

## Added

* Odyssey binds file support (`*.4.0.binds`). (#14)
* `bindED.disableHorizonsSync#` configuration option: Set this (to `true`) in
  your VoiceAttack profile to disable automatically syncing Odyssey binds
  changes to Horizons binds.

## Removed

* empty plugin context: Invoking the plugin without context no longer gives a
  deprecation warning and will instead fail.
* binds file as plugin context: Invoking the plugin with a binds file as context
  no longer gives a deprecation warning and will instead fail.

-----

# 3.1 (2021-01-29)

## Changed

* Invoking the `loadbinds` context will now force reset everything and reload
  from scratch. (#5)

## Added

* The current layout’s key map file is now monitored for changes. Should make
  adding support for new layouts slightly less annoying. (#4)

-----

# 3.0 (2020-11-12)

I did a complete refactoring of everything to prepare for some juicy new
features! Sadly that also meant breaking backwards compatibility. On the plus
side, the things that no longer work like they did in Gary’s initial release
should basically never be used anyway.

## Removed

* You can no longer specify binding files to use by linking them into the plugin
  directory.
* You can no longer specify binding files by using them as the plugin context.

## Changed

* Invoking the plugin with no context or with a binds file as context is now
  deprecated and will be removed in a future version. Use the `loadbinds`
  context instead.

## Added

* `en-gb` key map. Thank you A.Cyprus for the work on that!
* Bindings are now automagically read when VoiceAttack loads and when
  `bindED.layout#` is changed.
* After the initial reading of bindings the plugin will monitor the bindings
  directory for changes to a) the `StartPreset.start` file (preset has changed)
  and b) the binds file(s) corresponding to the current preset. Changes are
  automatically applied. (#3)
* The `listbinds` context will set the text variable `~bindED.bindsList` to a
  list of bindings present in the current bindings file. (#1)
* The `missingbinds` context will create a report of missing binds (anything
  that doesn’t have keyboard binds) and save it to `~bindED.missingBinds`. (#2)
* The included `bindED-reports` profile runs a missing binds report and a binds
  list report when you load it and saves them to your Desktop.

-----

# 2.0 (2020-09-23)

## Added

* Support for non-US keyboard layouts. `de-neo2` is included (because that’s
  what I’m using), others can be added ([see the wiki for
  instructions](https://github.com/alterNERDtive/bindED/wiki/Keyboard-Layouts)).