using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System;

public class AnimToolBoxWindow : EditorWindow
{

    private Animator[] _animatorComponentsArr = null;
    private AnimationClip[] _animationClipComponentsArr = null;
    private string[] _animationClipNameComponentsArr = null;

    private List<AnimationClip> _animationClipsPlaying = new List<AnimationClip>();
    private AnimToolboxData _animToolboxData;

    private Animator _selectedAnimator = null;
    private AnimationClip _selectedAnimationClip = null;

    private bool _isPlaying = false;
    private float _editorLastTime = 0f;

    private float _speedValue = 1;
    private float _sampleAnimationSliderValue = 1;
    private float _animeTime = 0;
    private float _timeBetweenLoopValue = 0;

    private int _tabIndex = 0;
    public static string[] _tabs = new string[]
    {
        "Animator",
        "Animation",
        "Animation Settings"
    };

    [MenuItem("Window/Toolbox/AnimToolboxWindow")]
    static void InitWindow()
    {
        EditorWindow window = GetWindow<AnimToolBoxWindow>();
        window.autoRepaintOnSceneChange = true;
        window.Show();
        window.titleContent = new GUIContent("Anim_toolbox");
    }

    void OnDestroy()
    {
        Debug.Log("Widow destroyed");
        StopAnim();
    }

    private void OnGUI()
    {
        _tabIndex = GUILayout.Toolbar(_tabIndex, _tabs);

        switch (_tabIndex)
        {
            case 0: _GUITabAnimator(); break;
            case 1: _GUITabAnimation(); break;
            case 2: _GUITabAnimationSettings(); break;
        }
    }

    private void _GUITabAnimator()
    {
        if (null == _animatorComponentsArr)
            _animatorComponentsArr = _FindAnimatorInScene();

        foreach (Animator animator in _animatorComponentsArr)
        {
            if (null != animator)
            {
                if (GUILayout.Button(animator.name))
                {
                    _selectedAnimator = animator;
                    Selection.activeGameObject = animator.gameObject;
                    SceneView.lastActiveSceneView.FrameSelected();
                    EditorGUIUtility.PingObject(animator.gameObject);
                    //StopAnim();
                    _selectedAnimationClip = null;

                }
            }
        }
    }

