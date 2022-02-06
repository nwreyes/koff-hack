using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IcePatch : MonoBehaviour
{
    [HideInInspector]
    public Collider icePatchCollider;
    public GameObject iceGameObject;

    // returns position of ice patch's position
    public Vector3 IcePatchPosition
    {
        get
        {
            return icePatchCollider.transform.position;
        }
    }

    /// <summary>
    /// A vector pointing straight out of the ice patch
    /// </summary>
    public Vector3 IcePatchUpVector
    {
        get
        {
            return icePatchCollider.transform.up;
        }
    }
    
    //the amount of ice remaining in the ice patch
    public float IceAmount { get; private set; }

    //whther the ice patch has any ice remaining
    public bool HasIce
    {
        get
        {
            return IceAmount > 0f;
        }
    }

    // method for drone to "salt" the snow/ice
    public float Salt(float amount)
    {
        // Track how much ice was successfully taken
        float iceTaken = Mathf.Clamp(amount, 0f, IceAmount);

        // Subtract the ice
        IceAmount -= amount;

        if (IceAmount <= 0)
        {
            IceAmount = 0;
            
            //Disable the ice patch object
            iceGameObject.SetActive(false);
        }
        //returns the amount of ice taken
        return iceTaken;
    }

    // after end of episode, reset individual ice patch
    public void ResetIcePatch()
    {
        iceGameObject.SetActive(false);
        // Refill Ice Patch
        IceAmount = 1f;

        //GIVE A RANDOM CHANCE OF BEING ENABLED
        if (UnityEngine.Random.value > 0.7f)
                iceGameObject.SetActive(true);        
    }

    // once ice patch wakes up, assign collider
    private void Awake()
    {
        // find ice patch collider
        icePatchCollider = iceGameObject.GetComponent<Collider>();
    }

    
    
}
