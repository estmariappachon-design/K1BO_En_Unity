using UnityEngine;

public class ExitGame : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Si ESC se usó para cerrar Ajustes o hay un video en curso, no se sale del juego
            if (MainMenuManager.EscapeOcupado())
                return;

            Application.Quit();
        }
    }
}
