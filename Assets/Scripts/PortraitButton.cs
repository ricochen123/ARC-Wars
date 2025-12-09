using UnityEngine;

public class PortraitButton : MonoBehaviour
{
    public int commanderID; // 1=Brody, 2=Emry, 3=Four, 4=Rava

    private void OnMouseDown()
    {
        Debug.Log($"Clicked commander {commanderID}");
        //FindObjectOfType<CharacterSelect>().ChooseCommander(commanderID);
    }
}
