using UnityEngine;
using UnityEngine.UI;

public class AnimatedUIImage : MonoBehaviour
{
    public Sprite[] frames; // Arrastrá tus frames acá en el Inspector
    public float frameRate = 12f;

    private Image img;
    private int currentFrame;
    private float timer;

    void Start()
    {
        img = GetComponent<Image>();
    }

    void Update()
    {
        if (frames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            timer -= 1f / frameRate;
            currentFrame = (currentFrame + 1) % frames.Length;
            img.sprite = frames[currentFrame];
        }
    }
}
