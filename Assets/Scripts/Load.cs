using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Load : MonoBehaviour
{
    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene("Level_1");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
