#include <Servo.h>

// DEBUG
const bool debug = false;

// CONFIGURACIONES
const int TIEMPO_LECTURA = 5000;
const int TIEMPO_REINICIO = 1000;

// Pines
const int PIN_SENSOR = 8;
const int PIN_BOTON = 3;
const int PIN_SERVO = 11;

// PENDULO
bool comenzoLectura = false, finalizo = false;
int primerTick;

// BOTON
int tiempoPresion = 0;
bool presionadoAnteriormente = false;

// TIEMPO DELTA
int lastMillis = 0;
int deltaTime = 0;
int actualMillis;

// SERVO
Servo servo;

void setup() 
{
  // Se inicia la comunicación por serial
  Serial.begin(9600);

  // Si inician los pines
  pinMode(INPUT, PIN_SENSOR);
  pinMode(INPUT, PIN_BOTON);   
  servo.attach(PIN_SERVO);

  OrigenServo();
}

void loop() 
{
  // CALCULO DE DELTA TIEMPO
  actualMillis = millis();
  deltaTime = actualMillis - lastMillis;
  lastMillis = actualMillis;
  
  // LECTURA
  Lectura();

  // BOTON INICIO/REINICIO
  Boton();
}

void Lectura()
{
  // Si el sensor de próximidad se activa y no ha finalizado la lectura, entonces se mandan a que milisegundos de que comenzo la lectura el sensor encontró algo
  if (!digitalRead(PIN_SENSOR) && !finalizo)
  {
    actualMillis = millis();
    // Se manda la diferencia del tiempo
    if (!comenzoLectura)
    {
        if (debug) Serial.println("Retomando tiempo inicial");
        comenzoLectura = true;
        primerTick = actualMillis;
    }
    Serial.print(String( actualMillis - primerTick ) + "\n");
  }

    // Si la lectura ya se comenzo, y ya han pasado 'TIEMPO_LECTURA' segundos desde que comenzo, se detiene.
  if (comenzoLectura && !finalizo && actualMillis - primerTick >= TIEMPO_LECTURA)
  {
    // Tag para inidicar fin de lectura
    Serial.println("[FIN]");
    // Bandera para indicar que no se deben enviar más datos.
    finalizo = true;
  }
}


void Boton()
{
  // Si el botón se activa y la lectura no ha comenzado.
  if (!digitalRead(PIN_BOTON))
  {
    if (debug) Serial.println(tiempoPresion);
    tiempoPresion += deltaTime; 
    presionadoAnteriormente = true;
  }
  else if (presionadoAnteriormente)
  {
    if (tiempoPresion >= TIEMPO_REINICIO)
    {
      if (debug) Serial.println("CLICK SOSTENIDO");
      comenzoLectura = false;
      finalizo = false;
      
      OrigenServo();
    }
    else
    {
      if (debug) Serial.println("CLICK");
      // Si la lectura no se ha comenzado, se obtiene el primerTick (Para restar todos los valores con este y hacer asi que los siguientes esten 'cerados')
      if (!comenzoLectura)
      {
        finalizo = false;
        comenzoLectura = false;
        primerTick = 0;
        // SE SUELTA LA MASA
        BajarServo();
      }
    }  

    presionadoAnteriormente = false;
    tiempoPresion = 0;
  }
  else
  {
    tiempoPresion = 0;
  }
}

void OrigenServo()
{
  servo.write(85);
}


void BajarServo()
{
 servo.write(30); 
}

