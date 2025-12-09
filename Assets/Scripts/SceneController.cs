using System.ComponentModel.Design;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public int[] players;
    public int count;
    //[SerializeField] private Scene scene;

    public static SceneController instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        count = 0;
    }

    // Update is called once per frame
    private void Update()
    {

    }

    public void ToCharacterMenu(int num)
    {
        players = new int[num];
        SceneManager.LoadSceneAsync(1);
        //SceneManager.LoadScene("CharacterMenu");
    }

    public void AddCharacter(int id)
    {
        players[count] = id;
        count++;
        if (count == players.Length)
        {
            ToMapMenu();
        }
    }

    public void ToMapMenu()
    {
        SceneManager.LoadSceneAsync(2);
        //SceneManager.LoadScene("MapMenu");

    }

    public void ToMap(string name)
    {
        SceneManager.LoadScene(name);
    }

    public void ToTitle()
    {
        SceneManager.LoadSceneAsync(0);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game!");
        Application.Quit();
    }
}
