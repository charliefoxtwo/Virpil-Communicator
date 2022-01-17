# Virpil Communicator

[![Nuget](https://img.shields.io/nuget/v/Virpil.Communicator?style=flat-square)](https://www.nuget.org/packages/Virpil.Communicator/)
[![.NET 5 CI build](https://github.com/charliefoxtwo/Virpil-Communicator/actions/workflows/ci-build.yml/badge.svg?branch=develop)](https://github.com/charliefoxtwo/Virpil-Communicator/actions/workflows/ci-build.yml)
[![GitHub](https://img.shields.io/github/license/charliefoxtwo/Virpil-Communicator?style=flat-square)](LICENSE)
[![Discord](https://img.shields.io/discord/840762843917582347?style=flat-square)](https://discord.gg/rWAF3AdsKT)

**Virpil Communicator** allows you to talk to Virpil HID devices. It is cross-platform and has been tested on Windows and Mac. It's fairly feature limited at this moment and is currently designed for firmware version **20210102**.

<img src="https://raw.githubusercontent.com/charliefoxtwo/Virpil-Communicator/main/VirpilCommunicator/resources/led.png" alt="Virpil Communicator logo - a vector outline of an led" width="150" />

## Usage/Examples

```c#
// 0x825B is the PID of Virpil Control Panel #2
var controlPanel2 = new DeviceCommunicator(0x825B, logger);

// set LED #1 on the panel to #FFFFFF (white)
controlPanel2.SendCommand(BoardType.OnBoard, 1, LedPower.Full, LedPower.Full, LedPower.Full);
```

```c#
// initialize a new device monitor
var monitor = VirpilMonitor.Initialize(loggerFactory);

// try to get the device - it may have been disconnected
if (_monitor.TryGetDevice(0x825B, out var cp2))
{
    // set LED #1 on the panel to #FFFFFF (white)
    cp2.SendCommand(BoardType.OnBoard, 1, LedPower.Full, LedPower.Full, LedPower.Full);
}
```

```c#
// initialize a new device monitor
var monitor = VirpilMonitor.Initialize(loggerFactory);

var numberOfDevices = _monitor.AllConnectedVirpilDevices.Count;
```

## Packet structure

This document is probably accurate enough for firmware 20210102. I make no guarantee any of this data will hold true for any other firmware version.

The data fragment for setting an LED (and perhaps other things, too) is 38 bytes in length. A sample payload might look something like the following:

```
02 66 05 00 00 80 00 00 00 00 00 00 00 00 00 00
00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
00 00 00 00 00 f0
```

This data should be written as a `Feature`.

### Understanding the bytes

#### 0x00: `0x02`
I'm not sure what this is for, but it's at the start of every data fragment

#### 0x01: `0x66`
For our purposes, this is the type of board you're communicating with. Possible values are:
 - `0x64`: default : used to set all leds back to their default settings that were set with the profile
 - `0x65`: add-board : I'm not sure what this is for
 - `0x66`: on-board : used when the led you want to modify is on the device you're communicating with
 - `0x67`: slave-board : used when the led you want to modify is on a slave device of the device you're communicating with

#### 0x02: `0x05`
This is the id of the command you are running. You can view these commands in the VPC_LED_Control tool, but I'll list them here too.
 - `0x00`: used to set all leds back to default
 - `0x01`-`0x04`: used for setting add-board leds 1-4
 - `0x05`-`0x18`: used for setting on-board leds 1-20
 - `0x19`-`0x2C`: used for setting slave-board leds 1-20

#### 0x05: `0x80`
Defines the [color](#the-color-byte) the LED should be set to. 

> **NOTE!** the position of this byte varies depending on the led being modified. For example, if you were modifying LED 2 this would be in slot 06, if you were modifying LED 15 this would be in slot 19, etc.

#### 0x25: `0xf0`
Not really sure what this one does either, but it seems to close out every data fragment.

### The Color Byte
Since color is sent over a single byte, we effectively get [6-bit](https://lospec.com/palette-list/6-bit-rgb) (2-2-2) color (note the colors at that link are slightly different from the ones you'll see on your hardware, as your hardware uses 25/50/100% instead of 33/67/100%). A sample color might look something like this:
```
1010 1100
```
and is structured as
```
10bb ggrr
```

This means that each channel has just 4 states - off *[00]*, and 3 intensities of on (25% *[01]*, 50% *[10]*, and 100% *[11]*) - for a total of 64 possible colors. In this example, we have 0 red, 100% green, and 50% blue - producing a lovely [Irish Stream](https://colornames.org/color/00ff80) color.

### Examples

#### Set LEDs to default colors
```
0000   02 64 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0010   00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0020   00 00 00 00 00 f0
```

#### Set on-board LED 3 to bright magenta
```
0000   02 66 07 00 00 00 00 b3 00 00 00 00 00 00 00 00
0010   00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0020   00 00 00 00 00 f0
```

#### Set slave-board LED 14 to dark cyan
```
0000   02 67 26 00 00 00 00 00 00 00 00 00 00 00 00 00
0010   00 00 94 00 00 00 00 00 00 00 00 00 00 00 00 00
0020   00 00 00 00 00 f0
```


## Acknowledgements

- [Package icon](https://www.flaticon.com/authors/those-icons)
- [readme tools](https://readme.so)
- [badges](https://shields.io)
