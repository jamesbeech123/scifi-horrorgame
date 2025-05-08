using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public GameObject settingsMenu, canvas;


    protected GameObject currentActiveMenu;

    virtual public void Update(){
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentActiveMenu != null)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu(settingsMenu);
            }
        }
    }


    virtual public void OpenMenu(GameObject menu)
    {
        if (currentActiveMenu != null)
        {
            Destroy(currentActiveMenu);
        }
        currentActiveMenu = Instantiate(menu, canvas.transform);
        currentActiveMenu.transform.Find("BackButton").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(CloseMenu);
    }

    public void CloseMenu()
    {
        if (currentActiveMenu != null)
        {
            Destroy(currentActiveMenu);
            currentActiveMenu = null;
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene("MazeLevel");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
