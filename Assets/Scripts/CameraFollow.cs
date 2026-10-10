using UnityEngine;

/// <summary>
/// Control de cámara suave para juegos de plataformas 2D estilo Pizza Tower / Chipa Unchained.
/// Sigue las directrices de rendimiento de AGENTS.md (cero Find o GetComponent en bucles).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("Transform del personaje (Chipa / Pollo). Si se deja vacío se busca automáticamente en Start.")]
    [SerializeField] private Transform objetivo;

    [Header("Ajustes de Seguimiento")]
    [Tooltip("Suavizado de la cámara. Menor número = cámara más rápida y reactiva.")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float tiempoSuavizado = 0.12f;

    [Tooltip("Desplazamiento de la cámara respecto al jugador. Ajusta Y para subir o bajar el encuadre.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.2f, -10f);

    [Header("Límites opcionales (Boundaries)")]
    [SerializeField] private bool usarLimites = false;
    [SerializeField] private float minX = -100f;
    [SerializeField] private float maxX = 100f;
    [SerializeField] private float minY = -10f;
    [SerializeField] private float maxY = 50f;

    private Vector3 velocidadActual = Vector3.zero;

    private void Start()
    {
        // Búsqueda única en Start() permitida por AGENTS.md
        if (objetivo == null)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null)
            {
                objetivo = jugador.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (objetivo == null) return;

        // Posición deseada calculada con el offset
        Vector3 posicionDeseada = objetivo.position + offset;
        posicionDeseada.z = offset.z; // Mantener siempre la profundidad Z fija (ej. -10)

        // Aplicar límites si están activados
        if (usarLimites)
        {
            posicionDeseada.x = Mathf.Clamp(posicionDeseada.x, minX, maxX);
            posicionDeseada.y = Mathf.Clamp(posicionDeseada.y, minY, maxY);
        }

        // Movimiento fluido mediante SmoothDamp (estándar para cámaras 2D de alta velocidad)
        transform.position = Vector3.SmoothDamp(transform.position, posicionDeseada, ref velocidadActual, tiempoSuavizado);
    }
}
