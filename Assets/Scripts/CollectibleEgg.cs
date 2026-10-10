using UnityEngine;

public class CollectibleEgg : MonoBehaviour
{
    [Header("Configuración del Coleccionable")]
    [Tooltip("Cantidad de puntos que otorga este huevo.")]
    [SerializeField] private int scoreValue = 100;

    [Header("Efectos")]
    [Tooltip("Prefab del efecto de partículas a instanciar al recolectar.")]
    [SerializeField] private GameObject collectEffectPrefab;

    [Tooltip("Clip de sonido opcional al recolectar.")]
    [SerializeField] private AudioClip collectSound;

    private bool isCollected = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Evita doble recolección accidental en el mismo frame
        if (isCollected) return;

        // Comprueba si el objeto que ingresó tiene la etiqueta 'Player'
        if (collision.CompareTag("Player"))
        {
            isCollected = true;

            // 1. Sumar puntos en el GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(scoreValue);
            }
            else
            {
                Debug.LogWarning("CollectibleEgg: No se encontró una instancia de GameManager en la escena.");
            }

            // 2. Instanciar efecto de partículas
            if (collectEffectPrefab != null)
            {
                Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
            }

            // (Opcional) Reproducir sonido si está asignado
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            // 3. Destruir el huevo coleccionable
            Destroy(gameObject);
        }
    }
}
