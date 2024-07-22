using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class CameraCanvasSetup : MonoBehaviour
{
   //버튼 클릭시 씬 이동
   public void StateButtonClick()
   {
      SceneManager.LoadScene("State");
      Debug.Log("State");
   }
   public void NextButtonClick()
   {
      SceneManager.LoadScene("select");
      Debug.Log("select");
   }

   public void ControllButtonClick()
   {
      SceneManager.LoadScene("Controll");
      Debug.Log("Controll");
   }
}
