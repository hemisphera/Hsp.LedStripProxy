# Introduction

This is a project that converts MIDI messages into UDP messages that are sent to external LED strips via WiFi to create a light show. The project consists of two parts:

- A service written in .NET using ASP.NET Core that listens to MIDI messages and sends UDP messages to the LED strips.
- An arduino sketch that runs on an ESP32 microcontroller, which receives the UDP messages and controls the LED strips accordingly.

# Critical Information

Timing is of the essence. The service must process MIDI messages and send UDP messages with minimal latency to ensure that the light show is synchronized with the music. The ESP32 must also be able to receive and process UDP messages quickly to control the LED strips in real-time.

# Architecture

The setup is as follows
- REAPER as the host inside which the plugin "Hsp.LedStripController" will run
- "ledcontroller.jsfx" as the interface. This is a REAPER JSFX plugin that lives on a track inside REAPER and receives the MIDI data from the protocol. It will then write the this data as into a shared GMEM memory block. The source for this JSFX plugin can be found at [https://github.com/hemisphera/reaper_scripts/blob/master/Effects/ledcontroller.jsfx]
- The "Hsp.LedStripController" plugin will read from the shared GMEM block and convert the data to OSC messages. These messages are then broadcast over the network.
- "Hsp.LedStripEmulator" serves as a debugging tool to emulate the LED strips and test them.
- [Arduino/LedStripController/LedStripController.ino] is a program running on an Arduino Nano ESP32 (4 of them) receiving the OSC messages and controlling the LED strip that it is connected to.

# GMEM Layout

The memory layout of the GMEM block that the JSFX writes to must be compatible with the memory layout that the REAPER plugin reads from. Always make sure you respect the layout as defined in the description of the JSFX.

# MIDI Protocol

The service will listen for MIDI messages on a specified port and convert them into UDP messages. The UDP messages will be sent to the ESP32, which will interpret them and control the LED strips accordingly.
The MIDI message protocol is defined as follows:
- This supports up to 4 LED strips. Each LED strip corresponds to a MIDI channel, starting at 4. For example, MIDI channel 4 corresponds to LED strip 1, MIDI channel 5 corresponds to LED strip 2, and so on.
- Each LED strip is divided into 12 segments. Each segment consists of 10 single LEDs. Only an entire segment can be controlled. This means that each strip has a resolution if 12 segments. Each segment can be turned on or off.
- Each segment is adressed via MIDI note on or off. The MIDI note number corresponds to the segment number, starting at 0. For example, MIDI note 0 corresponds to segment 1, MIDI note 1 corresponds to segment 2, and so on.
- The velocity of the MIDI note on message determines the brightness of the segment. A velocity of 0 means the segment is off, while a velocity of 127 means the segment is at full brightness. Values in between will set the brightness proportionally.
- Sending a note off will also set the brightness to 0.
- MIDI notes 12 to 14 control the color for the entire strip. 12 is B, 13 is G and 14 is R. The velocity of the note on message determines the intensity of the color channel, with 0 being off and 127 being full intensity.
- In addition, MIDI CC 110 acts as a multiplier for the brightness of the strip. The value of the CC message (0-127) will be used to scale the brightness of all segments on the strip. For example, if the CC value is 64, the brightness of all segments will be reduced to approximately 50% of their original brightness.

# OSC Protocol

## Transport

- UDP broadcast on port **9977** (default, configurable via `Settings.UdpPort`)
- The proxy broadcasts to all devices on the network; each device filters packets by OSC address
- One OSC message per strip per tick, sent at a configurable interval (default **10 ms**, i.e. 100 Hz)

## OSC Message Format

Every message is exactly **72 bytes**. Packets with a different size are silently discarded by the receiver.

| Offset | Size | Field       | Content                                                    |
|--------|------|-------------|------------------------------------------------------------|
| 0      | 8 B  | Address     | `/led/N\0\0` — N is the 1-based strip index (ASCII digit)  |
| 8      | 16 B | Type tag    | `,iiiiiiiiiiii\0\0\0` — 12 OSC int32 arguments             |
| 24     | 48 B | Data        | 12 × big-endian int32, each encoding one segment as ARGB   |

### Per-Segment int32 Layout (big-endian on the wire)

| Byte within int32 | Field | Description                                      |
|-------------------|-------|--------------------------------------------------|
| 0 (MSB)           | A     | Brightness / alpha (0 = off, 255 = full)         |
| 1                 | R     | Red component (0–255)                            |
| 2                 | G     | Green component (0–255)                          |
| 3 (LSB)           | B     | Blue component (0–255)                           |

OSC int32 (`i`) is used instead of float32 (`f`) to avoid IEEE 754 NaN/Inf collisions that arise when ARGB bytes are reinterpreted as a float bit pattern (e.g. `0xFFFFFFFF` = white at full brightness is a quiet NaN).

## Receiving (Arduino)

- The Arduino listens on UDP port 9977.
- A packet is accepted only when **all** conditions are met:
  1. `packetSize == 72`
  2. Address matches `/led/N` where `N == stripId` (the Arduino's `stripId` is 1-based, 1–4)
- Accepted data is copied into `segBuffer[12][4]` (`[segment][A, R, G, B]`).
- The LED grid is refreshed at most every **20 ms**.
- Each strip has **12 segments**, each driving **10 physical LEDs** (120 LEDs total).

## Color Computation on the Arduino

Each segment has its own ARGB value. `A` acts as a per-segment brightness multiplier:

```
R_eff = (R × A) / 255
G_eff = (G × A) / 255
B_eff = (B × A) / 255
```

Multiplication is done in `uint16_t` to avoid overflow before the division. An `A` of `0` turns the segment off; `255` renders it at full color intensity.

This model is backward-compatible with the MIDI implementation, which sets a single RGB base color per strip and varies only `A` per segment (the former brightness byte). Per-segment full color control is available in the protocol for future use.

## Strip Addressing Summary

| OSC address | Arduino `stripId` |
|-------------|-------------------|
| `/led/1`    | 1                 |
| `/led/2`    | 2                 |
| `/led/3`    | 3                 |
| `/led/4`    | 4                 |

