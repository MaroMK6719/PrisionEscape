# Guía rápida · Rescata la llave

Cómo descargar el minijuego **Rescata la llave** y ejecutarlo en Unity en unos 5 minutos.

## Requisitos

- **Unity 6** (6000.x) con un proyecto de la plantilla **Universal 2D**.
- Para probar en el móvil: módulo **Android Build Support** (con OpenJDK y Android SDK & NDK).

## 1. Descargar

**Opción A · Sin Git:** en la página del repositorio, pulsa el botón verde **Code > Download ZIP** y descomprime el archivo.

**Opción B · Con Git:**

```
git clone https://github.com/MaroMK6719/PrisionEscape-Rescata-La-LLave.git
```

## 2. Copiar al proyecto de Unity

Copia la carpeta `Assets/Platybytes/RescataLaLlave` **completa y con sus archivos `.meta`** dentro de la carpeta `Assets/Platybytes/` de tu proyecto de Unity (si no existe `Platybytes`, créala). Copia también `Assets/Platybytes.meta` si tu proyecto no lo tiene.

El resultado tiene que quedar así:

```
TuProyecto/Assets/Platybytes/RescataLaLlave/Editor/
TuProyecto/Assets/Platybytes/RescataLaLlave/Scripts/
TuProyecto/Assets/Platybytes/RescataLaLlave/link.xml
```

> No borres los archivos `.meta`: llevan identificadores fijos para que el juego funcione igual en el ordenador de todos.

## 3. Ejecutar en el editor

1. Abre el proyecto en Unity y espera a que termine de compilar.
2. En la barra de menús, pulsa **Platybytes > Rescata la llave > Crear escena**.
3. Pon la vista **Game** en una resolución horizontal (por ejemplo, **1920x1080**).
4. Pulsa **Play**.

**Controles en el ordenador:** flechas o **WASD** para inclinar el laberinto.

## 4. Probar en el móvil (opcional)

1. En el móvil Android, activa las **Opciones de desarrollador** y la **Depuración USB**, y conéctalo por USB.
2. En Unity: **File > Build Profiles > Android > Switch Platform**.
3. En **Scene List**, deja marcada solo la escena `RescataLaLlave`.
4. Desmarca **Export Project**, elige tu móvil en **Run Device** y pulsa **Build And Run**.

En el móvil, la bola se mueve **inclinando el teléfono**. La postura con la que lo sujetas al empezar es la posición neutra; el botón **CALIBRAR** la vuelve a tomar.

## Si algo falla

| Problema | Solución |
| --- | --- |
| No aparece el menú **Platybytes** | Abre **Window > General > Console** y revisa los errores en rojo. Si hablan de `InputSystem`: **Edit > Project Settings > Player > Active Input Handling > Both**. |
| Sale «Missing script» | Faltan los `.meta`. Vuelve a copiar la carpeta completa, borra la escena y créala otra vez desde el menú **Platybytes**. |
| La bola va al revés en el móvil | En `Scripts/RescataLaLlave.cs`, pon `InvertirX` o `InvertirY` a `true`. |
| El móvil no aparece en **Run Device** | Prueba otro cable USB, pon el USB en modo **Transferencia de archivos** y acepta el aviso de depuración. |
