#define NATIVE_PATCHER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using BsDiff;
using CsvHelper;
using IngameDebugConsole;
using SharpHDiffPatch.Core;
using Debug = UnityEngine.Debug;


public class PatchingTest : MonoBehaviour
{
    private string SrcPath => Path.Combine(Application.persistentDataPath, "Src");
    private string DstPath => Path.Combine(Application.persistentDataPath, "Dst");
    private string PatchPath => Path.Combine(Application.persistentDataPath, "Patch");
    private string HDiffPatchPath => Path.Combine(Application.persistentDataPath, "HPatch");

    private void Start()
    {
        DebugLogConsole.AddCommand("patch", "bsdiff patch file", BsdiffPatchFile);
        DebugLogConsole.AddCommand("hdiffpatch", "hdiff patch file", HDiffPatchFile);
    }

    public class PatchingRecord
    {
        // public string PatchFile { get; set; }
        // public long PatchSize { get; set; }
        // public long OldSize { get; set; }
        // public long NewSize { get; set; }
        // public long Cost { get; set; }
        //
        public string PatchFile;
        public long PatchSize;
        public long OldSize;
        public long NewSize;
        public long Cost;
    }

    // public static void Apply(Stream input, Func<Stream> openPatchStream, Stream output)
    // void PatchFilesTest(string oldFileDir, string patchDir, string newFileDir,
    IEnumerator PatchFilesTest(string oldFileDir, string patchDir, string newFileDir,
        #if NATIVE_PATCHER
        Action<string, string, string> patcher,
        #else
        Action<Stream, string, string> patcher,
        #endif
        string recordFilePath)
    {
        var srcFiles = Directory.GetFiles(oldFileDir, "*.*", SearchOption.AllDirectories);
        var sw = new Stopwatch();
        var records = new List<PatchingRecord>();
        foreach (var file in srcFiles)
        {
            var relativePath = Path.GetRelativePath(oldFileDir, file);
            var relativeDir = Path.GetDirectoryName(relativePath);
            var fileName = Path.GetFileName(file);
            var patchPath = Path.Combine(patchDir, relativePath + ".patch");
            var destPath = Path.Combine(newFileDir, relativePath);

            if (!Directory.Exists(Path.Combine(newFileDir, relativeDir)))
            {
                Directory.CreateDirectory(Path.Combine(newFileDir, relativeDir));
            }

            if (File.Exists(patchPath))
            {
                var record = new PatchingRecord();
                record.PatchFile = fileName;
                record.OldSize = new FileInfo(file).Length;
                record.PatchSize = new FileInfo(patchPath).Length;

                Debug.Log($"start patching {fileName}");
                yield return null;
#if NATIVE_PATCHER
                var srcFs = file;
#else
                using (var srcFs = File.OpenRead(file))
#endif
                // using (var dstFs = File.OpenWrite(destPath))
                {
                    sw.Restart();
                    patcher(srcFs, patchPath, destPath);
                    sw.Stop();
                    Debug.Log($"{fileName} patched in {sw.ElapsedMilliseconds}ms");
                    record.Cost = sw.ElapsedMilliseconds;
                }
                yield return null;
                record.NewSize = new FileInfo(destPath).Length;
                records.Add(record);
            }
        }

        if (!Directory.Exists(Path.GetDirectoryName(recordFilePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(recordFilePath));
        }

        yield return null;
        Sinbad.CsvUtil.SaveObjects(records, recordFilePath);
        Debug.Log($"Patching finished: {records.Count} file patched. {records.Sum(r => r.Cost)}(ms) cost.");
        // using (var writer = new StreamWriter(recordFilePath))
        // using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        // {
        //     csv.WriteRecords(records);
        // }
    }

    void BsdiffPatchFile()
    {
        StartCoroutine(
        PatchFilesTest(SrcPath, PatchPath, DstPath, (stream, p, o) =>
            {
                
#if NATIVE_PATCHER
                using (var _stream = File.OpenRead(stream))
#else
                var _stream = stream;
#endif
                using (var newFs = File.OpenWrite(o))
                {
                    BinaryPatchUtility.Apply(_stream, () => File.OpenRead(p), newFs);
                }
            },
            Path.Combine(DstPath, "BsdiffPatchFile.csv")));
    }

    void HDiffPatchFile()
    {
        // HDiffPatch patcher = new HDiffPatch();
        StartCoroutine(
        PatchFilesTest(SrcPath, HDiffPatchPath, DstPath, (stream, p, o) =>
            {
#if NATIVE_PATCHER
                hpatchz(stream, p, o, 64);
#else
                HDiffPatch patcher = new HDiffPatch();
                patcher.Initialize(p);
                patcher.Patch(stream, o, true, default, false, true);
#endif

            },
            Path.Combine(DstPath, "HDiffPatchFile.csv")));       
    }
    
#if NATIVE_PATCHER
#if UNITY_ANDROID && !UNITY_EDITOR
    static int hpatchz(string oldFileName, string diffFileName, string outNewFileName, long cacheMemory)
    {
        using (AndroidJavaClass cls = new AndroidJavaClass("com.github.sisong.HPatch"))
        {
            return cls.CallStatic<int>("patch", oldFileName, diffFileName, outNewFileName);
        }           
    }
#else
    [DllImport ("hpatchz")] private static extern int hpatchz(string oldFileName, string diffFileName, string outNewFileName, long cacheMemory);
#endif
#endif
    

}
