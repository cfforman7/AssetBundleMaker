using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;

public class AssetBundleBuilderWindow : EditorWindow
{
    private string[] buildTargetArr;
    private GUIContent[] buildOptionContents;
    private Vector2 scrollPosition;
    private Vector2 bundleScrollPosition;
    private Dictionary<string, bool> bundleFoldouts;
    private Dictionary<string, string> bundleFilterItemDic;
    private Dictionary<string, uint> bundleVersionDic;
    private int buildTarget;
    private BuildAssetBundleOptions buildOption;
    
    /// <summary>���¹��� ���� ���</summary>
    private string saveAssetBundlePath = "Assets/AssetBundle";
    private string saveBundleDownFolder = "";

    /// <summary>���¹��� ���� ���� ���</summary>
    private string bundleFilePath;
    /// <summary>���� ���¹��� ���� Ŭ����</summary>
    private AssetBundleDataInfoArray bundleFileDataInfoArray;

    int cellSize = 30;
    int numRows = 3;
    int numCols = 5;

    [MenuItem("Tools/AssetBundleBuildWindow")]
    public static void OpenWindow()
    {
        AssetBundleBuilderWindow window = GetWindow<AssetBundleBuilderWindow>();
        window.titleContent = new GUIContent("AssetBundleBuilder");
        window.minSize = new Vector2(900, 700);
        window.maxSize = new Vector2(1200, 900);
        window.Show();
    }

    /// <summary>
    /// ���� �ɼ� ����Ʈ�� ���� �մϴ�.
    /// </summary>
    private void MakeBuildOptionList()
    {
        BuildAssetBundleOptions[] array = Enum.GetValues(typeof(BuildAssetBundleOptions)) as BuildAssetBundleOptions[];
        buildOptionContents = new GUIContent[array.Length];
        for (int i = 0; i < array.Length; i++)
        {
            string title = array[i].ToString();
            string desc = string.Empty;
            switch (array[i])
            {
                case BuildAssetBundleOptions.None: 
                    desc = "�ɼ� ����"; break;
                case BuildAssetBundleOptions.UncompressedAssetBundle:
                    desc = "���� ������ �������� ����."; break;
                case BuildAssetBundleOptions.CollectDependencies:
                    desc = "(������ ����)���� ���� ���� ��, ���Ӽ��� �ڵ����� �����Ͽ� ���鿡 �߰���."; break;
                case BuildAssetBundleOptions.CompleteAssets:
                    desc = "(������ ����) ��� ������ �����ϴ� ���� ���� ����"; break;
                case BuildAssetBundleOptions.DisableWriteTypeTree:
                    desc = "���� ���� ���� ��, ����Ƽ�� ���� Ʈ��(type tree)�� �ۼ��ϴµ�, �� �ɼ��� ����ϸ� ���� Ʈ�� �ۼ��� ��Ȱ��ȭ�� �� �ֽ��ϴ�."; break;
                case BuildAssetBundleOptions.DeterministicAssetBundle:
                    desc = "������ ���� ���鿡 ���� �׻� ������ ���� ID ���� : ���� ������ ������ ������� �ʾҴ��� Ȯ���մϴ�. �̷��� �ϸ� ������ ������ ���� �� ���� ������ �ؽð��� �������ϴ�."; break;
                case BuildAssetBundleOptions.ForceRebuildAssetBundle:
                    desc = "���� ������ ������ �ٽ� �����մϴ�."; break;
                case BuildAssetBundleOptions.IgnoreTypeTreeChanges:
                    desc = "���� ������ ������ ��, ���� Ʈ�� ��������� �����մϴ�."; break;
                case BuildAssetBundleOptions.AppendHashToAssetBundleName:
                    desc = "���� ���� �̸� �ڿ� �ؽð��� �߰��Ͽ�, ���� ������ �̸��� �ߺ��Ǵ� ���� �����մϴ�."; break;
                case BuildAssetBundleOptions.ChunkBasedCompression:
                    desc = "���� ������ ûũ ������ �����մϴ�. : AssetBundle�� ���� �������� ������ �����ϴµ�, �̶� ���Ǵ� ������ ûũ(chunk)�Դϴ�. �⺻�����δ� 4MB�� �����Ǿ� �ֽ��ϴ�. ��, AssetBundle�� ũ�Ⱑ 4MB�� �Ѿ�� �ڵ����� ûũ�� ������ �����ϰ� �˴ϴ�."; break;
                case BuildAssetBundleOptions.StrictMode:
                    desc = "���� ���� ���� ��, ��� ��� ������ ����մϴ�."; break;
                case BuildAssetBundleOptions.DryRunBuild:
                    desc = "���� ���� ���带 �������� �ʰ�, ���忡 �ʿ��� ������ ����մϴ�."; break;
                case BuildAssetBundleOptions.DisableLoadAssetByFileName:
                    desc = "���� ���� ���� ��, ���� �̸����� ������ �ε��ϴ� ���� ��Ȱ��ȭ�մϴ�."; break;
                case BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension:
                    desc = "���� ���� ���� ��, ���� �̸��� Ȯ���ڸ� ��� ����Ͽ� ������ �ε��ϴ� ���� ��Ȱ��ȭ�մϴ�."; break;
                case BuildAssetBundleOptions.AssetBundleStripUnityVersion:
                    desc = "���� ���� ���� ��, ���Ӽ� ������ �ۼ����� �ʽ��ϴ�."; break;
            }
            buildOptionContents[i] = new GUIContent(title, desc);
        }
    }

