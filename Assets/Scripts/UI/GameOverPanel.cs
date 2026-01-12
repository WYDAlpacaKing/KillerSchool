using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverPanel : MonoBehaviour
{
    public void RestartGame()
    {
        // 1. 恢复游戏
        Time.timeScale = 1f;

        // 2. 锁定鼠标并隐藏
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene("Game");
    }

    public void Exit()
    {
        //按钮按下后退出游戏
        Application.Quit();
    }
}
