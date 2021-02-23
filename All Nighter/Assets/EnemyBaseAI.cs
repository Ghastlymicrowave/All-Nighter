using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBaseAI : MonoBehaviour
{
    private Transform playerTransform;
    [SerializeField] private float reactionTime = 0;
    [SerializeField] private float reactionTimer = 0;
    public enum state {
        patrol,
        alert,
        attack,
    }
    [SerializeField] private float seeDistance = 0;//Line of sight vision
    [SerializeField] private float hearDistance = 0;//Distance where they can sense actions
    [SerializeField] private float smellDistance = 0;//Distance where they automatically know where the player is

    [SerializeField] public float direction = 0;
    private Vector2 myPosition
    {
        get
        {
            return new Vector2(transform.position.x,transform.position.y); 
        }
    }
    private Vector2 playerPosition
    {
        get
        {
            return new Vector2(playerTransform.position.x, playerTransform.position.y);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        playerTransform = GameObject.Find("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
