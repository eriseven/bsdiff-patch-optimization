using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PatchingTest : MonoBehaviour
{
    private string SrcPath => Path.Combine(Application.persistentDataPath, "Src");
    private string DstPath => Path.Combine(Application.persistentDataPath, "Dst");
    private string PatchPath => Path.Combine(Application.persistentDataPath, "Patch");

    void ExcuteTest()
    {
        
    }
}
