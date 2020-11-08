# devel

I did a copmlete refactoring of everything to prepare for some juicy new
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

* The `listbinds` context will set the text variable `~bindED.bindsList` to a
  list of bindings present in the current bindings file. See README for details.
  (#1).

-----

# 2.0 (2020-09-23)

## Added

* Support for non-US keyboard layouts. `de-neo2` is included (because that’s
  what I’m using), others can be added ([see the wiki for
  instructions](https://github.com/alterNERDtive/bindED/wiki/Keyboard-Layouts)).