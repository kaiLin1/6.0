using UnityEngine;
using UnityEngine.UI;

public class Button_ExitGame : MonoBehaviour
{
    int state;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(Quit);
    }

    void Quit()
    {
        if (state == 1)
        {
            return;
        }
        else
        {
            state = 1;
        }
        Application.Quit();
    }
}
