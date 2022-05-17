using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;



public class XRExploration : MonoBehaviour
{
    [SerializeField]
    public LayerMask BVHLayer;
    public List<GameObject> currentPath;
    InfoAnchor[] anchors;

    public float maxDistanceFromAnchors=2.0f;

    public bool sortAnchors=false;

    List<InfoAnchor> results;

    public int numNeighbors=0;

    //mapping of user positions>


    // Start is called before the first frame update
    void Start()
    {
        //hide all anchors
        anchors=GameObject.FindObjectsOfType<InfoAnchor>();
        print("this many anchors: "+anchors.Length);
        results=new List<InfoAnchor>();
    }

    // Update is called once per frame
    void Update()
    {
        // print(Camera.main.gameObject.name);
        // print(""+BVHLayer);
        currentPath=this.getObjectCurrentBVHPath(Camera.main.gameObject,BVHLayer);
        // print(this.getPathString(currentPath,"CAMERA PATH: "));
        disableAllAnchors();
        //search for the anchors the user should see now by either
        //1. euclidean distance;no bvh. maybe index?
        //2. bvh but not tracking where user is; start at the root
        //3. bvh but starting at user's current node
        enableAnchorsWithEuclideanDistanceNotIndexed(maxDistanceFromAnchors);
        print("results: "+percentageCorrect(groundTruthForUserPos(),results)+", "+this.getPathString(currentPath,"CAMERA PATH: ")+", gt="+groundTruthForUserPos().Count);

        //ground truth is how many anchors should actually be visible 

        if (Input.GetKeyDown(KeyCode.P)){
            //push current hierarchy to firebase. easier than doing manually
        }

        if (Input.GetKeyDown(KeyCode.L)){
            //temporarily FB grab
        }
    }

    float percentageCorrect(List<InfoAnchor> groundTruth, List<InfoAnchor> results){
        int numRight=0;
        foreach (InfoAnchor i in groundTruth){
            if (results.Contains(i)){
                numRight++;
            } 
        }
        return ((float) numRight)/((float)groundTruth.Count);
    }

    List<InfoAnchor> groundTruthForUserPos(){
        List<InfoAnchor> groundTruthAnchors=new List<InfoAnchor>();//=//new List<InfoAnchor>(currentPath[0].GetComponentsInChildren<InfoAnchor>());

        for (int j=0; j< currentPath[0].transform.childCount; j++){
            if (currentPath[0].transform.GetChild(j).gameObject.GetComponent<InfoAnchor>()){
                groundTruthAnchors.Add(currentPath[0].transform.GetChild(j).gameObject.GetComponent<InfoAnchor>());
            }
        }
        print("Ground truth anchors");
        if (numNeighbors>0){
            //get all other children of currentpath[1]
            //enable all of their nodes
            //don't worry about contiguous rn
        }
        return groundTruthAnchors;
    }

    void disableAllAnchors(){
        foreach (InfoAnchor i in anchors){
            i.gameObject.SetActive(false);
        }
    }

    int enableAnchorsWithEuclideanDistanceNotIndexed(float maxDistance){
        results.Clear();
        foreach (InfoAnchor i in anchors){
            if ((i.transform.position-Camera.main.transform.position).magnitude<maxDistance){
                i.gameObject.SetActive(true);
                results.Add(i);
            }
        }
        return anchors.Length; //needs to check every anchor
    }


    public List<GameObject> getObjectCurrentBVHPath(GameObject g, LayerMask BVHLayer){
        
        List<GameObject> returnList=new List<GameObject>();
        Collider[] hitBoxes=Physics.OverlapSphere(g.transform.position,0.1f,BVHLayer);
        if (hitBoxes.Length>0){
            
            //doesn't matter which child we start at b/c this will go down to the lowest child anyway
            Transform currentChild=hitBoxes[0].gameObject.transform;
            
            Transform nextChild=null;

            if (currentChild.transform.childCount>0){
                // print("currentChild transform " +(currentChild.childCount));
                while (currentChild.childCount>0){
                    nextChild=null;
                    for(int i=0; i<currentChild.childCount;i++){
                        if (nextChild!=null){
                            break;
                        }

                        if (currentChild.GetChild(i).gameObject.layer==BVHLayer){
                            Collider[] colliders=currentChild.GetChild(i).gameObject.GetComponents<Collider>();
                            foreach (Collider c in colliders){
                                if (Array.IndexOf(hitBoxes,c)>-1){
                                    nextChild=currentChild.GetChild(i);
                                    // print("next child"+nextChild.gameObject.name);
                                    break;
                                }
                            }
                        }
                    }
                    // currentChild=currentChild.transform.GetChild(0);
                    if (nextChild==null){
                        break;
                    }
                    currentChild=nextChild;
                }
            } else{
                // print("no children " +(currentChild==null));
            }
            returnList.Add(currentChild.gameObject);
            while (currentChild.parent!=null){
                returnList.Add(currentChild.parent.gameObject);
                currentChild=currentChild.parent;
            }
            // print("num children for this"+returnList.Count);
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
