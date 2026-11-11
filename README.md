# Angry Birds 2D — Proyecto MCP (Unity 6000.6.0f1)

Nivel tipo Angry Birds hecho en Unity 2D (URP) con **arte procedural**: los sprites
se generan por código, no hay imágenes externas.

## Cómo jugar

- Abre `Assets/Scenes/AngryBirds.unity` y dale Play, o ejecuta
  `Builds/Windows/AngryBirds2D.exe`.
- Arrastra el ave desde la resortera y suéltala para disparar (se ve la trayectoria).
- `R` reinicia el nivel.
- 3 aves, 3 cerdos. Madera resiste golpes medios, el hielo es frágil,
  la piedra aguanta más.

## Estructura

```
Assets/
  AngryBirds/
    Scripts/   GameManager, Slingshot, Bird, Pig, Destructible, Pop, FollowCamera, PointerInput
    Sprites/   PNGs generados (+ material unlit y material de física)
  Editor/
    AngryBirdsBuilder.cs   Genera arte + escena + ejecutable (menú "AngryBirds")
    ABRepair.cs            Reimporta una vez los assets si Unity marca guid mismatch
  Scenes/
    AngryBirds.unity       Nivel
Builds/Windows/            Ejecutable compilado
```

## Regenerar todo

Menú **AngryBirds → 3. Todo (arte + escena + ejecutable)**, o en batch:

```
Unity.exe -batchmode -quit -projectPath "C:\Users\CA3-SEP\Desktop\GAMES\MCP" ^
  -executeMethod AngryBirdsBuilder.BuildAll -logFile build.log
```

## Control de versiones (Notion – sección 1)

- `Asset Serialization = Force Text` (ya configurado en ProjectSettings/EditorSettings.asset).
- Se conservan los archivos `.meta` y se versionan siempre.
- `.gitignore` oficial de Unity (Assets, Packages y ProjectSettings sí se suben).
- Ramas: `main` = versión estable. Trabaja en ramas para experimentos.
- Flujo: `git add .` → `git commit -m "..."` → `git push`.

## MCP for Unity (Notion – secciones 2 a 4)

Paquete instalado en `Packages/manifest.json`:
`https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`

Requisitos ya cubiertos en esta máquina: Git, Python 3.14 y `uv` (0.13.7).

Pasos que se hacen desde el editor (interfaz, no por archivo):

1. `Window → MCP for Unity`.
2. **Auto-Setup** y elegir el cliente MCP (VS Code, Claude Code u OpenClaw).
3. Click en **Start Bridge** hasta ver *Connected*.
4. En el cliente, pedir por ejemplo:
   *"Usa MCP para inspeccionar la escena activa: qué GameObjects tiene y si hay errores en consola. No modifiques nada."*

Notas:
- Cada cambio hecho por la IA vía MCP **no** se commitea solo: revisa y haz commit tú.
- Trabaja en una rama y con una versión estable guardada antes de pedir cambios grandes.
