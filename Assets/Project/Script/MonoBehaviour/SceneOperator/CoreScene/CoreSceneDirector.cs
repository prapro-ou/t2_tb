using UnityEngine;
namespace Project
{
    public class CoreSceneDirector : MonoBehaviour
    {
        [SerializeField] CoreSceneInitializationOperator _initializationOperator;
        void Start()
        {
            _initializationOperator.Intialize();
        }
    }
}