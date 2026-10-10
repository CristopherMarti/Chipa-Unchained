using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controlador principal de Chipa ("Chipa Unchained").
/// Cumple estrictamente las reglas de AGENTS.md:
/// - Separación estricta: Input (Update), Físicas (FixedUpdate), Visuales (Update).
/// - Rendimiento: Prohibido GetComponent / Find en Update o FixedUpdate. Todo en Awake.
/// - Control arcade veloz con momentum estilo Pizza Tower: Correr (Shift), Salto (Z),
///   Ataque/Dash (X), Parry (C), y Escalar Paredes (Wall Climb al correr contra un muro).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ChipaController : MonoBehaviour
{
    public enum EstadoChipa
    {
        Normal,
        Atacando,
        Parry,
        WallClimb
    }

    [Header("Velocidades y Movimiento (Arcade Momentum)")]
    [Tooltip("Velocidad normal de caminar.")]
    [SerializeField] private float velocidadCaminar = 7f;

    [Tooltip("Velocidad al mantener Shift (Sprint / Mach Run).")]
    [SerializeField] private float velocidadSprint = 15f;

    [Tooltip("Velocidad vertical constante al escalar muros con momentum.")]
    [SerializeField] private float velocidadWallClimb = 12f;

    [Tooltip("Velocidad horizontal de la embestida / dash ofensivo (Tecla X).")]
    [SerializeField] private float velocidadAtaqueDash = 18f;

    [Header("Salto y Físicas")]
    [Tooltip("Fuerza vertical del salto normal.")]
    [SerializeField] private float fuerzaSalto = 13f;

    [Tooltip("Fuerza horizontal del salto rebotando desde la pared (Wall Jump).")]
    [SerializeField] private float fuerzaWallJumpX = 10f;

    [Tooltip("Radio de detección de suelo.")]
    [SerializeField] private float radioDeteccionSuelo = 0.25f;

    [Tooltip("Distancia del raycast hacia el frente para detectar paredes a escalar.")]
    [SerializeField] private float distanciaCheckPared = 0.5f;

    [Header("Tiempos de Habilidad")]
    [SerializeField] private float duracionAtaque = 0.25f;
    [SerializeField] private float duracionParry = 0.2f;

    [Header("Capas (Layers)")]
    [Tooltip("Capa de plataformas y terreno transitables.")]
    [SerializeField] private LayerMask capaSuelo;

    [Header("Referencias de Detección (Opcionales)")]
    [Tooltip("Punto de chequeo de suelo en los pies. Si es null se usa la base del collider.")]
    [SerializeField] private Transform controladorSuelo;

    // Componentes cacheados (Cero GetComponent en Update/FixedUpdate)
    private Rigidbody2D rb;
    private Collider2D col;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    // Estados
    private EstadoChipa estadoActual = EstadoChipa.Normal;
    private bool enSuelo = false;
    private bool tocandoPared = false;
    private bool mirandoDerecha = true;
    private float direccionCara = 1f; // 1 = derecha, -1 = izquierda

    // Entradas capturadas en Update
    private float inputX = 0f;
    private bool inputSprint = false;
    private bool deseaSaltar = false;
    private bool deseaAtacar = false;
    private bool deseaParry = false;

    // Temporizadores
    private float timerAtaque = 0f;
    private float timerParry = 0f;

    // Hashes de Animator optimizados (evita cadenas en tiempo de ejecución)
    private static readonly int AnimVelocidadX = Animator.StringToHash("VelocidadX");
    private static readonly int AnimVelocidadY = Animator.StringToHash("VelocidadY");
    private static readonly int AnimEnSuelo = Animator.StringToHash("EnSuelo");
    private static readonly int AnimCorriendo = Animator.StringToHash("Corriendo");
    private static readonly int AnimEscalando = Animator.StringToHash("Escalando");
    private static readonly int AnimAtacarTrigger = Animator.StringToHash("Atacar");
    private static readonly int AnimParryTrigger = Animator.StringToHash("Parry");

    // Banderas para solo enviar parámetros que existan en el Animator (evita warnings en consola)
    private bool tieneVelocidadX = false;
    private bool tieneVelocidadY = false;
    private bool tieneEnSuelo = false;
    private bool tieneCorriendo = false;
    private bool tieneEscalando = false;
    private bool tieneAtacar = false;
    private bool tieneParry = false;

    private void Awake()
    {
        // Cacheado estricto al inicio
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // EVITAR QUE EL PERSONAJE SE DE VUELTA O RUEDE:
        if (rb != null)
        {
            rb.freezeRotation = true; // Bloquea la rotación física en el eje Z
        }
        transform.rotation = Quaternion.identity; // Asegura que esté erguido

        // Si no se asignó capaSuelo, detectar automáticamente Suelo y Default
        if (capaSuelo.value == 0)
        {
            int mascara = LayerMask.GetMask("Suelo", "Default");
            capaSuelo = mascara != 0 ? mascara : ~0;
        }

        // Detectar qué parámetros existen realmente en el Animator para no saturar la consola
        if (anim != null)
        {
            foreach (var p in anim.parameters)
            {
                if (p.nameHash == AnimVelocidadX) tieneVelocidadX = true;
                if (p.nameHash == AnimVelocidadY) tieneVelocidadY = true;
                if (p.nameHash == AnimEnSuelo) tieneEnSuelo = true;
                if (p.nameHash == AnimCorriendo) tieneCorriendo = true;
                if (p.nameHash == AnimEscalando) tieneEscalando = true;
                if (p.nameHash == AnimAtacarTrigger) tieneAtacar = true;
                if (p.nameHash == AnimParryTrigger) tieneParry = true;
            }
        }
    }

    private void Update()
    {
        // =========================================================================
        // 1. CAPTURA DE ENTRADA (INPUT) SEGÚN AGENTS.MD
        // =========================================================================
#if ENABLE_INPUT_SYSTEM
        // Soporte nativo para el nuevo Input System (teclado y gamepad)
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float moveX = 0f;
            if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) moveX -= 1f;
            if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) moveX += 1f;
            inputX = moveX;

            inputSprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

            if (keyboard.zKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            {
                deseaSaltar = true;
            }

            if (keyboard.xKey.wasPressedThisFrame && estadoActual == EstadoChipa.Normal)
            {
                deseaAtacar = true;
            }

            if (keyboard.cKey.wasPressedThisFrame && estadoActual == EstadoChipa.Normal)
            {
                deseaParry = true;
            }
        }

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            float stickX = gamepad.leftStick.x.ReadValue();
            if (Mathf.Abs(stickX) > 0.2f) inputX = Mathf.Sign(stickX);
            if (gamepad.dpad.left.isPressed) inputX = -1f;
            if (gamepad.dpad.right.isPressed) inputX = 1f;

            if (gamepad.rightShoulder.isPressed || gamepad.rightTrigger.isPressed) inputSprint = true;
            if (gamepad.buttonSouth.wasPressedThisFrame) deseaSaltar = true;
            if (gamepad.buttonWest.wasPressedThisFrame && estadoActual == EstadoChipa.Normal) deseaAtacar = true;
            if (gamepad.buttonEast.wasPressedThisFrame && estadoActual == EstadoChipa.Normal) deseaParry = true;
        }
