using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerControl : MonoBehaviour
{

    Vector2 velocity;

    [SerializeField] private float initalSpeed;
    [SerializeField] private float maxspd;
    [SerializeField] [Range(0.0f, 1.0f)] private float acceleration;
    [SerializeField] [Range(0.0f, 1.0f)] private float decelleration;
    [SerializeField] [Range(0.0f, 360f)] private float instantRotationCutoff;
    [SerializeField] [Range(0.0f, 360f)] private float lightRotationCutoff;
    [SerializeField] [Range(0.0f, 360f)] private float lightRotationBoost;
    [SerializeField] [Range(0.0f, 360f)] private float maxRotation;
    [SerializeField] private float minimumSpeed;
    [SerializeField] private Grid referenceGrid;

    [SerializeField] [Range(0.0f, 1.0f)] private float turnStrength;
    private float lastDirection;

    public Text debugSpeed;

    public float currentSpeed {
        get{
            return velocity.magnitude;
        }
    }

    public float direction
    {
        get
        {

            if (velocity.magnitude==0f)//velocity vector has no magnitude
            {
                return lastDirection;
            }

            if (velocity.x==0)//avoiding a divide by zero error
            {
                return 0f;
            }

            return Mathf.Rad2Deg * Mathf.Atan2(velocity.y,velocity.x);
        }
    }

    public static float vectorAngle(Vector2 v)
    {
        if (v.magnitude == 0)
        {
            return 0f;
        }
        else
        {
            return Mathf.Rad2Deg * Mathf.Atan2(v.y, v.x);
        }
    }

    public static Vector2 rotate(Vector2 v, float delta)
    {
        delta *= Mathf.Deg2Rad;
        return new Vector2(
            v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta),
            v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta)
        );
    }

    // Start is called before the first frame update
    void Start()
    {
        lastDirection = 0;
    }

    // Update is called once per frame
    void Update()
    {
        float inputx = Input.GetAxis("Horizontal");
        float inputy = Input.GetAxis("Vertical");

        if (inputx != 0 || inputy != 0)//if any input
        {
            Vector2 newDirection = Vector2.zero;
            float newSpeed = currentSpeed;
            if (currentSpeed < initalSpeed)
            {
                newDirection = new Vector2(inputx, inputy).normalized;

                if (currentSpeed == 0)
                {
                    velocity = newDirection * initalSpeed;
                    newSpeed = initalSpeed;

                }
                else
                {
                    newSpeed += acceleration * maxspd;
                }
            }
            else
            {
                newDirection = new Vector2(inputx, inputy).normalized;
                newSpeed += acceleration * maxspd;
                
            }

            float dangle = vectorAngle(newDirection);

            if (Mathf.Abs( Mathf.DeltaAngle(direction, vectorAngle(newDirection))) > instantRotationCutoff)
            {
                velocity = rotate(velocity.normalized, Mathf.DeltaAngle(direction, vectorAngle(newDirection))) * newSpeed;
            }
            else
            {
                if (Mathf.Abs(Mathf.DeltaAngle(direction, vectorAngle(newDirection))) > lightRotationCutoff)
                {
                    velocity = rotate(velocity.normalized, turnStrength * Mathf.Clamp(Mathf.DeltaAngle(direction, dangle), -lightRotationBoost, lightRotationBoost)) * newSpeed;
                }
                else//smooth rotation
                {
                    velocity = rotate(velocity.normalized, turnStrength * Mathf.Clamp(Mathf.DeltaAngle(direction, dangle), -maxRotation, maxRotation)) * newSpeed;
                }

                    
            }

            if (currentSpeed > maxspd)
            {
                velocity = velocity.normalized * maxspd;
            }

            lastDirection = direction;
        }
        else//no input: slow down
        {
            if (velocity.magnitude != 0)
            {
                if (velocity.magnitude < minimumSpeed)
                {
                    velocity = new Vector2(0f, 0f);
                }
                else
                {
                    velocity -= velocity.normalized * decelleration *currentSpeed;
                }
            }     
        }

        if (debugSpeed != null)
        {
            debugSpeed.text = currentSpeed.ToString();
        }

        Vector3 targetVector = new Vector3(transform.position.x + Mathf.Cos(direction * Mathf.Deg2Rad) * 20f, transform.position.y + Mathf.Sin(direction * Mathf.Deg2Rad) * 20f, transform.position.z);
        Debug.DrawLine(transform.position+Vector3.back, targetVector + Vector3.back);


        //move player
        transform.position = new Vector3(transform.position.x+velocity.x * referenceGrid.cellSize.x,transform.position.y+velocity.y * referenceGrid.cellSize.y, transform.position.z);

    }
}
