#include <ESP8266WiFi.h>
#include <WiFiUdp.h>

const char* ssid = "McdonaldsPlayground";
const char* password = "vivaelmaco";

byte id = 2;
byte timeout = 1;
unsigned long lastpacketmillis = 0;        // will store last time LED was updated
unsigned long connectedtimemillis = 0;

WiFiUDP Udp;
unsigned int localUdpPort = 42069;  // local port to listen on
char incomingPacket[255];  // buffer for incoming packets
char replyPacket[] = "Hi there! Got the message :-)";  // a reply string to send back

//pins
byte RedPin = D6;
byte GreenPin = D8;
byte BluePin = D5;

void setup()
{
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

  setLights(80,80,80);
  delay(100);
  setLights(0,0,0);
}


void loop()
{
  int packetSize = Udp.parsePacket();
  if (packetSize){
    // receive incoming UDP packets
    //Serial.printf("Received %d bytes from %s, port %d\n", packetSize, Udp.remoteIP().toString().c_str(), Udp.remotePort());
    int len = Udp.read(incomingPacket, 255);
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
      analogWrite(RedPin,incomingPacket[1]);
      analogWrite(GreenPin,incomingPacket[2]);
      analogWrite(BluePin,incomingPacket[3]);
    }else if(command == 3){ //Set timeout commmand
      timeout = incomingPacket[1]; //set timeout to second byte in message
    }
    
  }

  if(millis()>lastpacketmillis+60000*timeout){
    setLights(0,0,0);
    digitalWrite(LED_BUILTIN, HIGH);
  }
}

void setLights(int r, int g, int b){
    analogWrite(RedPin,r);
    analogWrite(GreenPin,g);
    analogWrite(BluePin,b);
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
 
