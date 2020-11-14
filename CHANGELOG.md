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