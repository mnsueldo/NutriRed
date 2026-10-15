# Plan de Implementación: Armado Inteligente de Kits (Plantillas, Auto-Sustitución y Balance Bromatológico)

> **Para Pair-Programming y Ejecución:** Seguimiento paso a paso según las reglas pedagógicas del Tutor C# .NET y la metodología MVC de NutriRed. Pasos con casillas de verificación (`- [ ]`).

**Meta:** Implementar el configurador administrativo de plantillas de recetas de kits, la auto-sustitución inteligente en 1 clic para quiebres de stock y el indicador visual de balance bromatológico en tiempo real.

**Arquitectura:**
1. **Frontend / Razor:** Enriquecer `Armar.cshtml` con el botón de auto-sustitución 1-clic y el componente de distribución bromatológica porcentual interactivo.
2. **Servicios / DTOs:** Extender `IPaqueteService` y `PaqueteService` con métodos de lectura y actualización transaccional de `TipoPaquete` y sus `PlantillaPaqueteDetalles`.
3. **Controladores y Vistas:** Implementar en `PaquetesController` las acciones para listar (`/Paquetes/Plantillas`) y editar (`/Paquetes/EditarPlantilla/{id}`) las recetas oficiales de cada franja familiar.

**Stack Tecnológico:** C# .NET 9, ASP.NET Core MVC, Entity Framework Core, PostgreSQL en Supabase, Bootstrap 5 + Bootstrap Icons.

---

## Restricciones y Buenas Prácticas Globales
- **Seguridad en Base de Datos:** Cero operaciones destructivas (`RemoveRange`). Las plantillas se actualizan de forma segura sin tocar lotes, donaciones ni entregas.
- **Validación Cliente/Servidor:** `ModelState.IsValid`, antiforgery tokens `[ValidateAntiForgeryToken]`, y scripts de validación `_ValidationScriptsPartial`.
- **Estilo y Accesibilidad:** Iconos consistentes de Bootstrap (`bi`), soporte nativo para Modo Oscuro y Alto Contraste, sin emojis en controles desplegables.

---

## Desglose de Tareas

### Tarea 1: Auto-Sustitución Inteligente en 1 Clic (Frontend)
- [ ] Agregar el botón de auto-sustitución en la tarjeta de advertencia de faltantes de `Armar.cshtml`.
- [ ] Implementar la función JavaScript `autoSustituirFaltantes()`:
  - Detectar alimentos de la receta con `faltante > 0`.
  - Buscar en `catalogoProductos` productos con stock disponible, priorizando misma categoría nutricional (`categoriaId`) y luego categorías afines.
  - Asignar el sustituto en el selector correspondiente, actualizar los inputs ocultos del model binder y actualizar visualmente el nombre con la etiqueta de sustituto.
  - Disparar el recálculo dinámico de métricas y balance.
- [ ] Probar la auto-sustitución en el navegador con un kit que tenga stock incompleto.

### Tarea 2: Indicador y Semáforo de Balance Bromatológico (Frontend)
- [ ] Diseñar el componente de barra segmentada en el panel de composición de `Armar.cshtml`:
  - Carbohidratos (Granos y Pastas): Azul `#0284c7`
  - Proteínas / Lácteos y Legumbres: Verde `#059669`
  - Conservas y Enlatados: Ámbar `#d97706`
  - Grasas, Aceites e Infusiones: Púrpura `#7c3aed`
- [ ] Crear la lógica JavaScript en `actualizarMetricasKit()` para calcular en vivo:
  - Porcentaje de representación de cada grupo nutricional.
  - Validación del semáforo bromatológico (Alerta si falta grupo proteico o base de cereales).
- [ ] Integrar el recálculo al agregar alimentos manuales, quitarlos o sustituirlos.

### Tarea 3: Capa de Negocio para el Gestor de Plantillas (Backend)
- [ ] Crear los DTOs y ViewModels de Plantillas:
  - `EditarPlantillaViewModel` (Id, Nombre, Descripción, MinIntegrantes, MaxIntegrantes, Items con ProductoId, CantidadRequerida).
- [ ] Extender `IPaqueteService` y `PaqueteService`:
  - `ObtenerTipoPaquetePorIdAsync(int id)`
  - `ActualizarPlantillaAsync(EditarPlantillaViewModel model)`
- [ ] Agregar acciones en `PaquetesController`:
  - `GET /Paquetes/Plantillas`: Listado visual de las recetas oficiales.
  - `GET /Paquetes/EditarPlantilla/{id}`: Formulario de edición de receta.
  - `POST /Paquetes/EditarPlantilla`: Guardado defensivo y transaccional de los ítems de la plantilla.

### Tarea 4: Vistas Razor para Plantillas de Kits (Interfaz de Usuario)
- [ ] Crear la vista `Plantillas.cshtml`:
  - Tarjetas comparativas de las 3 franjas familiares (*1-2, 3-5, 6+*).
  - Listado de alimentos requeridos, métricas de unidades totales por kit y botón de acceso rápido para "Editar Receta".
- [ ] Crear la vista `EditarPlantilla.cshtml`:
  - Modificación de rangos familiares y descripción.
  - Tabla dinámica para cambiar cantidades requeridas y quitar alimentos con confirmación.
  - Selector para incorporar nuevos productos del catálogo a la plantilla oficial.
- [ ] Enlazar `Plantillas` en el menú superior del Navbar (`_Layout.cshtml`).

### Tarea 5: Verificación Integral y Pruebas
- [ ] Compilar la solución con `dotnet build` verificando 0 errores.
- [ ] Probar la navegación completa:
  - Modificar una plantilla (ej. sumar 1 unidad de leche al kit familiar).
  - Ir a "Armar Kit" para esa familia y verificar que la propuesta refleje inmediatamente la receta actualizada.
  - Probar la auto-sustitución en 1 clic y comprobar que la barra de balance bromatológico responda en vivo.
