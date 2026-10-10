using UnityEngine;

/// <summary>
/// Mantiene el fondo o capas de escenario siguiendo al jugador o a la cámara.
/// Sigue estrictamente las reglas de rendimiento de AGENTS.md (sin GetComponent ni Find en Update).
/// </summary>
public class BackgroundFollow : MonoBehaviour
{
    [Header("Objetivo a Seguir")]
    [Tooltip("Transform de Chipa o de la Cámara principal. Si se deja vacío se busca una sola vez en Start().")]
    [SerializeField] private Transform objetivo;

    [Header("Efecto de Seguimiento / Parallax")]
    [Tooltip("1 = Sigue 1:1 al objetivo. Valores menores (ej. 0.8) crean efecto de profundidad.")]
    [Range(0f, 1f)]
    [SerializeField] private float factorSeguimientoX = 1f;

    [Tooltip("1 = Sigue 1:1 en el eje vertical. 0 = Fondo fijo verticalmente.")]
    [Range(0f, 1f)]
    [SerializeField] private float factorSeguimientoY = 1f;

    [Tooltip("Desplazamiento inicial del fondo respecto al objetivo.")]
    [SerializeField] private Vector2 offset = Vector2.zero;

    private Vector3 posicionInicialFondo;
    private Vector3 posicionInicialObjetivo;
    private float posicionZOriginal;

    private void Start()
    {
        posicionZOriginal = transform.position.z;
        posicionInicialFondo = transform.position;

        // Se busca el objetivo únicamente una vez en Start (permitido por AGENTS.md)
        if (objetivo == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                objetivo = player.transform;
            }
            else if (Camera.main != null)
            {
                objetivo = Camera.main.transform;
            }
        }

        if (objetivo != null)
        {
            posicionInicialObjetivo = objetivo.position;
        }
    }

    private void LateUpdate()
    {
        if (objetivo == null) return;

        // Desplazamiento acumulado del objetivo desde el inicio
        Vector3 desplazamiento = objetivo.position - posicionInicialObjetivo;

        // Nueva posición del fondo calculada según los factores de seguimiento
        float nuevoX = posicionInicialFondo.x + (desplazamiento.x * factorSeguimientoX) + offset.x;
        float nuevoY = posicionInicialFondo.y + (desplazamiento.y * factorSeguimientoY) + offset.y;

        transform.position = new Vector3(nuevoX, nuevoY, posicionZOriginal);
    }
}
