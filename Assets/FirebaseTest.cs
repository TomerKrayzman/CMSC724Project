using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Storage;

using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using System.Linq;
using TMPro;

using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using System.Globalization;
using UnityEngine.XR.ARFoundation;

public class FirebaseTest : MonoBehaviour
{
    DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;
    DatabaseReference reference;
    //Firebase.Auth.FirebaseUser thisUser;
    // FirebaseStorage storage;
    StorageReference gsReference;
    protected bool isFirebaseInitialized = false;
    public string debugMessage;

    // Start is called before the first frame update
    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available) {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                //StartListener();
                isFirebaseInitialized = true;
                reference = FirebaseDatabase.DefaultInstance.RootReference;
                reference.KeepSynced(true);
                // storage = FirebaseStorage.DefaultInstance;
                FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(true);
                signIn();
                Firebase.Auth.FirebaseAuth auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
                Firebase.Auth.FirebaseUser user = auth.CurrentUser;
                if (user != null && SceneManager.GetActiveScene().name=="_entry"){
                    debugMessage="already a user "+user.DisplayName;
                    signIn();
                }
                loadDataFromFB();
            } else {
                Debug.LogError(
                "Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
        
    }

    void signIn(){
        Firebase.Auth.FirebaseAuth auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
      //debugMessage="Attempting sign in1...";
      auth.SignInWithEmailAndPasswordAsync("n@n.com", "testtest").ContinueWith(task => {
        //debugMessage="Attempting sign in2...";
        if (task.IsCanceled) {
          Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
          return;
        }
        if (task.IsFaulted) {
          //debugMessage="Attempting sign in3...";
          Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
          Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + ((Firebase.FirebaseException)task.Exception.Flatten().InnerExceptions[0]).ErrorCode);

          foreach (Exception e in task.Exception.Flatten().InnerExceptions){
            if (((Firebase.FirebaseException) e).ErrorCode==12){
              debugMessage="Wrong password.";
            } else if (((Firebase.FirebaseException) e).ErrorCode==14){
              debugMessage="User does not exist.";
            } else if (((Firebase.FirebaseException) e).ErrorCode==11){
              debugMessage="Please enter a valid email address.";
            } else{
              debugMessage="Unhandled error: "+((Firebase.FirebaseException) e).ErrorCode;
            }
          }
          return;
        }
        //debugMessage="Attempting sign in5...";
        debugMessage="Signed in successfully, user="+task.Result.DisplayName;
        Firebase.Auth.FirebaseAuth auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        Firebase.Auth.FirebaseUser user = auth.CurrentUser;
        //debugMessage="Attempting sign in6...";
        Debug.LogFormat("User signed in successfully: {0} ({1})", task.Result.DisplayName, task.Result.Email);
      });
    }

    void loadDataFromFB(){
        if (reference != null&& isFirebaseInitialized){
            FirebaseDatabase.DefaultInstance.GoOnline();
        reference.GetValueAsync().ContinueWithOnMainThread(task2 => {
                if (task2.IsFaulted) {
                    print("--------------------------error1!!!-");
                }
                else if (task2.IsCompleted) {
                    DataSnapshot snapshot = task2.Result;
                    print("--------------------------OK1!!!-");
                    print("---------------------------snapshot befor ecomments"+snapshot.GetRawJsonValue());
                    
                    
                }
                
            });  
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
