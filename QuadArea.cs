using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuadArea : MonoBehaviour
{
   
    //Diameter of area where agent and icePatches can be, needed in DroneAgent sensor observations.
    public const float AreaDiameter = 20f;

    //A lookup dictionary for looking up an icePatch from a collider
    private Dictionary<Collider, IcePatch> colliderIcePatchDictionary;

    //create list of ice patches
    public List<IcePatch> IcePatches { get; private set; }

    // manages the whole bunch of ice areas
    public void ResetIce()
    {
        // find way to randomly reset each of the ice patches
        foreach (IcePatch icePatch in IcePatches)
        {
            icePatch.ResetIcePatch();
        }
    }

    //Gets the icepatch that an IcePatch collider belongs to .
    public IcePatch GetIcePatchFromCollider(Collider collider)
    {
        return colliderIcePatchDictionary[collider];
    }

    //Called when area wakes up
    private void Awake()
    {
        //Initialize vars
        colliderIcePatchDictionary = new Dictionary<Collider, IcePatch>();
        IcePatches = new List<IcePatch>();
    }
    
    private void Start()
    {
        //Find all icePatches that are children of this GameObject/Transform
        FindChildIcePatches(transform);
    }
    //Recursively find all icePatches that are children of a parent transform
    private void FindChildIcePatches(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            //look for IcePatch component
            IcePatch icePatch = child.GetComponent<IcePatch>();
            
            if (icePatch != null)
            {
                //Found an icePatch, add to icePatch list
                IcePatches.Add(icePatch);

                // Add the icepatch collider to the lookup dictionary
                colliderIcePatchDictionary.Add(icePatch.icePatchCollider, icePatch);
                Debug.Log(IcePatches);
            }
            else
            {
                //IcePatch component not found, so check children
                FindChildIcePatches(child);
            }
        }
    }
    
}

