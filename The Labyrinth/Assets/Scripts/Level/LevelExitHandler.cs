using UnityEngine;

namespace GDD3400.Labyrinth
{
    public class LevelExitHandler : MonoBehaviour
    {
        private LevelManager _levelManager;

        public void Initialize(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        public void OnTriggerEnter(Collider collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                _levelManager.LevelComplete();
            }
        }
    }
}
