using UnityEngine;
public class HomeSceneDirector : MonoBehaviour
{
    [SerializeField] HomeSceneInitializationOperator _homeSceneInitializationOperator;
    public void Start()
    {
        _homeSceneInitializationOperator.Initialize();
    }
}