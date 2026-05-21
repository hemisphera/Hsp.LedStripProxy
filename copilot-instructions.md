# Introduction

This is a project that converts MIDI messages into UDP messages that are sent to external LED strips via WiFi to create a light show. The project consists of two parts:

- A service written in .NET using ASP.NET Core that listens to MIDI messages and sends UDP messages to the LED strips.
- An arduino sketch that runs on an ESP32 microcontroller, which receives the UDP messages and controls the LED strips accordingly.

# Critical Information

Timing is of the essence. The service must process MIDI messages and send UDP messages with minimal latency to ensure that the light show is synchronized with the music. The ESP32 must also be able to receive and process UDP messages quickly to control the LED strips in real-time.

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

# UDP Protocol

## Transport

- UDP broadcast on port **9977** (default, configurable via `Settings.UdpPort`)
- The proxy broadcasts to all devices on the network; each device filters packets by strip index

## Packet Format

Every packet is exactly **16 bytes** (`LedCount(12) + 4 header bytes`). Packets with a different size are silently discarded by the receiver.

| Byte(s) | Field              | Type    | Description                                                       |
|---------|--------------------|---------|-------------------------------------------------------------------|
| 0       | Strip index        | uint8   | 0-based strip identifier. Matches `stripId - 1` on the Arduino.   |
| 1       | Red                | uint8   | Red component of the strip's base color (0–255)                   |
| 2       | Green              | uint8   | Green component of the strip's base color (0–255)                 |
| 3       | Blue               | uint8   | Blue component of the strip's base color (0–255)                  |
| 4–15    | Segment brightness | uint8×12| Per-segment brightness, one byte per segment (0 = off, 255 = full)|

## Sending (Proxy → Arduino)

- Packets are sent at a configurable interval (default **10 ms**, i.e. 100 Hz via `Settings.UpdateInterval`).
- Header bytes (0–3: index + RGB) are transmitted as-is.
- Segment brightness bytes (4–15) are multiplied by the current strip multiplier (0.0–1.0) before sending. This corresponds to the MIDI CC 110 value scaled to the 0–1 range.

## Receiving (Arduino)

- The Arduino listens on UDP port 9977.
- A packet is accepted only when **both** conditions are met:
  1. `packetSize == 16`
  2. `packet[0] == stripId - 1` (the Arduino's `stripId` is 1-based)
- The LED grid is refreshed at most every **20 ms**.
- Each strip has **12 segments**, each driving **10 physical LEDs** (120 LEDs total).

## Color Computation on the Arduino

The base color (bytes 1–3) is shared across all segments. Each segment's effective color is computed by scaling the base color by the segment's brightness:

```
R_eff = R_base × (brightness / 255)
G_eff = G_base × (brightness / 255)
B_eff = B_base × (brightness / 255)
```

A brightness of `0` turns the segment off; `255` renders it at full base color intensity.

## Strip Addressing Summary

| Proxy strip index (byte 0) | Arduino `stripId` |
|----------------------------|-------------------|
| 0                          | 1                 |
| 1                          | 2                 |
| 2                          | 3                 |
| 3                          | 4                 |

