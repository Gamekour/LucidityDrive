using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadInterface : MonoBehaviour
{

    public void LoadScene(string name) => SceneManager.LoadScene(name);

    public void LoadScene(int buildIndex) => SceneManager.LoadScene(buildIndex);

    public void LoadNextScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1); 

    public void LoadPrevScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);

    public void ReloadScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

}
