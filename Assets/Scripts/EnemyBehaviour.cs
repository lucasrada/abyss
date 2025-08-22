using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    private Transform target;
    public float speed;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
    }

    void Update()
    {
        transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
    }
}
