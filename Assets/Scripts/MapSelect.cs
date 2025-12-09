using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.Android;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;

public class MapSelect : MonoBehaviour
{
    [SerializeField] private int n;
    public TextMeshProUGUI promptText;
    public Button button1;
    public Button button2;
    public TextMeshProUGUI buttonText1;
    public TextMeshProUGUI buttonText2;
    public Image preview1;
    public Image preview2;
    public Sprite map1;
    public Sprite map2;
    //public Sprite map3;
    //public Sprite map4;
    //public Sprite map5;
    //public Sprite map6;
    

    private void Awake()
    {
        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        var controller = SceneController.instance;
        n = controller.count;
        promptText.text = $"{n}P Maps:";
        preview1.sprite = map1;
        preview2.sprite = map2;

        if (n == 2)
        {
            button1.onClick.AddListener(() => controller.ToMap("2p Map 1"));
            button2.onClick.AddListener(() => controller.ToMap("2p Map 2"));
            buttonText1.text = "Lockout";
            buttonText2.text = "Gemini";
        }
        else if (n == 3)
        {
            button1.onClick.AddListener(() => controller.ToMap("3p Map 1"));
            button2.onClick.AddListener(() => controller.ToMap("3p Map 2"));
            buttonText1.text = "Villa";
            buttonText2.text = "Battle Creek";
        }
        else
        {
            button1.onClick.AddListener(() => controller.ToMap("4p Map 1"));
            button2.onClick.AddListener(() => controller.ToMap("4p Map 2"));
            buttonText1.text = "Deadlock";
            buttonText2.text = "ARC";
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (n == 2)
        {
            preview1.sprite = map1;
            preview2.sprite = map2;
        }
        else if (n == 3)
        {
            preview1.sprite = map1;
            preview2.sprite = map2;
        }
        else
        {
            preview1.sprite = map1;
            preview2.sprite = map2;
        }
    }

}
