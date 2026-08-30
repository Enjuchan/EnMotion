# EnMotion

Shows which emotes your Penumbra mods actually change, plays them with one click, and
lets you pick idle poses directly instead of cycling through them.

## Why

Mods change emotes, but the game never tells you which ones. You end up remembering
that `/dote` looks different now and hunting for the rest by trial and error.

EnMotion asks Penumbra and lists exactly what is altered by an **enabled** mod — nothing
else. Switch a mod off and its emotes disappear from the list; switch it back on and
they return.

## Requirements

**Penumbra is required.** The whole plugin lives on its mod information; without it
there is nothing to show, and EnMotion says so on startup rather than presenting an
empty window.

## What it does

Three tabs:

**Emotes** and **Dances** show your modded ones as a grid of game icons. Click to play.
The star in a tile's corner pins it, and pinned entries move to the top row of their
tab — in the order you pinned them, so they stay where your hand expects them. Dances
are told apart by their animation path, not by name, so it works on any client
language.

**Poses** lists every idle pose of your current stance and lets you jump straight to
one. No more tapping `/cpose` four times and overshooting. It works out of a running
emote too — the emote is cancelled and the pose applies once the character settles.

A small button, styled like a hotbar slot, opens the window. Drag it where you want it,
lock it so it stays there, or turn it off and use `/enmotion` instead.

## How it works

**Emotes are played through the game's own emote agent**, not by typing into the chat
box. That avoids a signature hook that would break on every patch, and it means the
plugin knows whether an emote is unlocked — locked ones are greyed out instead of
doing nothing.

**Only enabled mods count.** EnMotion reads the collection that actually applies to
your character rather than walking every installed mod, so what you see is what is
really in effect.

## Settings

`/enmotion`, the button, or the cog in Dalamud's plugin list.

## Building

```
dotnet build
dotnet test
```

`EnMotion.Core` holds favourites and list building and has no Dalamud reference — that
is what makes those testable without the game running. Everything that touches Lumina,
Penumbra or the game's memory lives in the plugin project.

## Licence

AGPL-3.0
