#include <Adafruit_NeoPixel.h>
#include <WiFi.h>
#include <WiFiManager.h>
#include <WiFiUdp.h>

#define NUM_LEDS 120
#define LED_PIN D5
#define MAX_PACKET_SIZE 16  // 10 segments + color
const uint8_t stripId = 1; // this is 1-based and is the actual number

Adafruit_NeoPixel pixels(NUM_LEDS, LED_PIN, NEO_GRB + NEO_KHZ800);

uint8_t udpPacketBuffer[MAX_PACKET_SIZE];

WiFiUDP udp;
WiFiManager wifiManager;

int idx = 0;
bool off = false;

void setup() {
  Serial.begin(9600);
  Serial.println("Connecting WiFi");

  char* hostName = const_cast<char*>(("led-strip-" + String(stripId)).c_str());
  wifiManager.setHostname(hostName);
  if (!wifiManager.autoConnect(hostName, "Password123!")) {
    ESP.restart();
    delay(1000);
  }

  Serial.println("");
  Serial.println("WiFi connected");
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());

  udp.begin(9977);

  pixels.begin();  // Initialize the NeoPixel library
  pixels.setBrightness(15);
  pinMode(LED_BUILTIN, OUTPUT);
}

uint8_t currR = 100, currG = 100, currB = 100;

void loop() {
  int packetSize = udp.parsePacket();
  if (packetSize == 0) {
    return;
  }
  if (packetSize != MAX_PACKET_SIZE) {
    Serial.print("Invalid packet size ");
    Serial.println(packetSize);
    return;
  }

  int bytesRead = udp.read(udpPacketBuffer, packetSize);
  if (udpPacketBuffer[0] != stripId - 1) {
    return;
  }

  for (int i = 0; i < 12; i++) {
    uint32_t col = 0;
    float factor = (float)udpPacketBuffer[i + 4] / (float)255;
    uint8_t r = udpPacketBuffer[1] * factor;
    uint8_t g = udpPacketBuffer[2] * factor;
    uint8_t b = udpPacketBuffer[3] * factor;
    col = pixels.Color(r, g, b);
    for (int j = 0; j < 10; j++) {
      int8_t index = i * 10 + j;
      int32_t actual = pixels.getPixelColor(index);
      if (actual != col) {
        pixels.setPixelColor(index, col);
      }
    }
  }
  pixels.show();
}