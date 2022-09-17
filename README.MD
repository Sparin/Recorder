# Recorder

My personal research how to improve desktop capturing in .NET.

## Problem statement

Windows systems have a few options to capture Desktop. The most easiest way is to use GDI+ function [BitBlt](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-bitblt) ([System.Drawing.Graphics.CopyFromScreen](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.graphics.copyfromscreen)), but it relies on CPU and perfomance is not sufficient for desktop recording. The other ways were [described](https://www.virtualdub.org/blog2/entry_356.html) by VirtualDub developer and these ways got their cons and pros. Since we're not limited to Windows XP, my goal is to capture user's desktop in efficient way in .NET environment.

## Principles

* High-perfomance capturing (60 fps at least)
* Minimum GC collect operations as possible

## Milestones

- [x] Proof of Concept using DirectX 11 / DXGI 1.2
- [ ] Rotated monitors support
- [ ] Draw pointer
- [ ] Review releasing unmanaged memory
- [ ] Hardware changed (number of monitors, resolution, rotation) event support
- [ ] Improve error handling instead of throwing exceptions
- [ ] (?) Continous frame processing
- [ ] (?) Multithread processing
- [ ] (?) Single display capture
- [ ] (?) DirectX conversions (stretch, draws, crop)