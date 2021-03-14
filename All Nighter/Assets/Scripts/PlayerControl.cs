using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class PlayerControl : MonoBehaviour
{

    Vector2 velocity;
    [Header("Player Speed Settings")]
    [Tooltip("what the player's speed is set to when moving from being stationary")]
    [SerializeField] private float initalSpeed;
    [Tooltip("maximum speed")]
    [SerializeField] private float maxspd;
    private float currentMaxspd;
    [Tooltip("percent of maxspeed gained per frame")]
    [SerializeField] [Range(0.0f, 1.0f)] private float acceleration;
    [Tooltip("percent of maxspeed lost per frame")]
    [SerializeField] [Range(0.0f, 1.0f)] private float decelleration;
    [Tooltip("angle difference required for rotation to be instant")]
    [SerializeField] [Range(0.0f, 360f)] private float instantRotationCutoff = 145f;
    [Tooltip("the maximum angle difference where rotation is sped up")]
    [SerializeField] [Range(0.0f, 360f)] private float lightRotationCutoff = 60f;
    [Tooltip("the boost applied to rotation under the lightRotationCutoff")]
    [SerializeField] [Range(0.0f, 360f)] private float lightRotationBoost;
    [Tooltip("the maximum rotation allowed per frame, not including the lightRotationBoost")]
    [SerializeField] [Range(0.0f, 360f)] private float maxRotation;
    [Tooltip("if the player is moving, they will move atleast this speed")]
    [SerializeField] private float minimumSpeed;
    [SerializeField] private Grid referenceGrid;
    [Tooltip("a multiplier ontop of the maximum rotation per frame, keep it around .5 for best results")]
    [SerializeField] [Range(0.0f, 1.0f)] private float turnStrength;

    [Tooltip("time in seconds it takes to complete a full dash")]
    [SerializeField] private float dashTime;
    [Tooltip("time in seconds it takes to reach max dash speed, keep lower than dashTime")]
    [SerializeField] private float dashBurstTime;
    [Tooltip("dash max speed")]
    [SerializeField] private float dashSpeed;
    [Tooltip("the inital speed of a dash")]
    [SerializeField] private float initalDashSpeed;

    [SerializeField] private float attackTime;
    [SerializeField] private float attackBurstTime;
    [SerializeField] private float attackSpeed;
    [SerializeField] private float initalAttackSpeed;
    [SerializeField] GameObject attackHitboxRef;
    [SerializeField] private float attackHitboxEnableTime;
    [SerializeField] private float attackHitboxDisableTime;
    [SerializeField] private float attackHitboxDistance;


    [SerializeField] Text debugSpeed;
    private float lastDirection;    
    public LayerMask playerCollisionMask;
    private PolygonCollider2D circleCollider;

    Camera camera;
    [Header("Camera Settings")]
    [Tooltip("if true, resets the cameraOffset to the current localPosition when start is called")]
    [SerializeField] private bool setCameraOffsetAtStart = true;
    [Tooltip("the constant localposition offset of the camera from the player")]
    [SerializeField] private Vector3 cameraOffset;
    [Tooltip("how many units per frame the camera extends")] 
    [SerializeField] private float cameraMoveExtensionSpeed;
    [Tooltip("the maximum extension possible")] 
    [SerializeField] private float cameraMoveExtensionMaximumDistance;
    [Tooltip("the lerp value that determines how quickly the camera moves to it's target location")] 
    [SerializeField] [Range(0.0f, 1.0f)] private float cameraMovementFactor;
    [Tooltip("the lerp value which determines how quickly the player can rotate without the camera breaking it's extension")] 
    [SerializeField] [Range(0.0f, 1.0f)] private float cameraRotationFactor;
    [Tooltip("if true, the camera z value will change accoring to the low and high distances in relation to the current extension/maximum extension as a lerp value")] 
    [SerializeField] private bool cameraScaleZAccordingToExtension = true;
    [Tooltip("if true, the camera's z value will not change unless the extension is greater than the minimum distance")] 
    [SerializeField] private bool cameraScaleZOnlyIfFollowing = true;
    [Tooltip("the relative z position when extension is low")] 
    [SerializeField] private float cameraLowDistanceZ;
    [Tooltip("the relative z position when extension is at it's maximum")] 
    [SerializeField] private float cameraHighDistanceZ;
    [Tooltip("the minimum distance required before the camera will follow the extended position")] 
    [SerializeField] private float cameraMinimumDistanceToFollow;
    [Tooltip("the maximum angle allowed in a rotation before the extension breaks")] 
    [SerializeField] [Range(0.0f, 360.0f)] private float cameraAngleThreshold = 45f;

    private float currentDashTime = -1;//-1 means inactive, -0.1 means just started
    private float currentAttackTime = -1;

    private Vector2 cameraMove;

    private Vector2 lockedDirection;

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

    public Vector2 lastDirectionVector
    {
        get
        {
            return new Vector2(Mathf.Cos(Mathf.Deg2Rad * lastDirection), Mathf.Sin(Mathf.Deg2Rad * lastDirection));
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

    public static float OutCos(float t)
    {
        //returns a value from 0 to 1 based on 1, interpolates from t=0:1 to t=1:0 in a downward curve
        return -Mathf.Cos(Mathf.PI / 2f * (t + 2));
    }

    // Start is called before the first frame update
    void Start()
    {
        lastDirection = 0;
        circleCollider = GetComponent<PolygonCollider2D>();
        //grab main camera
        camera = Camera.main;
        camera.enabled = true;
        if (setCameraOffsetAtStart)
        {
            cameraOffset = camera.transform.localPosition;
        }
        currentMaxspd = maxspd;
        attackHitboxRef.SetActive(false); 
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
                    lastDirection = vectorAngle( newDirection);
                }
                else
                {
                    newSpeed += acceleration * currentMaxspd;
                }
            }
            else
            {
                newDirection = new Vector2(inputx, inputy).normalized;
                newSpeed += acceleration * currentMaxspd;
                
            }

            float dangle = vectorAngle(newDirection);
            if (currentDashTime != -1f)
            {
                dangle = vectorAngle(lockedDirection);
            }
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
                    velocity -= velocity.normalized * Mathf.Min( decelleration * currentMaxspd, velocity.magnitude);
                }
            }     
        }

        void SetLockedDir()
        {
            if (velocity.magnitude > 0.1f)
            {
                lockedDirection = velocity.normalized;
            }
            else
            {
                lockedDirection = lastDirectionVector;
            }
        }


        if (Input.GetButton("Dash") && currentDashTime == -1f)
        {
            SetLockedDir();
            currentDashTime = -0.1f;
        }
        else if (Input.GetButton("Attack") && currentAttackTime == -1f)
        {
            SetLockedDir();
            currentAttackTime = -0.1f;
        }

        void DashTick(ref float currentTime, float initalSpeed, float speed, float burstTime, float totalTime)
        {
            if (currentTime == -0.1f)
            {
                currentTime = 0f;
            }
            else
            {
                currentTime += Time.deltaTime;
            }
            if (currentTime < burstTime)
            {
                currentMaxspd = currentTime / burstTime * (speed - initalSpeed) + initalSpeed;
                velocity = lockedDirection.normalized * currentMaxspd;
            }
            else if (currentTime < totalTime)
            {
                currentMaxspd = OutCos((currentTime - burstTime) / (totalTime - burstTime)) * speed;
                print((currentTime - burstTime) / (totalTime - burstTime));
                velocity = lockedDirection.normalized * currentMaxspd;
            }
            else//dash over
            {
                currentMaxspd = maxspd;
                currentTime = -1f;
            }
        }



        if (currentDashTime != -1f)
        {
            DashTick(ref currentDashTime, initalDashSpeed, dashSpeed, dashBurstTime, dashTime);

        }else if (currentAttackTime != -1f)
        {
            DashTick(ref currentAttackTime, initalAttackSpeed, attackSpeed, attackBurstTime, attackTime);
            //enable or disable hitbox
            if (currentAttackTime >= attackHitboxEnableTime && currentAttackTime < attackHitboxDisableTime && !attackHitboxRef.activeInHierarchy)
            {
                attackHitboxRef.SetActive(true);
            }
            if ((currentAttackTime >= attackHitboxDisableTime || currentAttackTime == -1f) && attackHitboxRef.activeInHierarchy)
            {
                attackHitboxRef.SetActive(false);
            }
            //move hitbox
            if (attackHitboxRef.activeInHierarchy)
            {
                attackHitboxRef.transform.localPosition = new Vector2(lockedDirection.x * referenceGrid.cellSize.x, lockedDirection.y * referenceGrid.cellSize.y) * attackHitboxDistance;
            }
        }

        if (currentSpeed > currentMaxspd)
        {
            velocity = velocity.normalized * currentMaxspd;
        }


        if (debugSpeed != null)
        {
            debugSpeed.text = currentSpeed.ToString();
        }

        Vector3 moveVector = new Vector3(velocity.x * referenceGrid.cellSize.x, velocity.y * referenceGrid.cellSize.y, 0f);

        RaycastHit2D[] collision = new RaycastHit2D[1];

        

        float moveAngle = Mathf.Deg2Rad * vectorAngle(moveVector);
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = playerCollisionMask;
        
        int collisions = circleCollider.Cast(moveVector.normalized, filter,collision,moveVector.magnitude*4f);
        if (collisions>0)
        {
            Vector2 tempMove;
            float angle = vectorAngle(Vector2.Perpendicular(collision[0].normal).normalized);
            if (Mathf.Abs(Mathf.DeltaAngle(vectorAngle(velocity), angle)) > 90f)
            {
                angle += 180;
            }
            if (Mathf.Abs(Mathf.DeltaAngle(vectorAngle(velocity), angle)) < 90f)
            {
                tempMove = new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle) * velocity.magnitude, Mathf.Sin(Mathf.Deg2Rad * angle) * velocity.magnitude);
                moveVector = new Vector3(tempMove.x * referenceGrid.cellSize.x, tempMove.y * referenceGrid.cellSize.y, 0f);
            }
            //Debug.DrawLine(transform.position, transform.position + moveVector * 20f, Color.green, 3f);
        }

        //horizontal collision
        collisions = circleCollider.Cast(new Vector2(-1 + 2 * Convert.ToInt32((Mathf.Sign(moveVector.x) > 0)), 0), filter, collision, Mathf.Abs(moveVector.x));
        if (collisions > 0)
        {
            int shortest = 0;
            for (int i = 0; i < collisions; i++)
            {
                if ((collision[i].distance) < (collision[shortest].distance))
                {
                    shortest = 1;
                }
            }
            moveVector.x = Mathf.Cos(moveVector.x) * (collision[shortest].distance);
        }

        //vertical collision
        collisions = circleCollider.Cast(new Vector2(0, -1 + 2 * Convert.ToInt32((Mathf.Sign(moveVector.y) > 0))), filter, collision, Mathf.Abs(moveVector.y));
        if (collisions > 0)
        {
            int shortest = 0;
            for (int i = 0; i < collisions; i++)
            {
                if( (collision[i].distance) < (collision[shortest].distance))
                {
                    shortest = 1;
                }
            }
            moveVector.y = Mathf.Sign(moveVector.y) * (collision[shortest].distance);
        }

        collisions = circleCollider.Cast(moveVector.normalized, filter, collision, moveVector.magnitude);
        if (collisions > 0)
        {
            moveVector = Vector2.zero;
        }

        Vector3 targetVector = new Vector3(transform.position.x + Mathf.Cos(direction * Mathf.Deg2Rad) * 20f, transform.position.y + Mathf.Sin(direction * Mathf.Deg2Rad) * 20f, transform.position.z);
        Debug.DrawLine(transform.position+Vector3.back, targetVector + Vector3.back);


        //move player
        transform.position = moveVector + transform.position;

        //rotate camera  
        if (cameraMove != Vector2.zero &&( currentSpeed == 0 || Mathf.Abs(Mathf.DeltaAngle(vectorAngle(cameraMove),direction))>cameraAngleThreshold))
        {
            cameraMove = Vector2.zero;
        }
        else
        {
            cameraMove = Vector2.Lerp(cameraMove.normalized, velocity.normalized,cameraRotationFactor).normalized * (cameraMove.magnitude + cameraMoveExtensionSpeed);
            if (cameraMove.magnitude > cameraMoveExtensionMaximumDistance)
            {
                cameraMove = cameraMove.normalized * cameraMoveExtensionMaximumDistance;
            }
        }

        //move camera
        Vector3 cameraPosition = camera.transform.localPosition;
        if (cameraMove.magnitude > cameraMinimumDistanceToFollow)
        {
            float zOff = 0;
            if (cameraScaleZAccordingToExtension)
            {
                if (cameraScaleZOnlyIfFollowing)
                {
                    zOff = Mathf.Lerp(cameraLowDistanceZ, cameraHighDistanceZ, Mathf.Max(cameraMove.magnitude - cameraMinimumDistanceToFollow, 0) / cameraMoveExtensionMaximumDistance);
                }
                else
                {
                    zOff = Mathf.Lerp(cameraLowDistanceZ, cameraHighDistanceZ, cameraMove.magnitude  / cameraMoveExtensionMaximumDistance);
                }
                
            }
            Vector3 newCamPos = new Vector3(cameraOffset.x + cameraMove.x, cameraOffset.y + cameraMove.y, cameraOffset.z + zOff);
            camera.transform.localPosition = Vector3.Lerp(cameraPosition, newCamPos, cameraMovementFactor);
        }
        else
        {
            Vector3 newCamPos = new Vector3(cameraOffset.x , cameraOffset.y , cameraOffset.z );
            camera.transform.localPosition = Vector3.Lerp(cameraPosition, newCamPos, cameraMovementFactor);
        }

        
    }
}
