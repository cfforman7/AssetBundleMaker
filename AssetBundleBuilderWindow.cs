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
    
    /// <summary>에셋번들 저장 경로</summary>
    private string saveAssetBundlePath = "Assets/AssetBundle";
    private string saveBundleDownFolder = "";

    /// <summary>에셋번들 정보 파일 경로</summary>
    private string bundleFilePath;
    /// <summary>로컬 에셋번들 정보 클래스</summary>
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
    /// 빌드 옵션 리스트를 생성 합니다.
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
                    desc = "옵션 없음"; break;
                case BuildAssetBundleOptions.UncompressedAssetBundle:
                    desc = "에셋 번들을 압축하지 않음."; break;
                case BuildAssetBundleOptions.CollectDependencies:
                    desc = "(사용되지 않음)에셋 번들 빌드 시, 종속성을 자동으로 수집하여 번들에 추가함."; break;
                case BuildAssetBundleOptions.CompleteAssets:
                    desc = "(사용되지 않음) 모든 에셋을 포함하는 에셋 번들 생성"; break;
                case BuildAssetBundleOptions.DisableWriteTypeTree:
                    desc = "에셋 번들 빌드 시, 유니티는 유형 트리(type tree)를 작성하는데, 이 옵션을 사용하면 유형 트리 작성을 비활성화할 수 있습니다."; break;
                case BuildAssetBundleOptions.DeterministicAssetBundle:
                    desc = "동일한 에셋 번들에 대해 항상 동일한 버전 ID 생성 : 에셋 번들의 내용이 변경되지 않았는지 확인합니다. 이렇게 하면 동일한 내용을 가진 두 에셋 번들의 해시값이 같아집니다."; break;
                case BuildAssetBundleOptions.ForceRebuildAssetBundle:
                    desc = "에셋 번들을 강제로 다시 빌드합니다."; break;
                case BuildAssetBundleOptions.IgnoreTypeTreeChanges:
                    desc = "에셋 번들을 빌드할 때, 유형 트리 변경사항을 무시합니다."; break;
                case BuildAssetBundleOptions.AppendHashToAssetBundleName:
                    desc = "에셋 번들 이름 뒤에 해시값을 추가하여, 에셋 번들의 이름이 중복되는 것을 방지합니다."; break;
                case BuildAssetBundleOptions.ChunkBasedCompression:
                    desc = "에셋 번들을 청크 단위로 압축합니다. : AssetBundle을 작은 조각으로 나누어 저장하는데, 이때 사용되는 단위가 청크(chunk)입니다. 기본적으로는 4MB가 지정되어 있습니다. 즉, AssetBundle의 크기가 4MB를 넘어가면 자동으로 청크로 나누어 저장하게 됩니다."; break;
                case BuildAssetBundleOptions.StrictMode:
                    desc = "에셋 번들 빌드 시, 모든 경고를 오류로 취급합니다."; break;
                case BuildAssetBundleOptions.DryRunBuild:
                    desc = "에셋 번들 빌드를 수행하지 않고, 빌드에 필요한 정보만 출력합니다."; break;
                case BuildAssetBundleOptions.DisableLoadAssetByFileName:
                    desc = "에셋 번들 빌드 시, 파일 이름으로 에셋을 로드하는 것을 비활성화합니다."; break;
                case BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension:
                    desc = "에셋 번들 빌드 시, 파일 이름과 확장자를 모두 사용하여 에셋을 로드하는 것을 비활성화합니다."; break;
                case BuildAssetBundleOptions.AssetBundleStripUnityVersion:
                    desc = "에셋 번들 빌드 시, 종속성 파일을 작성하지 않습니다."; break;
            }
            buildOptionContents[i] = new GUIContent(title, desc);
        }
    }

    /// <summary>
    /// 활성화 시 1회 호출
    /// </summary>
    private void OnEnable()
    {
        bundleFoldouts = new Dictionary<string, bool>();
        bundleFilterItemDic = new Dictionary<string, string>();
        bundleVersionDic = new Dictionary<string, uint>();
        //현재 빌드 타겟 리턴.
        BuildTarget curBuildTarget = EditorUserBuildSettings.activeBuildTarget;
        //파일 정보가 존재하는 Path 리턴
        bundleFilePath = MakeAssetBundleFileInfoFolderPath(curBuildTarget);
        //파일 존재 여부
        if (File.Exists(bundleFilePath))
        {//에셋 번들 파일 정보 리턴.
            string localInfoData = File.ReadAllText(bundleFilePath);
            //번들 파일 정보 클래스로 변환
            bundleFileDataInfoArray = JsonUtility.FromJson<AssetBundleDataInfoArray>(localInfoData);
        }
        else
        {
            File.Create(bundleFilePath);
        }
        buildTargetArr = new string[] { BuildTarget.Android.ToString(), BuildTarget.iOS.ToString(), BuildTarget.StandaloneWindows.ToString()};
        //옵션 리스트 생성
        MakeBuildOptionList();
    }

    private void OnGUI()
    {
        DrawLine(10);
        //빌드 타겟 셋팅
        DrawBuildTarget();
        DrawLine(10);
        //빌드 옵션 셋팅
        DrawBuildOption();
        DrawLine(10);
        //저장 경로 셋팅
        DrawBundleSavePath();
        DrawLine(10);
        GUILayout.BeginVertical();
        //프로젝트 내에 있는 지정된 에셋번들 리스트 보여주기
        DrawAssetBundleList();
        DrawLine(10);
        //에셋번들 빌드 시작
        DrawBuildBundleProcess();
        DrawLine(10);
        //에셋번들 파일 정보를 보여준다.
        DrawAssetbundleDataFileInfo();
        GUILayout.EndVertical();
    }

    /// <summary>
    /// 빌드 타겟 설정
    /// </summary>
    private void DrawBuildTarget()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("빌드 타겟을 선택하세요");
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        buildTarget = GUILayout.SelectionGrid(buildTarget, buildTargetArr, buildTargetArr.Length, EditorStyles.toolbarButton);
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 빌드 옵션 설정
    /// </summary>
    private void DrawBuildOption()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("빌드 옵션을 선택하세요");
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        buildOption = (BuildAssetBundleOptions)GUILayout.SelectionGrid((int)buildOption, buildOptionContents, 3, EditorStyles.toolbarButton);
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 에셋번들 저장 경로 설정
    /// </summary>
    private void DrawBundleSavePath()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("에셋 번들 저장 경로 : ", GUILayout.Width(130));
        GUILayout.Space(5);
        saveAssetBundlePath = EditorGUILayout.TextField(saveAssetBundlePath);        
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal("Box");
        //빌드 타겟 설정
        BuildTarget selectBuildTarget = (BuildTarget)Enum.Parse(typeof(BuildTarget), buildTargetArr[buildTarget]);
        switch (selectBuildTarget)
        {
            case BuildTarget.Android: saveBundleDownFolder = Path.DirectorySeparatorChar + "Android"; break;
            case BuildTarget.iOS: saveBundleDownFolder = Path.DirectorySeparatorChar + "IOS"; break;
            case BuildTarget.StandaloneWindows: saveBundleDownFolder = Path.DirectorySeparatorChar + "Standalone"; break;
        }
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.yellow;
        GUILayout.Label("타겟에 따라 해당 경로에 저장 됩니다. ", style, GUILayout.Width(250));
        GUILayout.Label(saveAssetBundlePath + saveBundleDownFolder, style);
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 에셋번들 리스트 보여주기
    /// </summary>
    private void DrawAssetBundleList()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("에셋 번들 리스트", GUILayout.Width(130));
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
            //버전 설정
            if (bundleVersionDic.ContainsKey(allBundles[i]))
            {
                bundleVersion = bundleVersionDic[allBundles[i]];
            }
            GUILayout.Label("저번에 생성 된 번들의 Verstion", GUILayout.Width(300));
            bundleVersion = (uint)EditorGUILayout.IntField((int)bundleVersion, GUILayout.Width(100));
            bundleVersionDic[allBundles[i]] = bundleVersion;

            if (GUILayout.Button(new GUIContent("+1", "버전을 1 올립니다"), GUILayout.Width(100)))
            {
                bundleVersionDic[allBundles[i]] = bundleVersionDic[allBundles[i]] + 1;
            }
            if (GUILayout.Button(new GUIContent("-1", "버전을 1 내립니다"), GUILayout.Width(100)))
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
                    t: 타입 검색 (예: "t: Texture")
                    l: 경로 검색 (예: "l: Assets/Textures")
                    n: 이름 검색 (예: "n: texture")
                    s: 크기 검색 (예: "s: > 1024")
                    b: 에셋 번들 검색 (예: "b: mybundle")
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
    /// 에셋번들 생성 및 파일로 저장합니다.
    /// </summary>
    private void DrawBuildBundleProcess()
    {
        if (GUILayout.Button("에셋 번들 빌드 시작"))
        {
            //위의 지정된 경로에 폴더가 없으면 생성
            if (Directory.Exists(saveAssetBundlePath + saveBundleDownFolder) == false)
            {
                Directory.CreateDirectory(saveAssetBundlePath + saveBundleDownFolder);
            }

            //빌드 타겟 설정
            BuildTarget selectBuildTarget = (BuildTarget)Enum.Parse(typeof(BuildTarget), buildTargetArr[buildTarget]);
            //빌드 시작
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(saveAssetBundlePath + saveBundleDownFolder, buildOption, selectBuildTarget);
            //BuildPipeline.BuildAssetBundles(saveAssetBundlePath, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

            //파일로 저장하기 위해 모든 에셋번들 다시 리턴
            string[] allBundles = AssetDatabase.GetAllAssetBundleNames();
            //임시 리스트 생성
            List<AssetBundleDataInfo> bundleDataInfoList = new List<AssetBundleDataInfo>();
            for (int i = 0; i < allBundles.Length; i++)
            {
                AssetBundleDataInfo dundleDataInfo = new AssetBundleDataInfo();
                dundleDataInfo.name = allBundles[i];

                uint crc;
                BuildPipeline.GetCRCForAssetBundle(saveAssetBundlePath + saveBundleDownFolder + Path.DirectorySeparatorChar + allBundles[i], out crc);

                dundleDataInfo.version = bundleVersionDic[allBundles[i]];
                dundleDataInfo.crc = crc;
                //리스트에 저장
                bundleDataInfoList.Add(dundleDataInfo);
            }
            //JsonUtility 를 사용하기 위해 클래스를 다시 생성.
            AssetBundleDataInfoArray bundleDataInfoArray = new AssetBundleDataInfoArray();
            bundleDataInfoArray.assetBundleDataInfoArray = bundleDataInfoList.ToArray();
            string bundleDataFile = JsonUtility.ToJson(bundleDataInfoArray);
            if (bundleDataFile != null)
            {
                bundleFilePath = MakeAssetBundleFileInfoFolderPath(selectBuildTarget);
                File.WriteAllText(bundleFilePath, bundleDataFile);
                AssetDatabase.Refresh();
            }
            //번들 파일 최신 것으로 갱신
            bundleFileDataInfoArray = bundleDataInfoArray;
        }
    }

    /// <summary>
    /// 생성 된 Json 파일 정보 보여주기
    /// </summary>
    private void DrawAssetbundleDataFileInfo()
    {
        if (bundleFileDataInfoArray == null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("JSON File Viewer", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label(": 프로젝트에 저장된 번들 파일 정보 입니다 : 정보 없음");
            GUILayout.EndHorizontal();
            return;
        }
        //빌드 타겟 설정
        BuildTarget selectBuildTarget = (BuildTarget)Enum.Parse(typeof(BuildTarget), buildTargetArr[buildTarget]);
        //에셋번들 파일 정보 경로 리턴
        bundleFilePath = MakeAssetBundleFileInfoFolderPath(selectBuildTarget);
        //에셋 번들 파일 정보 리턴.
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
            GUILayout.Label(": 프로젝트에 저장된 번들 파일 정보 입니다 : 정보 없음");
            GUILayout.EndHorizontal();
            return;
        }
        //GUILayout.BeginArea(new Rect(10, 490, Screen.width - 20, Screen.height - 500));
        GUILayout.BeginHorizontal();
        GUILayout.Label("JSON File Viewer", EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.Label(": 프로젝트에 저장된 번들 파일 정보 입니다");
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
    /// 그리드 만들기
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
    /// 에셋번들 정보 파일 경로 만들기
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

        //폴더가 없으면 생성
        if (Directory.Exists(Application.streamingAssetsPath + downFolder) == false)
        {
            Directory.CreateDirectory(Application.streamingAssetsPath + downFolder);
        }

        totalPath = Application.streamingAssetsPath + downFolder + Path.DirectorySeparatorChar + "AssetBundleInfo_" + buildTarget.ToString() + ".json";
        return totalPath;
    }

    /// <summary>라인 그리기</summary>
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
