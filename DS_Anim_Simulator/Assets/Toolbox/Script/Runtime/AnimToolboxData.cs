using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Toolbox/Game/AnimToolboxData")]
public class AnimToolboxData : ScriptableObject
{
    public List<string> animationNameList = new List<string>();
    public List<float> animationTimeBetweenLoopList = new List<float>();

    public void AddTimeToList(string animName, float animeTimeBetweenLoop)
    {
        if(animationNameList.Contains(animName))
        {
            int indexList = animationNameList.FindIndex(x => x.Equals(animName));
            animationTimeBetweenLoopList[indexList] = animeTimeBetweenLoop;
        }
        else
        {
            animationNameList.Add(animName);
            animationTimeBetweenLoopList.Add(animeTimeBetweenLoop);
        }
    }

    public void ResetList()
    {
        animationNameList.Clear();
        animationTimeBetweenLoopList.Clear();
    }
}
