#include <Adafruit_NeoPixel.h>
#include <WiFi.h>
#include <WiFiUdp.h>

#define NUM_LEDS 120
#define STRIP_ID 0
#define LED_PIN D5
#define MAX_PACKET_SIZE 16  // 10 segments + color

Adafruit_NeoPixel pixels(NUM_LEDS, LED_PIN, NEO_GRB + NEO_KHZ800);

const char* ssid = "<ssid>";
const char* password = "<key>";

uint8_t udpPacketBuffer[MAX_PACKET_SIZE];

WiFiUDP udp;

int idx = 0;
bool off = false;

void setup() {
  Serial.begin(9600);
  Serial.println("Connecting WiFi");

  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
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
  if (udpPacketBuffer[0] != STRIP_ID) {
    return;
  }

  for (int i = 0; i < 12; i++) {
    uint32_t col = 0;
    uint8_t factor = udpPacketBuffer[i + 4];
    col = pixels.Color(
      (udpPacketBuffer[1] * factor) / 127,
      (udpPacketBuffer[2] * factor) / 127,
      (udpPacketBuffer[3] * factor) / 127);
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