    /// <summary>
    /// Ȱ��ȭ �� 1ȸ ȣ��
    /// </summary>
    private void OnEnable()
    {
        bundleFoldouts = new Dictionary<string, bool>();
        bundleFilterItemDic = new Dictionary<string, string>();
        bundleVersionDic = new Dictionary<string, uint>();
        //���� ���� Ÿ�� ����.
        BuildTarget curBuildTarget = EditorUserBuildSettings.activeBuildTarget;
        //���� ������ �����ϴ� Path ����
        bundleFilePath = MakeAssetBundleFileInfoFolderPath(curBuildTarget);
        //���� ���� ����
        if (File.Exists(bundleFilePath))
        {//���� ���� ���� ���� ����.
            string localInfoData = File.ReadAllText(bundleFilePath);
            //���� ���� ���� Ŭ������ ��ȯ
            bundleFileDataInfoArray = JsonUtility.FromJson<AssetBundleDataInfoArray>(localInfoData);
        }
        else
        {
            File.Create(bundleFilePath);
        }
        buildTargetArr = new string[] { BuildTarget.Android.ToString(), BuildTarget.iOS.ToString(), BuildTarget.StandaloneWindows.ToString()};
        //�ɼ� ����Ʈ ����
        MakeBuildOptionList();
    }

    private void OnGUI()
    {
        DrawLine(10);
        //���� Ÿ�� ����
        DrawBuildTarget();
        DrawLine(10);
        //���� �ɼ� ����
        DrawBuildOption();
        DrawLine(10);
        //���� ��� ����
        DrawBundleSavePath();
        DrawLine(10);
        GUILayout.BeginVertical();
        //������Ʈ ���� �ִ� ������ ���¹��� ����Ʈ �����ֱ�
        DrawAssetBundleList();
        DrawLine(10);
        //���¹��� ���� ����
        DrawBuildBundleProcess();
        DrawLine(10);
        //���¹��� ���� ������ �����ش�.
        DrawAssetbundleDataFileInfo();
        GUILayout.EndVertical();
    }