    private void _GUITabAnimation()
    {
        if (null == _selectedAnimator)
        {
            EditorGUILayout.HelpBox("No animator selected", MessageType.Warning);
        }
        else
        {
            _animationClipComponentsArr = _FindAnimationInAnimator();
            _animationClipNameComponentsArr = FindAnimNames();

            for (int i = 0; i < _animationClipComponentsArr.Length; i++)
            {
                if (GUILayout.Button(_animationClipNameComponentsArr[i]))
                {
                    _selectedAnimationClip = _animationClipComponentsArr[i];
                    SceneView.lastActiveSceneView.FrameSelected();
                    if (_isPlaying)
                        StopAnim();
                }
            }

            if (EditorApplication.isPlaying)
            {
                StopAnim();
            }
            else
            {
                GUILayout.Space(20f);
                if (null != _selectedAnimationClip)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Play"))
                        PlayAnim();
                    if (GUILayout.Button("Stop"))
                        StopAnim();
                    GUILayout.EndHorizontal();
                }
            }
        }
    }

    //ne marche pas ....?
    private void OnHierarchyChange()
    {
        _animatorComponentsArr = _FindAnimatorInScene();
    }

    private void _GUITabAnimationSettings()
    {
        if (null == _selectedAnimationClip)
        {
            EditorGUILayout.HelpBox("No animation selected", MessageType.Warning);
        }
        else
        {
            //Speed
            EditorGUILayout.LabelField("Speed");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-"))
                _SlowDownAnime();

            _speedValue = EditorGUILayout.FloatField(_speedValue);

            if (GUILayout.Button("+"))
                _AccelerateAnime();
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            //sample
            EditorGUILayout.LabelField("Sample Slider");
            _sampleAnimationSliderValue = EditorGUILayout.Slider(_sampleAnimationSliderValue, 0, _selectedAnimationClip.length);
            _selectedAnimationClip.SampleAnimation(_selectedAnimator.gameObject, _sampleAnimationSliderValue);

            //more Info
            EditorGUILayout.LabelField("Actual animation Time : " + _animeTime);
            EditorGUILayout.LabelField("Total animation Time : " + _selectedAnimationClip.length + "s");
            EditorGUILayout.LabelField("Is animation looping : " + _selectedAnimationClip.isLooping.ToString());
            GUILayout.Space(10f);


            //time between update
            EditorGUILayout.LabelField("time between update");
            _timeBetweenLoopValue = EditorGUILayout.FloatField(_timeBetweenLoopValue);

            //Save setting in scriptableObject
            if (GUILayout.Button("Save time between loop"))
            {
                _SaveTimebeetwenLoopDataSetting(_timeBetweenLoopValue);
            }
            if (GUILayout.Button("Reset list"))
            {
                _ResetDataSetting();
            }

        }
    }

    private Animator[] _FindAnimatorInScene()
    {
        List<Animator> animatorList = new List<Animator>();
        foreach (GameObject rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            animatorList.AddRange(rootGameObject.GetComponentsInChildren<Animator>());
        }
        return animatorList.ToArray();
    }

    private AnimationClip[] _FindAnimationInAnimator()
    {
        return FindAnimClips();
    }

    private AnimationClip[] FindAnimClips()
    {
        List<AnimationClip> resultList = new List<AnimationClip>();

        AnimatorController editorController = _selectedAnimator.runtimeAnimatorController as AnimatorController;

        AnimatorControllerLayer controllerLayer = editorController.layers[0];
        foreach (ChildAnimatorState childState in controllerLayer.stateMachine.states)
        {
            AnimationClip animClip = childState.state.motion as AnimationClip;
            if (animClip != null)
            {
                resultList.Add(animClip);
            }
        }
        return resultList.ToArray();
    }

    private string[] FindAnimNames()
    {
        List<string> resultList = new List<string>();
        foreach (AnimationClip clip in _animationClipComponentsArr)
        {
            resultList.Add(clip.name);
        }

        return resultList.ToArray();
    }

    private void PlayAnim()
    {
        if (_isPlaying) return;
        _editorLastTime = Time.realtimeSinceStartup;
        EditorApplication.update += _OnEditorUpdate;
        AnimationMode.StartAnimationMode();
        _isPlaying = true;
    }

    private void StopAnim()
    {
        if (!_isPlaying) return;
        EditorApplication.update -= _OnEditorUpdate;
        AnimationMode.StopAnimationMode();
        _isPlaying = false;
    }

    private void _OnEditorUpdate()
    {
        if (!_isPlaying) return;
        float animTime = Time.realtimeSinceStartup - _editorLastTime;
        animTime *= _speedValue;
        animTime %= _selectedAnimationClip.length + _timeBetweenLoopValue;
        _animeTime = animTime;
        AnimationMode.SampleAnimationClip(_selectedAnimator.gameObject, _selectedAnimationClip, animTime);
    }



    private void _AccelerateAnime()
    {
        _speedValue += 0.25f;
    }

    private void _SlowDownAnime()
    {
        _speedValue -= 0.25f;
    }

    private void _ResetSettings()
    {
        _selectedAnimator.speed = 1;
    }

    private void _SaveTimebeetwenLoopDataSetting(float timeBetweenLoop)
    {
        if (null == _animToolboxData)
            _animToolboxData = _FindAnimToolboxDataInProject();

        _animToolboxData.AddTimeToList(_selectedAnimationClip.name, timeBetweenLoop);
    }

    private void _ResetDataSetting()
    {
        if (null == _animToolboxData)
            return;

        _animToolboxData.ResetList();
    }

    private AnimToolboxData _FindAnimToolboxDataInProject()
    {
        string[] fileGuisArr = AssetDatabase.FindAssets("t:" + typeof(AnimToolboxData));
        if (fileGuisArr.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(fileGuisArr[0]);
            return AssetDatabase.LoadAssetAtPath<AnimToolboxData>(assetPath);
        }
        else
        {
            AnimToolboxData gameData = ScriptableObject.CreateInstance<AnimToolboxData>();
            AssetDatabase.CreateAsset(gameData, "Assets/AnimToolboxData.asset");
            AssetDatabase.SaveAssets();
            return gameData;
        }
    }
}
