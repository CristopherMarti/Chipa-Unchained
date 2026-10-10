using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    [Tooltip("Nombre exacto de la escena del primer nivel a cargar.")]
    [SerializeField] private string nombreEscenaNivel = "Nivel01";

    /// <summary>
    /// Carga la escena del juego. Se vincula al botón 'Jugar' o 'Iniciar'.
    /// </summary>
    public void IniciarJuego()
    {
        SceneManager.LoadScene(nombreEscenaNivel);
    }

    /// <summary>
    /// Cierra la aplicación. Se vincula al botón 'Salir'.
    /// </summary>
    public void SalirJuego()
    {
        Debug.Log("Saliendo del juego...");

        // Cierra la aplicación en compilaciones ejecutables (.exe, etc.)
        Application.Quit();

#if UNITY_EDITOR
        // Detiene el modo Play si se está probando directamente dentro del Editor de Unity
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