#else
        // Soporte para Input Manager clásico
        inputX = Input.GetAxisRaw("Horizontal");
        inputSprint = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Space))
        {
            deseaSaltar = true;
        }

        if (Input.GetKeyDown(KeyCode.X) && estadoActual == EstadoChipa.Normal)
        {
            deseaAtacar = true;
        }

        if (Input.GetKeyDown(KeyCode.C) && estadoActual == EstadoChipa.Normal)
        {
            deseaParry = true;
        }
#endif

        // Gestión de timers de habilidades
        ActualizarTimers();

        // =========================================================================
        // 2. LÓGICA VISUAL Y ANIMACIONES (UPDATE)
        // =========================================================================
        ActualizarVisuales();
    }

    private void FixedUpdate()
    {
        // =========================================================================
        // 3. FÍSICAS Y MANIPULACIÓN DE VELOCIDAD (FIXEDUPDATE)
        // =========================================================================
        ActualizarChecksFisicos();
        ProcesarMaquinaEstadosFisica();
    }

    /// <summary>
    /// Comprueba colisiones con suelo y paredes usando Overlap y Raycast con la capa Suelo.
    /// </summary>
    private void ActualizarChecksFisicos()
    {
        // Check de suelo
        Vector2 origenSuelo = controladorSuelo != null 
            ? (Vector2)controladorSuelo.position 
            : new Vector2(col.bounds.center.x, col.bounds.min.y);

        enSuelo = Physics2D.OverlapCircle(origenSuelo, radioDeteccionSuelo, capaSuelo);

        // Check de pared frontal (hacia donde se mira o camina)
        Vector2 centro = col.bounds.center;
        Vector2 direccionRayo = new Vector2(direccionCara, 0f);
        RaycastHit2D hitPared = Physics2D.Raycast(centro, direccionRayo, distanciaCheckPared, capaSuelo);

        tocandoPared = (hitPared.collider != null);
    }

    /// <summary>
    /// Gestiona los estados y la velocidad física del Rigidbody2D.
    /// </summary>
    private void ProcesarMaquinaEstadosFisica()
    {
        switch (estadoActual)
        {
            case EstadoChipa.Normal:
                ProcesarMovimientoNormal();
                break;

            case EstadoChipa.WallClimb:
                ProcesarWallClimb();
                break;

            case EstadoChipa.Atacando:
                ProcesarDashAtaque();
                break;

            case EstadoChipa.Parry:
                ProcesarParryFisico();
                break;
        }
    }

    private void ProcesarMovimientoNormal()
    {
        // Verificación de transición a Wall Climb (Regla: Correr + dirección hacia la pared)
        bool presionandoContraPared = (inputX != 0f && Mathf.Sign(inputX) == direccionCara);
        if (tocandoPared && inputSprint && presionandoContraPared)
        {
            estadoActual = EstadoChipa.WallClimb;
            deseaSaltar = false;
            return;
        }

        // Salto normal
        if (deseaSaltar)
        {
            deseaSaltar = false;
            if (enSuelo)
            {
                AsignarVelocidad(rb.linearVelocity.x, fuerzaSalto);
            }
        }

        // Activación de Ataque
        if (deseaAtacar)
        {
            deseaAtacar = false;
            estadoActual = EstadoChipa.Atacando;
            timerAtaque = duracionAtaque;
            if (anim != null && tieneAtacar) anim.SetTrigger(AnimAtacarTrigger);
            return;
        }

        // Activación de Parry
        if (deseaParry)
        {
            deseaParry = false;
            estadoActual = EstadoChipa.Parry;
            timerParry = duracionParry;
            if (anim != null && tieneParry) anim.SetTrigger(AnimParryTrigger);
            return;
        }

        // Cálculo de velocidad horizontal (Caminar vs Sprint/Momentum)
        float velocidadObjetivo = inputSprint ? velocidadSprint : velocidadCaminar;
        float velX = inputX * velocidadObjetivo;

        AsignarVelocidad(velX, rb.linearVelocity.y);
    }

    /// <summary>
    /// Escalar Paredes (Wall Climb) estilo Pizza Tower:
    /// Sube verticalmente a alta velocidad mientras se mantenga la carrera hacia la pared.
    /// Permite saltar (Wall Jump) despegándose de la pared.
    /// </summary>
    private void ProcesarWallClimb()
    {
        // Si presiona salto durante la escalada -> Wall Jump!
        if (deseaSaltar)
        {
            deseaSaltar = false;
            estadoActual = EstadoChipa.Normal;

            // Impulso hacia el lado contrario de la pared y hacia arriba
            float impulsoX = -direccionCara * fuerzaWallJumpX;
            direccionCara = -direccionCara;
            mirandoDerecha = (direccionCara > 0);
            AsignarVelocidad(impulsoX, fuerzaSalto);
            return;
        }

        bool siguePresionando = (inputX != 0f && Mathf.Sign(inputX) == direccionCara);

        // Si suelta Shift, suelta la flecha hacia el muro o se termina la pared:
        if (!inputSprint || !siguePresionando || !tocandoPared)
        {
            estadoActual = EstadoChipa.Normal;

            // Si llegó a la cima de la pared (ya no toca pared), pequeño impulso para montarse
            if (!tocandoPared)
            {
                AsignarVelocidad(direccionCara * velocidadSprint * 0.6f, fuerzaSalto * 0.5f);
            }
            return;
        }

        // Mantener subida vertical continua por la pared sin perder momentum
        AsignarVelocidad(0f, velocidadWallClimb);
    }

    private void ProcesarDashAtaque()
    {
        // Dash frontal a alta velocidad
        AsignarVelocidad(direccionCara * velocidadAtaqueDash, 0f);

        if (timerAtaque <= 0f)
        {
            estadoActual = EstadoChipa.Normal;
        }
    }

    private void ProcesarParryFisico()
    {
        // Breve detención o micro-retroceso en el parry
        AsignarVelocidad(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y * 0.5f);

        if (timerParry <= 0f)
        {
            estadoActual = EstadoChipa.Normal;
        }
    }

    private void AsignarVelocidad(float x, float y)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(x, y);
