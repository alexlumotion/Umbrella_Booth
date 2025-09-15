using UnityEngine;
using UnityEngine.UI;

public class ToogleController : MonoBehaviour
{

    public string name1, name2, name3, name4, name5 = "";

    public Sprite[] spritesAnswers;
    public Image imageAnswer;

    public CsvLogManager csvLogManager;

    public void SetName01(string name) { name1 = name; }
    public void SetName02(string name) { name2 = name; }
    public void SetName03(string name) { name3 = name; }
    public void SetName04(string name) { name4 = name; }
    public void SetName05(string name) { name5 = name; }

    public void CheckValue()
    {
        // сценарій 1
        if (name1 == "A" &&
            (name2 == "A" || name2 == "D") &&
            name3 == "D" &&
            name4 == "A" &&
            name5 == "A")
        {
            Debug.Log("Сценарій 1");
            imageAnswer.sprite = spritesAnswers[0];
        }
        // сценарій 2
        else if (name1 == "A" &&
                 (name2 == "A" || name2 == "B") &&
                 name3 == "A" &&
                 name4 == "A" &&
                 name5 == "B")
        {
            Debug.Log("Сценарій 2");
            imageAnswer.sprite = spritesAnswers[1];
        }
        // сценарій 3
        else if ((name1 == "A" || name1 == "D") &&
                 (name2 == "B" || name2 == "C") &&
                 name3 == "C" &&
                 (name4 == "B" || name4 == "D") &&
                 (name5 == "B" || name5 == "D"))
        {
            Debug.Log("Сценарій 3");
            imageAnswer.sprite = spritesAnswers[2];
        }
        // сценарій 4
        else if (name1 == "D" &&
                 (name2 == "C" || name2 == "D") &&
                 (name3 == "C" || name3 == "D") &&
                 name4 == "D" &&
                 name5 == "D")
        {
            Debug.Log("Сценарій 4");
            imageAnswer.sprite = spritesAnswers[3];
        }
        // сценарій 5
        else if ((name1 == "A" || name1 == "B") &&
                 name2 == "A" &&
                 name3 == "A" &&
                 name4 == "B" &&
                 name5 == "A")
        {
            Debug.Log("Сценарій 5");
            imageAnswer.sprite = spritesAnswers[4];
        }
        // сценарій 6
        else if (name1 == "C" &&
                 name2 == "B" &&
                 name3 == "B" &&
                 name4 == "C" &&
                 name5 == "C")
        {
            Debug.Log("Сценарій 6");
            imageAnswer.sprite = spritesAnswers[5];
        }
        else if (name1 == "A" &&
                 name2 == "A" &&
                 name3 == "A" &&
                 name4 == "A" &&
                 name5 == "A")
        {
            Debug.Log("Сценарій 7");
            imageAnswer.sprite = spritesAnswers[6];
        }
        else if (name1 == "B" &&
                 name2 == "B" &&
                 name3 == "B" &&
                 name4 == "B" &&
                 name5 == "B")
        {
            Debug.Log("Сценарій 8");
            imageAnswer.sprite = spritesAnswers[7];
        }
        else if (name1 == "C" &&
                 name2 == "C" &&
                 name3 == "C" &&
                 name4 == "C" &&
                 name5 == "C")
        {
            Debug.Log("Сценарій 9");
            imageAnswer.sprite = spritesAnswers[8];
        }
        else if (name1 == "D" &&
                 name2 == "D" &&
                 name3 == "D" &&
                 name4 == "D" &&
                 name5 == "D")
        {
            Debug.Log("Сценарій 10");
            imageAnswer.sprite = spritesAnswers[9];
        }
        else
        {
            Debug.Log("Жоден сценарій не збігся");
            imageAnswer.sprite = spritesAnswers[Random.Range(0, 10)];
        }

        csvLogManager.Log(name1, name2, name3, name4, name5);

    }

}
