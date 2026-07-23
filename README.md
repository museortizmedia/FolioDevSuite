# 🦊 Folio DevSuite | Indie Project Manager Suite for Unity

![Unity](https://img.shields.io/badge/Unity-6.0%2B-blue?logo=unity)
![License](https://img.shields.io/badge/License-MIT-green)
![Status](https://img.shields.io/badge/Status-Active_Development-orange)
![Version](https://img.shields.io/badge/V-1.0.2-red)

**Folio** es una suite integral de herramientas integradas directamente en el editor de Unity, diseñada para organizar, documentar y gestional el flujo de trabajo de proyectos de videojuegos de manera transversal. 

Centraliza la gestión del proyecto mediante **tres módulos especializados** liderados por su panel principal (**Folio**), permitiendo a desarrolladores e indies tener un control claro de sus entregables, estructura y documentación sin salir de Unity.

## 🌟 Arquitectura de la Suite

Folio coordina 3 herramientas clave inspiradas en la naturaleza:
```
                       ┌─────────────────────────────────┐
                       │          🦊 FOLIO              │
                       │      (Dashboard Central)        │
                       └────────────────┌────────────────┘
                                        │
  ┌─────────────────────────────────────┼──────────────────────────────┐
  ▼                                     ▼                              ▼
  ┌─────────────┐                ┌─────────────┐                ┌─────────────┐
  │   🐻 KODA  │                 │  🦎 FLUX   │                │  🐝 NEXO   │
  │ (Docs Live) │                │ (Folders)   │                │ (Tasks)     │
  └─────────────┘                └─────────────┘                └─────────────┘
```

## 🐻 1. Koda (Documentación Viva & Transversal)
**Koda (El Oso)** administra la documentación modular directamente en Unity mediante Markdown, permitiendo vincular datos del proyecto en código mediante variables en tiempo real.

* **Markdown Editor & Preview:** Visualizador interno con soporte para modo claro/oscuro, tablas, imágenes y bloques de código.
* **Documentación Viva:** Declara variables y las interpreta adentro de Unity, listo para ser usado en tiempo de ejecución.
  * **Variables Locales y Vinculadas:** Soporta evaluación de variables locales (`$[Variable]`).
  * **Variables Locales y Vinculadas:** Soporta evaluación de variables inter-documentos (`$$[Variable]`).
* **Exportación:** Une múltiples documentos modulares y exporta a un único compilado `.md`.

## 🦎 2. Flux (Diseñador & Validador de Carpetas)
**Flux (El Camaleón)** se encarga de la jerarquía, orden estético y validación de la estructura del proyecto en Unity.

* **Estructura Personalizada:** Permite diseñar la arquitectura de carpetas desde una interfaz visual intuitiva.
* **Colorización de Carpetas:** Asigna descripciones y colores a directorios clave en la ventana *Project* para mejorar la navegabilidad visual.
* **Sistemas de Reglas y Validación:** Configura restricciones de extensiones permitidas por carpeta para mantener el proyecto limpio de assets desordenados.
* **Plantillas:** Exporta e importa estructuras organizacionales mediante archivos `.json`.

---

## 🐝 3. Nexo (Task Manager & Control de Módulos)
**Nexo (La Abeja)** es el gestor de tareas enfocado en el desarrollo ágil dentro del editor.

* **Módulos de Software:** Agrupa funcionalidades del juego por módulos independientes.
* **Control de Tareas:** Estados (`Por Hacer`, `En Proceso`, `Completado`), progreso porcentual (`0% - 100%`) y asignación de responsables.
* **Gestión Multiusuario:** Asignación de roles y miembros del equipo.
* **Métricas de Avance:** Cálculo automático de métricas de avance global y por módulo.

---

## 🦊 Dashboard Central: Folio
El panel unificado que consolida toda la información en una sola vista de alto nivel:

* **Métricas Globales:** Porcentaje de avance total del desarrollo y listado de módulos completados.
* **Filtros Avanzados:** Filtrado rápido de tareas por roles, responsables y estado actual.
* **Notas Rápidas:** Bloc de notas integrado dentro del editor para ideas o anotaciones temporales.
* **Lector Transversal:** Panel lateral derecho para consultar la documentación de **Koda** de forma rápida sin interrumpir el flujo de trabajo.

---

## 📂 Estructura del Repositorio

```text
Assets/
└── Folio/
    ├── Editor/
    │   ├── Utils/
    │   │   └── MarkdownRenderer.cs      # Motor de renderizado Markdown & RichText
    │   └── Windows/
    │       ├── ProjectManagerWindow.cs  # Dashboard Principal (Folio)
    │       ├── FolderDesigner/          # Módulo Flux
    │       ├── TaskManager/             # Módulo Nexo
    │       └── TransversalDocs/         # Módulo Koda
    ├── Generated/
    │   └── DocVariables.cs              # Script C# generado automáticamente por Koda
    └── Resources/                        # Configuración por defecto, bases de datos y assets
```
---

## 🚀 Instalación y Uso
1. Abre **Package Manager** de tu Unity Editor
2. Presiona `Install package from git URL`
3. Ingresa `https://github.com/museortizmedia/FolioDevSuite.git`
4. Espera que se instale el paquete.
5. Abre la suite desde el menú superior de Unity:

* Window > Folio > 🦎 Flux: Folder Designer

* Window > Folio > 🐝 Nexo: Task Manager

* Window > Folio > 🐻 Koda: Docs Manager

* Window > Folio > 🦊 Folio: Dashboard Dev Suite

### 📝 Ejemplo de Variables Vivas con Koda
Declaración dentro de cualquier archivo .md en la carpeta de documentación:

```text
<!-- DOCVARS
name: PlayerSpeed; type: Float; value: 7.5
name: GameTitle; type: String; value: "Super Space Adventure"
DOCVARS -->

# Configuración de $[GameTitle]
La velocidad actual del jugador es $[PlayerSpeed].
```

Koda compilará automáticamente estas variables en `Assets/Folio/Generated/DocVariables.cs`:
```text
void Start()
{
    Debug.Log($"Iniciando {DocVariables.GameTitle} a velocidad {DocVariables.PlayerSpeed}");
}
```

## 📄 Licencia
Desarrollado con ❤️ por [Diego Ortiz](https://museortizmedia.github.io/) (MuseOrtiz). Licencia MIT.