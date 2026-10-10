using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI - Puntuación")]
    [Tooltip("Referencia al componente de texto de TextMeshPro para mostrar los puntos.")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Configuración")]
    [Tooltip("Puntos acumulados del jugador.")]
    [SerializeField] private int currentScore = 0;

    private void Awake()
    {
        // Configuración de patrón Singleton
        if (Instance == null)
        {
            Instance = this;
            // Descomenta la siguiente línea si deseas que persista entre cambio de escenas:
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        UpdateScoreUI();
    }

    /// <summary>
    /// Suma puntos al marcador global y refresca la interfaz visual.
    /// </summary>
    /// <param name="amount">Cantidad de puntos a sumar.</param>
    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreUI();
    }

    /// <summary>
    /// Actualiza el texto en pantalla usando TextMeshPro.
    /// </summary>
    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Puntos: {currentScore}";
        }
        else
        {
            Debug.LogWarning("GameManager: ¡No se ha asignado la referencia a Score Text en el Inspector!");
        }
    }

    /// <summary>
    /// Retorna la puntuación actual.
    /// </summary>
    public int GetScore()
    {
        return currentScore;
    }
}
