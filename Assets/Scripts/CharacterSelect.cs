using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CharacterSelect : MonoBehaviour
{
    public int currentPlayer;
    public TextMeshProUGUI promptText;
    public GameObject clickedPortrait;



    private void Start()
    {
        currentPlayer = 1;
        if (promptText != null)
        {
            promptText.text = $"Player {currentPlayer}, choose your commander!";
        }
    }

    public void ChooseCommander(int commanderID)
    {
        var controller = SceneController.instance;
        controller.AddCharacter(commanderID);

       
        clickedPortrait = GameObject.Find($"Portrait_{commanderID}");
        if (clickedPortrait != null)
        {
            var sr = clickedPortrait.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = new Color(1, 1, 1, 0.3f); 
        }

        currentPlayer++;

        if (currentPlayer <= controller.players.Length)
        {
            promptText.text = $"Player {currentPlayer}, choose your commander!";
        }
        else
        {
            promptText.text = "Thank you! Loading...";
        }
    }
}
