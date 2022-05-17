using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;



public class XRExploration : MonoBehaviour
{
    [SerializeField]
    public LayerMask BVHLayer;
    public List<GameObject> currentPath;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // print(Camera.main.gameObject.name);
        // print(""+BVHLayer);
        currentPath=this.getObjectCurrentBVHPath(Camera.main.gameObject,BVHLayer);
        // print(this.getPathString(currentPath,"CAMERA PATH: "));

        if (Input.GetKeyDown(KeyCode.P)){
            //push current hierarchy to firebase. easier than doing manually
        }

        if (Input.GetKeyDown(KeyCode.L)){
            //temporarily FB grab
        }
    }


    public List<GameObject> getObjectCurrentBVHPath(GameObject g, LayerMask BVHLayer){
        List<GameObject> returnList=new List<GameObject>();
        Collider[] hitBoxes=Physics.OverlapSphere(g.transform.position,0.01f,BVHLayer);
        if (hitBoxes.Length>0){
            //doesn't matter which child we start at b/c this will go down to the lowest child anyway
            Transform currentChild=hitBoxes[0].gameObject.transform;
            Transform nextChild=null;
            while (currentChild.transform.childCount>0){
                nextChild=null;
                for(int i=0; i<currentChild.transform.childCount;i++){
                    if (nextChild!=null){
                        // break;
                    }

                    if (currentChild.transform.GetChild(i).gameObject.layer==BVHLayer){
                        Collider[] colliders=currentChild.transform.GetChild(i).gameObject.GetComponents<Collider>();
                        foreach (Collider c in colliders){
                            if (Array.IndexOf(hitBoxes,c)>-1){
                                nextChild=currentChild.transform.GetChild(i);
                                print("next child"+nextChild.gameObject.name);
                                // break;
                            }
                        }
                    }
                }
                // currentChild=currentChild.transform.GetChild(0);
                currentChild=nextChild;
            }
            returnList.Add(currentChild.gameObject);
            while (currentChild.transform.parent!=null){
                returnList.Add(currentChild.transform.parent.gameObject);
                currentChild=currentChild.transform.parent;
            }
        } else{
            return null;
        }
        return returnList;
    }

    public string getPathString(List<GameObject> pathObjects, string prefix){
        string currentPathString=prefix;
        
        for (int i=0;i< pathObjects.Count;i++){
            currentPathString+=pathObjects[i].name+(i==pathObjects.Count-1?"":" < ");
        }
        return currentPathString;
    }
    
}
