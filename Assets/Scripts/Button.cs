using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public struct LevelData
{
    public int level;
    public int width;
    public int height;
    public int moveCount;
    public string colors;
}

public class Button : MonoBehaviour
{
    public const int OFFLINE_LEVEL_AMOUNT = 10;
    public const int ONLINE_LEVEL_AMOUNT = 15;
    public const int APPROX_MIN_FILE_SIZE = 30;
    public const int APPROX_MAX_FILE_SIZE = 300;

    public bool onlineDownloaded = false;
    public bool problemOccuredDownloading = true;
    public Text levelsText;

    public int frameCounter;

    public LevelData levelData;

    // Start is called before the first frame update
    void Start()
    {
        levelData = new LevelData();
        CheckIfOnlineDownloaded();
        levelsText = GameObject.Find("LevelsText").GetComponent<Text>();
    }

    private void OnMouseDown()
    {
        int level;
        bool isLoadable = false;
        if (int.TryParse(levelsText.text, out level))
        {
            if (level < 1 || level > OFFLINE_LEVEL_AMOUNT + ONLINE_LEVEL_AMOUNT)
            {
                return;
            }

            if (level < OFFLINE_LEVEL_AMOUNT)
            {
                isLoadable = GetLevelDataOffline(level);
            }
            else if (onlineDownloaded)
            {
                Debug.Log("online downloaded");
                isLoadable = GetLevelDataOnline(level);
            }
            if (levelData.level == level && isLoadable)
            {
                PlayerPrefs.SetInt("levelNumber", levelData.level);
                PlayerPrefs.SetInt("boardHeight", levelData.height);
                PlayerPrefs.SetInt("boardWidth", levelData.width);
                PlayerPrefs.SetInt("moveCount", levelData.moveCount);
                PlayerPrefs.SetString("initialColors", levelData.colors);
                PlayerPrefs.SetInt("highscore", PlayerPrefs.GetInt("highscore" + level, 0));
                SceneManager.LoadScene("MainScene");
            }
        }
    }

    void CheckIfOnlineDownloaded()
    {
        string code;
        for (int i = 0; i < ONLINE_LEVEL_AMOUNT; i++)
        {
            if (i < 5)
            {
                code = "A1" + (i + 1);
            }
            else
            {
                code = "B" + (i - 4);
            }
            string filePath = Path.Combine(Application.persistentDataPath, code + ".txt");
            if (!File.Exists(filePath))
            {
                onlineDownloaded = false;
                return;
            }
        }
    }

    bool GetLevelDataOnline(int level)
    {
        string code;
        if (level <= 15)
        {
            code = "A1" + (level - 10);
        }
        else
        {
            code = "B" + (level - 15);
        }
        string filePath = Path.Combine(Application.persistentDataPath, code + ".txt");
        if (File.Exists(filePath))
        {
            Debug.Log("File found");
            using (StreamReader sr = new StreamReader(filePath))
            {
                string data = sr.ReadToEnd();
                Debug.Log("data: \n" + data);
                return GetLevelDataFromText(data);
            }
        }
        return false;
    }

    bool GetLevelDataOffline(int level)
    {
        TextAsset text = null;
        text = Resources.Load<TextAsset>("RM_A" + level) as TextAsset;
        if (text != null)
        {
            return GetLevelDataFromText(text.text);
        }
        return false;
    }

    bool GetLevelDataFromText(string text)
    {
        int indexStart;
        int indexEnd;
        int colorIndex = 0;
        string levelStr = "level_number: ";
        string widthStr = "grid_width: ";
        string heightStr = "grid_height: ";
        string moveStr = "move_count: ";
        string gridStr = "grid: ";
        indexStart = text.IndexOf(levelStr);
        indexStart += levelStr.Length;
        indexEnd = text.IndexOf("\n");
        levelData.level = Int32.Parse(text.Substring(indexStart, indexEnd - indexStart));
        indexStart = text.IndexOf(widthStr);
        indexStart += widthStr.Length;
        indexEnd = text.IndexOf("\n", indexEnd + 1);
        levelData.width = Int32.Parse(text.Substring(indexStart, indexEnd - indexStart));
        indexStart = text.IndexOf(heightStr);
        indexStart += heightStr.Length;
        indexEnd = text.IndexOf("\n", indexEnd + 1);
        levelData.height = Int32.Parse(text.Substring(indexStart, indexEnd - indexStart));
        indexStart = text.IndexOf(moveStr);
        indexStart += moveStr.Length;
        indexEnd = text.IndexOf("\n", indexEnd + 1);
        levelData.moveCount = Int32.Parse(text.Substring(indexStart, indexEnd - indexStart));
        indexStart = text.IndexOf(gridStr);
        indexStart += gridStr.Length;
        indexEnd = text.Length;

        levelData.colors = "";
        while ((indexStart < indexEnd) && (colorIndex < (levelData.width * levelData.height)))
        {
            char c = text[indexStart];
            if (c == 'r')
            {
                levelData.colors += 'r';
                colorIndex++;
            }
            else if (c == 'g')
            {
                levelData.colors += 'g';
                colorIndex++;
            }
            else if (c == 'b')
            {
                levelData.colors += 'b';
                colorIndex++;
            }
            else if  (c == 'y')
            {
                levelData.colors += 'y';
                colorIndex++;
            }
            indexStart++;
        }

        if (colorIndex != (levelData.width * levelData.height))
        {
            return false;
        }
        return true;
    }

    // Update is called once per frame
    void Update()
    {
        if (frameCounter % 600 == 0 && !onlineDownloaded)
        {
            if (!problemOccuredDownloading)
            {
                onlineDownloaded = true;
            }
            else
            {
                StartCoroutine(GetRequest("https://row-match.s3.amazonaws.com/levels/RM_A1"));
            }
        }
        frameCounter++;
    }

    IEnumerator GetRequest(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (!webRequest.isNetworkError)
            {
                StartDownloading();
            }
        }
    }

    void StartDownloading()
    {
        problemOccuredDownloading = false;
        Debug.Log("Starting download");
        for (int i = 0; i < ONLINE_LEVEL_AMOUNT; i++)
        {
            if (i < 5)
            {
                StartCoroutine(RequestDownload("https://row-match.s3.amazonaws.com/levels/RM_A1" + (i + 1)));
            }
            else 
            {
                StartCoroutine(RequestDownload("https://row-match.s3.amazonaws.com/levels/RM_B" + (i - 4)));
            }
        }
    }

    IEnumerator RequestDownload(string url)
    {
        var request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();
        var data = request.downloadHandler.text;

        if (url[url.Length - 2] != '1')
        {
            ResponseCallback(data, url.Substring(url.Length - 2));
        }
        else
        {
            ResponseCallback(data, url.Substring(url.Length - 3));
        }
    }

    // write found file to persistent datapath
    void ResponseCallback(string data, string code)
    {
        if (data.Length < APPROX_MIN_FILE_SIZE || data.Length > APPROX_MAX_FILE_SIZE)
        {
            Debug.Log("problem occured downloading");
            problemOccuredDownloading = true;
            return;
        }
        string filePath = Path.Combine(Application.persistentDataPath, code + ".txt");
        try
        {
            File.WriteAllText(filePath, data);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
}
