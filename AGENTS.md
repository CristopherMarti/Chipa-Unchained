# AGENTS.md - Directrices de Desarrollo para "CHIPA UNCHAINED"

## 1. CONTEXTO DEL PROYECTO
* **Nombre:** Chipa Unchained.
* **Motor:** Unity / C# / Físicas 2D (Rigidbody2D, Tilemaps, Cinemachine).
* **Género:** Plataformas 2D de acción rápida, arcade y comedia satírica.
* **Estilo y Atmósfera:** Pixel art estricto (Grid 32x32). Sátira industrial y corporativa (McNuggetCorp). Acción frenética, momentum y "Game Feel" fuertemente inspirados en *Pizza Tower*.

## 2. REGLAS ESTRICTAS DE PROGRAMACIÓN
* **Arquitectura (CRÍTICO):** Separar estrictamente la captura de entrada (`Input`), el controlador de físicas (cálculos en `FixedUpdate`) y la lógica visual/animaciones (cálculos en `Update`).
* **Rendimiento:** Prohibido usar `Find()`, `FindGameObjectWithTag()` o `GetComponent()` dentro de los métodos `Update()` o `FixedUpdate()`. Todas las referencias deben almacenarse en caché durante el `Awake()` o `Start()`.
* **Físicas:** El movimiento debe basarse en la manipulación de la velocidad del `Rigidbody2D` para mantener un control preciso del plataformeo.

## 3. CONTROLES Y MECÁNICAS PRINCIPALES
* **Movimiento:** Flechas direccionales.
* **Correr (Sprint/Momentum):** Mantener `Shift` + Flechas direccionales.
* **Salto:** Tecla `Z`.
* **Ataque / Embestida (Dash ofensivo):** Tecla `X`.
* **Parry (Rechazo/Defensa):** Tecla `C`.
* **Escalar Paredes (Wall Climb):** Mantener presionada la flecha direccional hacia la pared mientras el personaje está en estado de "Correr" (momentum). Actúa como una transición fluida que permite a Chipa subir verticalmente sin perder la inercia, idéntico a las mecánicas de *Pizza Tower*.