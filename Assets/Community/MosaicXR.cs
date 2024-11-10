using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;

[System.Serializable]
public class ImageResponse
{
    public List<string> images;
    public List<string> keys;
    public List<bool> stereos;
}

public class MosaicXR : MonoBehaviour
{
    [SerializeField] private List<Transform> imageSpawns; // List of empty GameObjects as spawn points

    [SerializeField] private List<Material> stereoImageMats;
    public GameObject imageCanvas; // Prefab to instantiate at each spawn point
    [SerializeField] private float checkInterval = 8f; // Check interval in seconds (modifiable in Inspector)
    
    private List<string> existingKeys = new List<string>(); // Track already downloaded keys

    private int stereoIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DownloadImages());
        StartCoroutine(CheckForMissingImages());
    }

    private IEnumerator DownloadImages()
    {
        int spawnCount = imageSpawns.Count;
        UnityWebRequest www = UnityWebRequest.Get($"https://kite-prompt-koi.ngrok-free.app/images/get-images-and-keys/{spawnCount}");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error downloading images: " + www.error);
            yield break;
        }

        ImageResponse imageResponse = JsonUtility.FromJson<ImageResponse>(www.downloadHandler.text);
        Debug.Log("Got Images");
        Debug.Log(www.downloadHandler.text);
        
        for (int i = 0; i < imageResponse.images.Count && i < imageSpawns.Count; i++)
        {
            byte[] imageBytes = System.Convert.FromBase64String(imageResponse.images[i]);
            bool isStereo = imageResponse.stereos[i];

            Texture2D texture = new Texture2D(2, 2);
            bool isLoaded = texture.LoadImage(imageBytes); // Check if loading the image was successful
            if (!isLoaded)
            {
                Debug.LogError("Failed to load texture from image data.");
            }


            GameObject newCanvas = Instantiate(imageCanvas, imageSpawns[i].position, imageSpawns[i].rotation);
            newCanvas.transform.localScale = imageSpawns[i].localScale;

            Renderer renderer = newCanvas.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (isStereo) {
                    Material mat = new Material(renderer.material); // Create a new material instance if necessary
                    mat.mainTexture = texture;
                    renderer.material = mat; // Assign the new material to the renderer
                    renderer.material.shader = Shader.Find("Stereoscopic/StereoImage_SideBySide2");
                }
                renderer.material.mainTexture = texture;
            }
            else
            {
                Debug.LogWarning("Renderer not found on imageCanvas prefab.");
            }

            existingKeys.Add(imageResponse.keys[i]); // Track downloaded keys
        }
    }

    private IEnumerator CheckForMissingImages()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            UnityWebRequest www = UnityWebRequest.Get("https://kite-prompt-koi.ngrok-free.app/images/get-keys");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching image keys: " + www.error);
                continue;
            }

            // Parse keys from the response
            ImageResponse response = JsonUtility.FromJson<ImageResponse>(www.downloadHandler.text);
            List<string> allKeys = response.keys;
            List<string> missingKeys = allKeys.FindAll(key => !existingKeys.Contains(key));

            if (missingKeys.Count > 0)
            {
                Debug.Log("Missing images found, downloading...");
                StartCoroutine(DownloadMissingImages(missingKeys));
            }
            else
            {
                Debug.Log("All images are up-to-date.");
            }
        }
    }

    private IEnumerator DownloadMissingImages(List<string> missingKeys)
    {
        string url = "https://kite-prompt-koi.ngrok-free.app/images/get-by-keys";
        string keysQuery = string.Join("&keys=", missingKeys); // Create the query for FastAPI
        UnityWebRequest www = UnityWebRequest.Get($"{url}?keys={keysQuery}");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error downloading missing images: " + www.error);
            yield break;
        }

        ImageResponse imageResponse = JsonUtility.FromJson<ImageResponse>(www.downloadHandler.text);

        for (int i = 0; i < imageResponse.images.Count && i < imageSpawns.Count; i++)
        {
            byte[] imageBytes = System.Convert.FromBase64String(imageResponse.images[i]);
            bool isStereo = imageResponse.stereos[i];

            Texture2D texture = new Texture2D(2, 2);
            bool isLoaded = texture.LoadImage(imageBytes); // Check if loading the image was successful
            if (!isLoaded)
            {
                Debug.LogError("Failed to load texture from image data.");
            }


            GameObject newCanvas = Instantiate(imageCanvas, imageSpawns[i].position, imageSpawns[i].rotation);
            newCanvas.transform.localScale = imageSpawns[i].localScale;

            Renderer renderer = newCanvas.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (isStereo) {
                    Material mat = new Material(renderer.material); // Create a new material instance if necessary
                    mat.mainTexture = texture;
                    renderer.material = mat; // Assign the new material to the renderer
                    renderer.material.shader = Shader.Find("Stereoscopic/StereoImage_SideBySide2");
                }
                renderer.material.mainTexture = texture;
            }

            existingKeys.Add(imageResponse.keys[i]);
        }

        Debug.Log("Missing images downloaded and updated.");
    }

}
