using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class XRExploration : MonoBehaviour
{
    [SerializeField]
    LayerMask BVHLayer;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        string currentPathString="";
        List<GameObject> pathObjects=getUsersCurrentPath();
        for (int i=0;i< pathObjects.Count;i++){
            currentPathString+=pathObjects[i].name+(i==pathObjects.Count-1?"":" < ");
        }
        print(currentPathString);
    }

    List<GameObject> getUsersCurrentPath(){
        List<GameObject> returnList=new List<GameObject>();
        Collider[] hitBoxes=Physics.OverlapSphere(Camera.main.transform.position,0.01f,BVHLayer);
        if (hitBoxes.Length>0){
            //doesn't matter which child we start at b/c this will go down to the lowest child anyway
            Transform currentChild=hitBoxes[0].gameObject.transform;
            while (currentChild.transform.childCount>0){
                currentChild=currentChild.transform.GetChild(0);
            }
            returnList.Add(currentChild.gameObject);
            while (currentChild.transform.parent!=null){
                returnList.Add(currentChild.transform.parent.gameObject);
                currentChild=currentChild.transform.parent;
            }
        } else{
            print("user is out of bounds");
        }
        return returnList;
    }
}
