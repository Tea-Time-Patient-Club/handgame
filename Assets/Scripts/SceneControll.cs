using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UniqueClassName : MonoBehaviour
{
    private string previousScene;

    void Start()
    {
        // 현재 씬의 이름을 previousScene에 저장
        previousScene = SceneManager.GetActiveScene().name;
    }

    public void LoadScene(string sceneName)
    {
        // 현재 씬의 이름을 previousScene에 저장
        previousScene = SceneManager.GetActiveScene().name;
        // 새로운 씬 로드
        SceneManager.LoadScene(sceneName);
    }

    public void LoadPreviousScene()
    {
        // 이전 씬으로 이동
        if (!string.IsNullOrEmpty(previousScene))
        {
            SceneManager.LoadScene(previousScene);
        }
        else
        {
            Debug.LogWarning("No previous scene found!");
        }
    }

    
   //버튼 클릭시 씬 이동
   public void StateButtonClick()
   {
      SceneManager.LoadScene("State");
      Debug.Log("State");
   }
   public void SongSelectButtonClick()
   {
      SceneManager.LoadScene("select");
      Debug.Log("select");
   }

   public void ControllButtonClick()
   {
      SceneManager.LoadScene("Controll");
      Debug.Log("Controll");
   }
   public void PlayButtonClick()
   {
      SceneManager.LoadScene("Main");
      Debug.Log("Main");
   }
   public void EndGame()
   {
      SceneManager.LoadScene("Main");
      Debug.Log("Main");
   }
   public void MainPage()
   {
      SceneManager.LoadScene("Start");
      Debug.Log("Start");
   }
}
