using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using BsDiff;
using IngameDebugConsole;
using Debug = UnityEngine.Debug;

public class PatchingTest : MonoBehaviour
{
    private string SrcPath => Path.Combine(Application.persistentDataPath, "Src");
    private string DstPath => Path.Combine(Application.persistentDataPath, "Dst");
    private string PatchPath => Path.Combine(Application.persistentDataPath, "Patch");

    private void Start()
    {
        DebugLogConsole.AddCommand("patch", "bsdiff patch file", BsdiffPatchFile);
    }

    void BsdiffPatchFile()
    {
        var srcFiles = Directory.GetFiles(SrcPath, "*.*", SearchOption.AllDirectories);
        var sw = new Stopwatch();
        foreach (var file in srcFiles)
        {
            var relativePath = Path.GetRelativePath(SrcPath, file);
            var relativeDir = Path.GetDirectoryName(relativePath);
            var fileName = Path.GetFileName(file);
            if (!Directory.Exists(Path.Combine(DstPath, relativeDir)))
            {
                Directory.CreateDirectory(Path.Combine(DstPath, relativeDir));
            }
            
            if (File.Exists(Path.Combine(PatchPath, relativePath + ".patch")))
            {
                Debug.Log($"start patching {fileName}");
                using (var srcFs = File.OpenRead(file))
                using (var dstFs = File.OpenWrite(Path.Combine(DstPath, relativePath)))
                {
                    sw.Restart();
                    BinaryPatchUtility.Apply(srcFs, () => File.OpenRead(Path.Combine(PatchPath, relativePath + ".patch")), dstFs);
                    sw.Stop();
                    Debug.Log($"{fileName} patched in {sw.ElapsedMilliseconds}ms");
                }
            }
        }
    }
}