    /// <summary>
    /// ���� Ÿ�� ����
    /// </summary>
    private void DrawBuildTarget()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("���� Ÿ���� �����ϼ���");
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        buildTarget = GUILayout.SelectionGrid(buildTarget, buildTargetArr, buildTargetArr.Length, EditorStyles.toolbarButton);
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// ���� �ɼ� ����
    /// </summary>
    private void DrawBuildOption()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("���� �ɼ��� �����ϼ���");
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        buildOption = (BuildAssetBundleOptions)GUILayout.SelectionGrid((int)buildOption, buildOptionContents, 3, EditorStyles.toolbarButton);
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// ���¹��� ���� ��� ����
    /// </summary>
    private void DrawBundleSavePath()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("���� ���� ���� ��� : ", GUILayout.Width(130));
        GUILayout.Space(5);
        saveAssetBundlePath = EditorGUILayout.TextField(saveAssetBundlePath);        
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal("Box");
        //���� Ÿ�� ����
        BuildTarget selectBuildTarget = (BuildTarget)Enum.Parse(typeof(BuildTarget), buildTargetArr[buildTarget]);
        switch (selectBuildTarget)
        {
            case BuildTarget.Android: saveBundleDownFolder = Path.DirectorySeparatorChar + "Android"; break;
            case BuildTarget.iOS: saveBundleDownFolder = Path.DirectorySeparatorChar + "IOS"; break;
            case BuildTarget.StandaloneWindows: saveBundleDownFolder = Path.DirectorySeparatorChar + "Standalone"; break;
        }
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.yellow;
        GUILayout.Label("Ÿ�ٿ� ���� �ش� ��ο� ���� �˴ϴ�. ", style, GUILayout.Width(250));
        GUILayout.Label(saveAssetBundlePath + saveBundleDownFolder, style);
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// ���¹��� ����Ʈ �����ֱ�
    /// </summary>
    private void DrawAssetBundleList()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("���� ���� ����Ʈ", GUILayout.Width(130));
        GUILayout.EndHorizontal();
        string[] allBundles = AssetDatabase.GetAllAssetBundleNames();
        bundleScrollPosition = EditorGUILayout.BeginScrollView(bundleScrollPosition);
        for (int i = 0; i < allBundles.Length; i++)
        {
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal("Box");
            bool bundleFoldout = false;
            if (bundleFoldouts.ContainsKey(allBundles[i]) == false)
            {
                bundleFoldouts.Add(allBundles[i], false);
            }
            bundleFoldout = bundleFoldouts[allBundles[i]];
            EditorGUIUtility.labelWidth = 100;
            bundleFoldouts[allBundles[i]] = EditorGUILayout.Foldout(bundleFoldout, allBundles[i]);
            uint bundleVersion = 0;
            //���� ����
            if (bundleVersionDic.ContainsKey(allBundles[i]))
            {
                bundleVersion = bundleVersionDic[allBundles[i]];
            }
            GUILayout.Label("������ ���� �� ������ Verstion", GUILayout.Width(300));
            bundleVersion = (uint)EditorGUILayout.IntField((int)bundleVersion, GUILayout.Width(100));
            bundleVersionDic[allBundles[i]] = bundleVersion;

            if (GUILayout.Button(new GUIContent("+1", "������ 1 �ø��ϴ�"), GUILayout.Width(100)))
            {
                bundleVersionDic[allBundles[i]] = bundleVersionDic[allBundles[i]] + 1;
            }
            if (GUILayout.Button(new GUIContent("-1", "������ 1 �����ϴ�"), GUILayout.Width(100)))
            {
                bundleVersionDic[allBundles[i]] = bundleVersionDic[allBundles[i]] - 1;
            }
            EditorGUILayout.EndHorizontal();
            if (bundleFoldout)
            {
                if (bundleFilterItemDic.ContainsKey(allBundles[i]) == false)
                {
                    bundleFilterItemDic.Add(allBundles[i], "");
                }

                string filterText = bundleFilterItemDic[allBundles[i]];
                bundleFilterItemDic[allBundles[i]] = EditorGUILayout.TextField("Filter", filterText, GUILayout.Width(200));
                EditorGUIUtility.labelWidth = 100;
                string[] searchItems = AssetDatabase.FindAssets(filterText + " b:" + allBundles[i]);
                /*
                    t: Ÿ�� �˻� (��: "t: Texture")
                    l: ��� �˻� (��: "l: Assets/Textures")
                    n: �̸� �˻� (��: "n: texture")
                    s: ũ�� �˻� (��: "s: > 1024")
                    b: ���� ���� �˻� (��: "b: mybundle")
                */

                EditorGUI.indentLevel++;
                for (int j = 0; j < searchItems.Length; j++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(searchItems[j]),
                            AssetDatabase.GetMainAssetTypeAtPath(AssetDatabase.GUIDToAssetPath(searchItems[j]))),
                        AssetDatabase.GetMainAssetTypeAtPath(AssetDatabase.GUIDToAssetPath(searchItems[j])), false);
                    EditorGUILayout.LabelField(AssetDatabase.GUIDToAssetPath(searchItems[j]));
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// ���¹��� ���� �� ���Ϸ� �����մϴ�.
    /// </summary>
    private void DrawBuildBundleProcess()
    {
        if (GUILayout.Button("���� ���� ���� ����"))
        {
            //���� ������ ��ο� ������ ������ ����
            if (Directory.Exists(saveAssetBundlePath + saveBundleDownFolder) == false)
            {
                Directory.CreateDirectory(saveAssetBundlePath + saveBundleDownFolder);
            }

            //���� Ÿ�� ����
            BuildTarget selectBuildTarget = (BuildTarget)Enum.Parse(typeof(BuildTarget), buildTargetArr[buildTarget]);
            //���� ����
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(saveAssetBundlePath + saveBundleDownFolder, buildOption, selectBuildTarget);
            //BuildPipeline.BuildAssetBundles(saveAssetBundlePath, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

            //���Ϸ� �����ϱ� ���� ��� ���¹��� �ٽ� ����
            string[] allBundles = AssetDatabase.GetAllAssetBundleNames();
            //�ӽ� ����Ʈ ����
            List<AssetBundleDataInfo> bundleDataInfoList = new List<AssetBundleDataInfo>();
            for (int i = 0; i < allBundles.Length; i++)
            {
                AssetBundleDataInfo dundleDataInfo = new AssetBundleDataInfo();
                dundleDataInfo.name = allBundles[i];

                uint crc;
                BuildPipeline.GetCRCForAssetBundle(saveAssetBundlePath + saveBundleDownFolder + Path.DirectorySeparatorChar + allBundles[i], out crc);

                dundleDataInfo.version = bundleVersionDic[allBundles[i]];
                dundleDataInfo.crc = crc;
                //����Ʈ�� ����
                bundleDataInfoList.Add(dundleDataInfo);
            }
            //JsonUtility �� ����ϱ� ���� Ŭ������ �ٽ� ����.
            AssetBundleDataInfoArray bundleDataInfoArray = new AssetBundleDataInfoArray();
            bundleDataInfoArray.assetBundleDataInfoArray = bundleDataInfoList.ToArray();
            string bundleDataFile = JsonUtility.ToJson(bundleDataInfoArray);
            if (bundleDataFile != null)
            {
                bundleFilePath = MakeAssetBundleFileInfoFolderPath(selectBuildTarget);
                File.WriteAllText(bundleFilePath, bundleDataFile);
                AssetDatabase.Refresh();
            }
            //���� ���� �ֽ� ������ ����
            bundleFileDataInfoArray = bundleDataInfoArray;
        }
    }

    /// <summary>
    /// ���� �� Json ���� ���� �����ֱ�
    /// </summary>
    private void DrawAssetbundleDataFileInfo()
    {
        if (bundleFileDataInfoArray == null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("JSON File Viewer", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label(": ������Ʈ�� ����� ���� ���� ���� �Դϴ� : ���� ����");
            GUILayout.EndHorizontal();
            return;
        }
        //���� Ÿ�� ����
        BuildTarget selectBuildTarget = (BuildTarget)Enum.Parse(typeof(BuildTarget), buildTargetArr[buildTarget]);
        //���¹��� ���� ���� ��� ����
        bundleFilePath = MakeAssetBundleFileInfoFolderPath(selectBuildTarget);
        //���� ���� ���� ���� ����.
        //string bundleDataFile = JsonUtility.ToJson(bundleFileDataInfoArray);
        if (File.Exists(bundleFilePath) == false)
        {
            File.Create(bundleFilePath);
        }
        string bundleDataFile = File.ReadAllText(bundleFilePath);
        if (string.IsNullOrEmpty(bundleDataFile))
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("JSON File Viewer", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label(": ������Ʈ�� ����� ���� ���� ���� �Դϴ� : ���� ����");
            GUILayout.EndHorizontal();
            return;
        }
        //GUILayout.BeginArea(new Rect(10, 490, Screen.width - 20, Screen.height - 500));
        GUILayout.BeginHorizontal();
        GUILayout.Label("JSON File Viewer", EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.Label(": ������Ʈ�� ����� ���� ���� ���� �Դϴ�");
        GUILayout.EndHorizontal();
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        StringBuilder sb = new (bundleDataFile);
        for (int i = 0; i < sb.Length; i++)
        {
            if (sb[i].Equals('{'))
            {
                sb.Insert(i + 1, '\n');
            }
            if (sb[i].Equals(','))
            {
                sb.Insert(i + 1, '\n');
            }
            if (sb[i].Equals('['))
            {
                sb.Insert(i + 1, '\n');
            }
            if (sb[i].Equals(']'))
            {
                sb.Insert(i + 1, '\n');
            }
            if (sb[i].Equals('}'))
            {
                sb.Insert(i + 1, '\n');
            }
        }
        GUILayout.TextArea(sb.ToString(), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();
        //GUILayout.EndArea();
    }

    /// <summary>
    /// �׸��� �����
    /// </summary>
    private void DrawGridSlot()
    {
        Debug.Log(position.width);
        GUILayout.BeginArea(new Rect(5, 100, position.width / numRows, numCols * cellSize));

        for (int i = 0; i < numRows; i++)
        {
            GUILayout.BeginHorizontal();

            for (int j = 0; j < numCols; j++)
            {

            }

            GUILayout.EndHorizontal();
        }
        GUILayout.EndArea();
    }

    /// <summary>
    /// ���¹��� ���� ���� ��� �����
    /// </summary>
    private string MakeAssetBundleFileInfoFolderPath(BuildTarget buildTarget)
    {
        string downFolder = "";
        string totalPath = "";
        switch (buildTarget)
        {
            case BuildTarget.StandaloneWindows: downFolder = Path.DirectorySeparatorChar + "Standalone"; break;
            case BuildTarget.Android: downFolder = Path.DirectorySeparatorChar + "Android"; break;
            case BuildTarget.iOS: downFolder = Path.DirectorySeparatorChar + "IOS"; break;
        }

        //������ ������ ����
        if (Directory.Exists(Application.streamingAssetsPath + downFolder) == false)
        {
            Directory.CreateDirectory(Application.streamingAssetsPath + downFolder);
        }

        totalPath = Application.streamingAssetsPath + downFolder + Path.DirectorySeparatorChar + "AssetBundleInfo_" + buildTarget.ToString() + ".json";
        return totalPath;
    }

    /// <summary>���� �׸���</summary>
    private void DrawLine(int aSpace = 5)
    {
        GUILayout.Space(aSpace);
        var rect = EditorGUILayout.BeginHorizontal();
        Handles.color = Color.gray;
        Handles.DrawLine(new Vector2(rect.x - 15, rect.y), new Vector2(rect.width + 15, rect.y));
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(aSpace);
    }
}
