using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadInterface : MonoBehaviour
{
    private int currentIndex = 0;

    private void OnLevelWasLoaded(int level) => currentIndex = level;

    public void LoadScene(string name) => SceneManager.LoadScene(name);

    public void LoadScene(int buildIndex) => SceneManager.LoadScene(buildIndex);

    public void LoadNextScene() => SceneManager.LoadScene(currentIndex + 1);

    public void LoadPrevScene() => SceneManager.LoadScene(currentIndex - 1);

    public void ReloadScene() => SceneManager.LoadScene(currentIndex);

}
