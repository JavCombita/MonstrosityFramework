# 🦁 Monstrosity Framework

> **El Motor de Élite para Monstruos Personalizados en Stardew Valley.**

**Monstrosity Framework** es una infraestructura de alto nivel que permite a los modders agregar nuevos monstruos al juego utilizando simples archivos JSON y texturas. Maneja automáticamente la serialización compleja (SpaceCore), la sincronización multijugador y la inyección procedural en las minas.

¡Ya no necesitas ser un experto en C# para crear enemigos nuevos!

---

## 📦 Instalación

### Para Jugadores
1. Instala la última versión de **[SMAPI](https://smapi.io/)**.
2. Instala **[SpaceCore](https://www.nexusmods.com/stardewvalley/mods/1348)** (Requerido para que el juego pueda guardar la partida sin errores).
3. Descarga e instala **Monstrosity Framework** en tu carpeta `Mods`.
4. Instala cualquier **Content Pack** (Mod de Monstruos) que use este framework.

### Para Modders (Dependencias)
Si estás creando un mod, agrega esto a tu `manifest.json`:

```json
"Dependencies": [
   {
      "UniqueID": "TuNombre.MonstrosityFramework",
      "IsRequired": true
   }
]