using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

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

    [SerializeField] Text debugSpeed;
    public LayerMask playerCollisionMask;

    private CircleCollider2D circleCollider;

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
        circleCollider = GetComponent<CircleCollider2D>();
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

        Vector3 moveVector = new Vector3(velocity.x * referenceGrid.cellSize.x, velocity.y * referenceGrid.cellSize.y, 0f);

        RaycastHit2D collision;

        Debug.DrawLine(transform.position,transform.position+ Vector3.right * circleCollider.radius * transform.localScale.x);

        float moveAngle = Mathf.Deg2Rad * vectorAngle(moveVector);

        
        float circleRadius = circleCollider.radius * transform.localScale.x;
        collision = Physics2D.CircleCast(transform.position, circleRadius, moveVector.normalized, moveVector.magnitude, playerCollisionMask);
        if (collision.transform != null)
        {
            Vector2 tempMove;
            float angle = vectorAngle(Vector2.Perpendicular(collision.normal).normalized);
            if (Mathf.Abs(Mathf.DeltaAngle(vectorAngle(velocity), angle)) > 80f)
            {
                angle += 180;
            }
            if (Mathf.Abs(Mathf.DeltaAngle(vectorAngle(velocity), angle)) < 50f)
            {
                tempMove = new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle) * velocity.magnitude, Mathf.Sin(Mathf.Deg2Rad * angle) * velocity.magnitude);
                moveVector = new Vector3(tempMove.x * referenceGrid.cellSize.x, tempMove.y * referenceGrid.cellSize.y, 0f);
            }
        }

        //horizontal collision
        collision = Physics2D.CircleCast(transform.position, circleCollider.radius * transform.localScale.x, new Vector2( -1+2*Convert.ToInt32((Mathf.Sign(moveVector.x) > 0)),0), Mathf.Abs(moveVector.x),playerCollisionMask);
        if (collision.transform != null)//hit something
        {
            float off = 0f;
            off = Mathf.Cos(Mathf.Deg2Rad * vectorAngle(collision.point- new Vector2(transform.position.x,transform.position.y))) * circleCollider.radius * transform.localScale.x;
            moveVector.x = (collision.point.x - (transform.position.x + off));
        }

        //vertical collision
        collision = Physics2D.CircleCast(transform.position, circleCollider.radius * transform.localScale.x, new Vector2(0, -1 + 2 * Convert.ToInt32((Mathf.Sign(moveVector.y) > 0))), Mathf.Abs(moveVector.y), playerCollisionMask);
        if (collision.transform != null)//hit something
        {
            moveVector.y -= Mathf.Max( Mathf.Abs( (transform.position.y+moveVector.y)- collision.point.y),moveVector.y) * Mathf.Sign(moveVector.y) ;
            float off = 0f;
            off = Mathf.Sin(Mathf.Deg2Rad * vectorAngle(collision.point - new Vector2(transform.position.x, transform.position.y))) * circleCollider.radius * transform.localScale.y;
            moveVector.y = (collision.point.y - (transform.position.y + off));
        }

        

        Vector3 targetVector = new Vector3(transform.position.x + Mathf.Cos(direction * Mathf.Deg2Rad) * 20f, transform.position.y + Mathf.Sin(direction * Mathf.Deg2Rad) * 20f, transform.position.z);
        Debug.DrawLine(transform.position+Vector3.back, targetVector + Vector3.back);


        //move player
        transform.position = moveVector + transform.position;

    }
}
