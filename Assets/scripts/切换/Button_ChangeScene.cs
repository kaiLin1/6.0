using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class Button_ChangeScene : MonoBehaviour
{
    public string SceneName;
    int state;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(ChangeScene);
    }

    void ChangeScene()
    {
        if (state == 1)
        {
            return;
        }
        else
        {
            state = 1;
        }
        SceneManager.LoadSceneAsync(SceneName);
    }
}
