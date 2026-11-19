# Guía rápida · Prison Escape (PLATYBYTES)

Repositorio común del equipo: aquí está el **proyecto de Unity completo** con los minijuegos de *Prison Escape*. Cada minijuego vive en su propia carpeta dentro de `Assets/Platybytes/`.

## Requisitos

- **Unity 6000.6.6f1** (la misma versión para todo el equipo, instalada desde Unity Hub).
- **Git** o **GitHub Desktop**.
- Para probar en el móvil: módulo **Android Build Support** (con OpenJDK y Android SDK & NDK).

## Estructura

```
Assets/Platybytes/
├── PressTheButton/      Minijuego de las flechas (script + escena PressTheButton.unity)
└── RescataLaLlave/      Minijuego del laberinto (scripts + menú del editor)
ProjectSettings/         Configuración común del proyecto
Packages/                Paquetes del proyecto (URP, Input System, TextMeshPro...)
```

## 1. Descargar y abrir el proyecto

1. Clona el repositorio:
   ```
   git clone https://github.com/MaroMK6719/PrisionEscape-Rescata-La-LLave.git
   ```
   (o en GitHub Desktop: **File > Clone repository**).
2. En **Unity Hub**, pulsa **Add > Add project from disk** y elige la carpeta clonada.
3. Ábrelo y espera a que Unity importe todo (la primera vez tarda unos minutos).

## 2. Probar cada minijuego

**Press the Button! (flechas):** abre `Assets/Platybytes/PressTheButton/PressTheButton.unity` y pulsa **Play**.

**Rescata la llave (laberinto):**
1. Pulsa **Platybytes > Rescata la llave > Crear escena** en la barra de menús (solo la primera vez).
2. Pon la vista **Game** en horizontal (por ejemplo, **1920x1080**) y pulsa **Play**.
3. En el ordenador se juega con las flechas o **WASD**; en el móvil, inclinando el teléfono.

## 3. Probar en el móvil (opcional)

1. En el móvil Android, activa las **Opciones de desarrollador** y la **Depuración USB**, y conéctalo por USB.
2. En Unity: **File > Build Profiles > Android > Switch Platform**.
3. En **Scene List**, deja marcada (y primera) la escena que quieras probar.
4. Desmarca **Export Project**, elige tu móvil en **Run Device** y pulsa **Build And Run**.

## 4. Normas para trabajar juntos

- **Cada uno trabaja solo en su carpeta** de `Assets/Platybytes/`. No edites la escena ni los scripts de otro minijuego.
- **Antes de empezar:** `git pull` (o **Fetch origin > Pull origin** en GitHub Desktop).
- **Al terminar:** `git add`, `git commit` y `git push` (o **Commit to main > Push origin**).
- **No subas archivos desde la web de GitHub:** se salta las subcarpetas y deja todo en la raíz.
- Sube siempre los archivos **`.meta`** junto a sus archivos. Nunca subas `Library`, `Temp`, `Logs`, `Build` ni `.apk` (el `.gitignore` ya los excluye).

## Si algo falla

| Problema | Solución |
| --- | --- |
| Unity avisa de que la versión es distinta | Instala **6000.6.6f1** desde Unity Hub y abre el proyecto con ella. |
| No aparece el menú **Platybytes** o hay errores de `InputSystem` / `TMPro` | Abre **Window > General > Console**. Si faltan paquetes, comprueba que la carpeta `Packages` está en el repositorio. |
| Sale «Missing script» | Faltan archivos `.meta`. Haz `git pull` de nuevo y, en Rescata la llave, borra la escena y créala otra vez con el menú **Platybytes**. |
| La bola del laberinto va al revés en el móvil | En `RescataLaLlave/Scripts/RescataLaLlave.cs`, pon `InvertirX` o `InvertirY` a `true`. |
| Error al hacer `git push` porque hay cambios nuevos | Haz `git pull` primero y después `git push`. |
