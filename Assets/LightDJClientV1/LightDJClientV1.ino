#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
#include <FastLED.h>



FASTLED_USING_NAMESPACE

const char* ssid = "McdonaldsPlayground";
const char* password = "vivaelmaco";

byte id = 3;
byte timeout = 1;
unsigned long lastpacketmillis = 0;        // will store last time LED was updated
unsigned long connectedtimemillis = 0;

WiFiUDP Udp;
unsigned int localUdpPort = 42069;  // local port to listen on
char incomingPacket[1460];  // buffer for incoming packets
char replyPacket[] = "Hi there! Got the message :-)";  // a reply string to send back

//pins
byte RedPin = D6;
byte GreenPin = D8;
byte BluePin = D5;

//FASTLED stuff
#define DATA_PIN    D4
//#define CLK_PIN   4
#define LED_TYPE    WS2811
#define COLOR_ORDER GRB
#define NUM_LEDS    300
CRGB leds[NUM_LEDS];

#define BRIGHTNESS          96
#define FRAMES_PER_SECOND  120

uint8_t gCurrentPatternNumber = 0; // Index number of which pattern is current
uint8_t gHue = 0; // rotating "base color" used by many of the patterns

//END FASTLED

void setup()
{
  delay(1000); //delay for recovery
  pinMode(LED_BUILTIN, OUTPUT);
  pinMode(RedPin, OUTPUT);
  pinMode(GreenPin, OUTPUT);
  pinMode(BluePin, OUTPUT);

  
  digitalWrite(LED_BUILTIN, HIGH);
  Serial.begin(115200);
  Serial.println();

  Serial.printf("Connecting to %s ", ssid);
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED)
  {
    delay(500);
    Serial.print(".");
  }
  digitalWrite(LED_BUILTIN, LOW);
  Serial.println(" connected");

  Udp.begin(localUdpPort);
  Serial.printf("Now listening at IP %s, UDP port %d\n", WiFi.localIP().toString().c_str(), localUdpPort);

  FastLED.addLeds<LED_TYPE,DATA_PIN,COLOR_ORDER>(leds, NUM_LEDS).setCorrection(TypicalLEDStrip);
  FastLED.setBrightness(BRIGHTNESS);

  
  setLights(80,80,80); //set lights to flasy to show connection has happend

  for( int i = 0; i < 50; i++) {
    fill_solid(leds, NUM_LEDS, CRGB::Green);
    FastLED.show();
  }
  
  delay(100);
  
  setLights(0,0,0);
  fill_solid(leds, NUM_LEDS, CRGB::Black);
  FastLED.show();
  
}


void loop()
{
  int packetSize = Udp.parsePacket();
  if (packetSize){
    // receive incoming UDP packets
    //Serial.printf("Received %d bytes from %s, port %d\n", packetSize, Udp.remoteIP().toString().c_str(), Udp.remotePort());
    int len = Udp.read(incomingPacket, 1460);
    if (len > 0)
    {
      incomingPacket[len] = 0;
    }

    lastpacketmillis = millis(); //reset timer when any packet is recived [TODO only reset when heartbeat]
    
    byte command = incomingPacket[0];
    
    if(command == 1){ //get ID commmand
      char SendPacket[] = {id};
      Udp.beginPacket(Udp.remoteIP(), Udp.remotePort());
      Udp.write(SendPacket);
      Udp.endPacket();
      Serial.println(SendPacket);
    }else if(command == 2){ //Set lights commmand
      //Serial.println("SL");
      analogWrite(RedPin,incomingPacket[1]*4);
      analogWrite(GreenPin,incomingPacket[2]*4);
      analogWrite(BluePin,incomingPacket[3]*4);
    }else if(command == 3){ //Set timeout commmand
      timeout = incomingPacket[1]; //set timeout to second byte in message
    }else if(command == 4){ //SetMultipleLights
      for( int i = 0; i < NUM_LEDS; i++) {

      }
      for( int i = 0; i < 300; i++) {
        leds[i].r = incomingPacket[(i*3)+1];
        leds[i].g = incomingPacket[(i*3)+2];
        leds[i].b = incomingPacket[(i*3)+3];
      }
      
    }
    
  }
  doFastLEDLoop();
  
  if(millis()>lastpacketmillis+60000*timeout){
    setLights(0,0,0);
    digitalWrite(LED_BUILTIN, HIGH);
    fill_solid(leds, NUM_LEDS, CRGB::Black);
  }
}

void doFastLEDLoop(){
  FastLED.show();  
  FastLED.delay(1000/FRAMES_PER_SECOND);    // insert a delay to keep the framerate modest
  EVERY_N_MILLISECONDS( 20 ) { gHue++; } // slowly cycle the "base color" through the rainbow
  //EVERY_N_SECONDS( 10 ) { nextPattern(); } // change patterns periodically
}

void setLights(int r, int g, int b){
    analogWrite(RedPin,r);
    analogWrite(GreenPin,g);
    analogWrite(BluePin,b);
}

void rainbow() 
{
  fill_rainbow( leds, NUM_LEDS, gHue, 7);
}




//Helper functions
//Serial.printf("UDP packet YEET contents: %s\n", incomingPacket);
//Serial.println("Temp: "+String(temp));

//old code
/*
 *       if(incomingPacket[3] == 0x01){
        //digitalWrite(LED_BUILTIN, LOW);
      }else{
        //digitalWrite(LED_BUILTIN, HIGH);
      }
 */
 
