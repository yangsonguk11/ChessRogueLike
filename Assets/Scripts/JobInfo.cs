using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewJobInfo", menuName = "JobInfo")]
public class JobInfo : ScriptableObject
{
    [SerializeField] string _jobName;

    [Tooltip("이 직업의 기물이 전투 승리 보상 화면에서 카드 선택지를 뽑을 전용 풀. CardDatabase에 등록된 카드 이름.")]
    [SerializeField] List<string> _rewardCardPool;

    public string JobName => _jobName;
    public List<string> RewardCardPool => _rewardCardPool;
}
