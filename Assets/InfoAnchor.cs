using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
public class InfoAnchor : MonoBehaviour
{
    XRExploration x;
    public List<GameObject> bvhPath;
    public LayerMask BVHLayer;
    // Start is called before the first frame update
    void Start()
    {
        x=GameObject.FindObjectOfType<XRExploration>();
        // this.gameObject.name=this.gameObject.transform.Find("mainBG").Find("title").GetComponent<TextMeshPro>().text;
        bvhPath=this.getObjectCurrentBVHPath(this.gameObject,BVHLayer);
        // this.gameObject.transform.parent=bvhPath[0].transform;
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.rotation=Quaternion.LookRotation(Vector3.Scale(Camera.main.transform.position-this.transform.position, new Vector3(1,0,1)), Vector3.up);

        // print("yay");
        // print(CommonStuff.getPathString(,"ANCHOR PATH: "));
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
            print("ANCHOR IS NOT INSIDE ANYTHING!");
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
