using UnityEngine;

namespace BubbleTeaShop
{
    public class ShopfrontController : MonoBehaviour
    {
        [Header("Sub-Systems")]
        [SerializeField] private ShutterController shutter;
        [SerializeField] private DeskBell bell;
        [SerializeField] private CupStation cupStation;
        [SerializeField] private SugarDispenser sugarDispenser;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.MorningPrep)
            {
                sugarDispenser?.UpdateUpgradeMode();
            }
        }
    }
}
