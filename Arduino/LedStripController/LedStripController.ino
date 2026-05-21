#include <Adafruit_NeoPixel.h>
#include <WiFi.h>
#include <WiFiManager.h>
#include <WiFiUdp.h>

#define NUM_LEDS 120
#define LED_PIN D5
#define OSC_PACKET_SIZE 72  // OSC: 8B addr + 16B typetag + 12×4B ARGB int32
#define DEBUG 0

const uint8_t stripId = 4;  // this is 1-based and is the actual number
const int redChannel = 0;
const int greenChannel = 1;
const int blueChannel = 2;

Adafruit_NeoPixel pixels(NUM_LEDS, LED_PIN, NEO_GRB + NEO_KHZ800);

uint8_t segBuffer[12][4];  // per-segment [A, R, G, B]
int packetCount[2];
int packetRate[2];

WiFiUDP udp;
WiFiManager wifiManager;

unsigned long lastEmit = 0;
unsigned long lastDebugEmit = 0;
unsigned long lastGridUpdate = 0;
unsigned long lastGridUpdateDuration = 0;
unsigned long lastPacketRateUpdate = 0;

void setup() {
  delay(4000);
  Serial.begin(115200);
  Serial.println("Connecting WiFi");
  String hostName = "strip" + String(stripId);
  Serial.print("Hostname: ");
  Serial.println(hostName);
  wifiManager.setHostname(hostName);
  if (!wifiManager.autoConnect(hostName.c_str(), "Password123!")) {
    ESP.restart();
    delay(1000);
  }

  Serial.println("WiFi connected");
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());

  // setup LED indicator
  //setupIndicatorLed();

  // setup UDP receiver
  udp.begin(9977);

  // setup LED grid
  pixels.begin();  // Initialize the NeoPixel library
  pixels.setBrightness(15);
  pinMode(LED_BUILTIN, OUTPUT);
}

void loop() {
  int res = readIncomingPackets();
  trackPacketCount(res);

  //toggleIndicatorLed();
  updateLedGrid();
  emitDebug();
}

void trackPacketCount(int res) {
  if (res < 0)
    packetCount[0]++;
  if (res > 0)
    packetCount[1]++;

  if (millis() - lastPacketRateUpdate >= 1000) {
    lastPacketRateUpdate = millis();
    packetRate[0] = packetCount[0];
    packetCount[0] = 0;
    packetRate[1] = packetCount[1];
    packetCount[1] = 0;
  }
}

void emitDebug() {
  if (DEBUG != 1) return;
  if (millis() - lastDebugEmit <= 1000) return;
  lastDebugEmit = millis();
  Serial.print("fail: ");
  Serial.print(packetRate[0]);
  Serial.print("/s - ok: ");
  Serial.print(packetRate[1]);
  Serial.println("/s");
  Serial.print("last grid update: ");
  Serial.print(lastGridUpdateDuration);
  Serial.println("ms");
}

int readIncomingPackets() {
  int packetSize = udp.parsePacket();
  if (packetSize == 0) return 0;

  if (packetSize != OSC_PACKET_SIZE) {
    udp.flush();
    return -1;
  }

  uint8_t udpBuffer[OSC_PACKET_SIZE];
  int bytesRead = udp.read(udpBuffer, OSC_PACKET_SIZE);
  if (bytesRead != OSC_PACKET_SIZE) return -1;

  // Verify OSC address "/led/N" where N is 1-based strip index
  if (udpBuffer[0] != '/' || udpBuffer[1] != 'l' || udpBuffer[2] != 'e' ||
      udpBuffer[3] != 'd' || udpBuffer[4] != '/' ||
      udpBuffer[5] != ('0' + stripId)) {
    return 0;
  }

  // Data starts at offset 24 (8B address + 16B typetag)
  for (int i = 0; i < 12; i++) {
    segBuffer[i][0] = udpBuffer[24 + i * 4 + 0];  // A
    segBuffer[i][1] = udpBuffer[24 + i * 4 + 1];  // R
    segBuffer[i][2] = udpBuffer[24 + i * 4 + 2];  // G
    segBuffer[i][3] = udpBuffer[24 + i * 4 + 3];  // B
  }
  return 1;
}

void setupIndicatorLed() {
  int freq = 5000;     // PWM frequency (Hz)
  int resolution = 8;  // PWM resolution (bits) - gives values from 0 to 255

  // Configure PWM channels
  ledcSetup(redChannel, freq, resolution);
  ledcSetup(greenChannel, freq, resolution);
  ledcSetup(blueChannel, freq, resolution);

  // Attach the LED pins to the PWM channels
  ledcAttachPin(9, redChannel);
  ledcAttachPin(10, greenChannel);
  ledcAttachPin(11, blueChannel);
}

void toggleIndicatorLed() {
  if (millis() - lastEmit <= 1000) return;
  lastEmit = millis();

  uint8_t r = 0;
  uint8_t g = 0;
  uint8_t b = 0;

  if (packetCount[0] == 0 && packetCount[1] != 0) {
    r = 255;
    g = 0;
    b = 0;
  } else if (packetCount[0] != 0 && packetCount[1] == 0) {
    r = 0;
    g = 255;
    b = 0;
  } else if (packetCount[0] != 0 && packetCount[1] != 0) {
    r = 255;
    g = 140;
    b = 0;
  }

  packetCount[0] = 0;
  packetCount[1] = 0;

  ledcWrite(redChannel, r);
  ledcWrite(greenChannel, g);
  ledcWrite(blueChannel, b);
}

void updateLedGrid() {
  if (millis() - lastGridUpdate < 20) return;
  lastGridUpdate = millis();

  long start = millis();
  for (int i = 0; i < 12; i++) {
    uint8_t a = segBuffer[i][0];
    uint8_t r = ((uint16_t)segBuffer[i][1] * a) / 255;
    uint8_t g = ((uint16_t)segBuffer[i][2] * a) / 255;
    uint8_t b = ((uint16_t)segBuffer[i][3] * a) / 255;
    uint32_t col = pixels.Color(r, g, b);
    for (int j = 0; j < 10; j++) {
      int8_t index = i * 10 + j;
      int32_t actual = pixels.getPixelColor(index);
      if (actual != col) {
        pixels.setPixelColor(index, col);
      }
    }
  }
  pixels.show();
  lastGridUpdateDuration = millis() - start;
}