#else
        rb.velocity = new Vector2(x, y);
#endif
    }

    private void ActualizarTimers()
    {
        if (timerAtaque > 0f) timerAtaque -= Time.deltaTime;
        if (timerParry > 0f) timerParry -= Time.deltaTime;
    }

    /// <summary>
    /// Maneja el volteo del personaje y actualiza los parámetros del Animator.
    /// </summary>
    private void ActualizarVisuales()
    {
        // Orientación del sprite (no girar mientras escala o ataca)
        if (estadoActual == EstadoChipa.Normal)
        {
            if (inputX > 0.1f && !mirandoDerecha)
            {
                Voltear();
            }
            else if (inputX < -0.1f && mirandoDerecha)
            {
                Voltear();
            }
        }

        // Actualización segura de parámetros en Animator (solo los que existan creados)
        if (anim != null)
        {
            if (tieneVelocidadX) anim.SetFloat(AnimVelocidadX, Mathf.Abs(rb.linearVelocity.x));
            if (tieneVelocidadY) anim.SetFloat(AnimVelocidadY, rb.linearVelocity.y);
            if (tieneEnSuelo) anim.SetBool(AnimEnSuelo, enSuelo);
            if (tieneCorriendo) anim.SetBool(AnimCorriendo, inputSprint && Mathf.Abs(inputX) > 0.1f);
            if (tieneEscalando) anim.SetBool(AnimEscalando, estadoActual == EstadoChipa.WallClimb);
        }
    }

    private void Voltear()
    {
        mirandoDerecha = !mirandoDerecha;
        direccionCara = mirandoDerecha ? 1f : -1f;

        Vector3 escala = transform.localScale;
        escala.x = Mathf.Abs(escala.x) * direccionCara;
        transform.localScale = escala;
    }

    /// <summary>
    /// Dibuja los Gizmos en la vista Scene para calibrar suelo y pared fácilmente.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (col == null) col = GetComponent<Collider2D>();

        // Radio de suelo
        Gizmos.color = enSuelo ? Color.green : Color.red;
        Vector2 origenSuelo = controladorSuelo != null 
            ? (Vector2)controladorSuelo.position 
            : (col != null ? new Vector2(col.bounds.center.x, col.bounds.min.y) : (Vector2)transform.position);

        Gizmos.DrawWireSphere(origenSuelo, radioDeteccionSuelo);

        // Raycast de pared
        if (col != null)
        {
            Gizmos.color = tocandoPared ? Color.yellow : Color.blue;
            Vector2 origenPared = col.bounds.center;
            Gizmos.DrawLine(origenPared, origenPared + new Vector2(direccionCara * distanciaCheckPared, 0f));
        }
    }
}
