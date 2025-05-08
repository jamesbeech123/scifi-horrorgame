using UnityEngine.SceneManagement;
using UnityEngine;

public class LevelUIManager : UIManager
{
    public GameObject pauseMenu;

    override public void Update(){
       if(Input.GetKeyDown(KeyCode.Escape)){
            if(currentActiveMenu != null){
                Debug.Log("Closing menu from update");
                Time.timeScale = 1;
                CloseMenu();
            }else{
                Debug.Log("Opening menu from update");
                OpenMenu(pauseMenu);
            }
        }
    }


    public override void OpenMenu(GameObject menu)
    {
        Time.timeScale = 0;
        if (currentActiveMenu != null)
        {
            Destroy(currentActiveMenu);
        }
        currentActiveMenu = Instantiate(menu, canvas.transform);
        if(menu == pauseMenu){
            Debug.Log("Menu equaled pause menu");
            SetupPauseMenu();
        }else if(menu == settingsMenu){
            Debug.Log("Menu equaled settings menu");
            SetupSettingsMenu();
        }
    }

    public void SetupPauseMenu(){
        // Time.timeScale = 0;
        Debug.Log("Setting up pause menu");
        currentActiveMenu.transform.Find("ResumeButton").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ResumeGame);
        currentActiveMenu.transform.Find("RestartButton").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(RestartGame);
        currentActiveMenu.transform.Find("MainMenuButton").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(MainMenu);
        currentActiveMenu.transform.Find("QuitButton").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(QuitGame);
        currentActiveMenu.transform.Find("SettingsButton").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => OpenMenu(settingsMenu));
    }

    public void SetupSettingsMenu(){
        Debug.Log("Setting up settings menu");
        currentActiveMenu.transform.Find("BackButton").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(CloseMenu);
    }
    public void ResumeGame(){
        Time.timeScale = 1;
        CloseMenu();
    }

    public void RestartGame(){
        Time.timeScale = 1;
        CloseMenu();
        SceneManager.LoadScene("MazeLevel");
    }

    public void MainMenu(){
        Time.timeScale = 1;
        CloseMenu();
        SceneManager.LoadScene("MainMenu");
    }

}
