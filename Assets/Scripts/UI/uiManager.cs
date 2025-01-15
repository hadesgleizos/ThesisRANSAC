using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class uiManager : MonoBehaviour
{
    public TextMeshProUGUI health, ammo, score;
    public GameObject[] weaponIndicator = new GameObject[2];

    // Start is called before the first frame update
    private void Start()
    {   }

    public void setHealth (string i){health.text = i;}
    public void setAmmo (string i){ammo.text = i;}
    public void setScore (string i){score.text = i;}

    public void setWeaponToDisplay (int e){
        for (int i = 0; i < weaponIndicator.Length; i++){
            weaponIndicator[i].SetActive(false);
        }
        for(int i = 0; i < weaponIndicator.Length; i++){
            if(i == e) weaponIndicator[i].SetActive(true);
        }
    }
}
