# EnMotion

Shows which emotes, dances and expressions your Penumbra mods actually change, plays
them with one click, and lets you pick idle poses directly instead of cycling through
them. Built to stay open on screen while you play.

## Why

Mods change emotes, but the game never tells you which ones. You end up remembering
that `/dote` looks different now and hunting for the rest by trial and error.

EnMotion asks Penumbra and lists exactly what is altered by an **enabled** mod — nothing
else. Switch a mod off and its emotes disappear from the list; switch it back on and
they return.

## Requirements

**Penumbra is required.** The whole plugin lives on its mod information; without it
there is nothing to show, and EnMotion says so instead of presenting an empty window.

## What it does

**Emotes, Dances and Expressions** each have their own tab and show your modded ones
as a grid of game icons. Click a tile to play it; it lights up briefly as confirmation.
Locked emotes carry a padlock, and emotes you can't use right now (mounted, for
example) are dimmed. Dances are told apart by their animation path and expressions by
the game's emote category, so both work on any client language.

**Right-click a tile** to play it, add or remove it from your favourites, copy its text
command, or show only what a particular mod changes.

**Favourites** sit at the top of every tab, marked with a heart, in the order you added
them — emotes, dances and expressions together, so your go-to set is always one click
away no matter which tab is open.

**Mod filter.** The *Mods* button next to the search opens a list of every mod that
touches the current tab, with how many entries each one changes. Tick one or several
to narrow the grid down. The search also matches mod names.

**Poses** shows your current stance as a card with one chip per idle pose. Click a chip
to jump straight to it — no more tapping `/cpose` four times and overshooting. It works
out of a running emote too; the pose applies once the character settles. Right-click a
chip to make it a favourite: favourite poses of your current stance appear in the
favourites row of every tab.

## Keeping it open

- **Compact view** drops the title bar, header and section titles and leaves a slim
  strip with tab icons, search and the grid. Switch with the button in the header or
  in the strip; each view remembers its own size.
- **Background opacity** lets the game show through while tiles and text stay solid.
- **Lock position and size** so a misclick during an emote doesn't drag the window.
- **Hide in combat** hides window and button during fights; they come back afterwards.
- **Close after playing** for those who prefer to open it, pick, and get it out of the
  way.

## The button

A small floating button opens the window. Choose between a hotbar-style game icon —
any emote icon you like — or the EnMotion look. Drag it where you want it, lock it,
reset its position if it ever goes missing, or turn it off and use `/enmotion` instead.
It only appears once you are logged in.

## How it works

**Emotes are played through the game's own emote agent**, not by typing into the chat
box. That avoids a signature hook that would break on every patch, and it means the
plugin knows whether an emote is unlocked or usable right now.

**Only enabled mods count.** EnMotion reads the collection that actually applies to
your character, so what you see is what is really in effect.

**It stays light.** The list is refreshed when Penumbra reports a change to your mods,
not on a timer, so keeping the window open all evening costs next to nothing.

## Settings

`/enmotion`, the button, or the cog in Dalamud's plugin list. Settings live in their
own tab: window, tiles and button.

## Building

```
dotnet build
dotnet test
```

`EnMotion.Core` holds favourites, pose favourites and list building and has no Dalamud
reference — that is what makes those testable without the game running. Everything
that touches Lumina, Penumbra or the game's memory lives in the plugin project.

## Licence

AGPL-3.0
