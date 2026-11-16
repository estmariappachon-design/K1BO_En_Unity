using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject menuUI;
    public VideoPlayer videoPlayer;

    public void PlayGame()
    {
        menuUI.SetActive(false);

        videoPlayer.loopPointReached -= EndVideo;
        videoPlayer.loopPointReached += EndVideo;

        videoPlayer.Play();
    }

    void EndVideo(VideoPlayer vp)
    {
        SceneManager.LoadScene("MainScene");
    }
}